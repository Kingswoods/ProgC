using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Net;
using MailServer.Exceptions;
using MailServer.Database;

namespace MailServer
{
    class SmtpListener : TcpListener
    {
        private TcpClient Client;
        private NetworkStream Stream;
        private StreamReader Reader;
        private StreamWriter Writer;
        private Thread Thread;
        private Program Owner;

        //Bools
        private bool HasGreeted;
        private bool HasAuthenticated;

        private readonly Mail Email;

        //String for console output
        private const string AUTH = "AUTH LOGIN";
        private const string MAIL_FROM = "MAIL FROM:";
        private const string RCPT_TO = "RCPT TO:";
        private const string SUBJECT = "Subject: ";
        private const string MIME_VERSION = "MIME-Version: ";
        private const string DATE = "Date: ";
        private const string CONTENT_TYPE = "Content-Type: ";
        private const string CONTENT_TRANSFER_ENCODING = "Content-Transfer-Encoding: ";

        public SmtpListener(Program owner, IPAddress addr, int port) : base(addr, port)
        {
            this.Owner = owner;
            this.Email = new Mail();
        }

        public new void Start()
        {
            base.Start();

            this.Client = this.AcceptTcpClient();
            this.Client.ReceiveTimeout = 5000;
            this.Stream = this.Client.GetStream();
            this.Stream.ReadTimeout = 120000;
            this.Reader = new StreamReader(this.Stream);
            this.Writer = new StreamWriter(this.Stream)
            {
                NewLine = "\r\n",
                AutoFlush = true
            };

            this.Thread = new Thread(this.RunThread);
            this.Thread.Start();
        }

        protected void RunThread()
        {
            this.WriteLine("220 localhost");

            try
            {
                while(this.Reader != null)
                {
                    string line = this.Reader.ReadLine();
                    Console.WriteLine("C: " + line);

                    //Handle QUIT - Always allowed
                    if(line == "BYE" || line == "QUIT")
                    {
                        this.WriteLine("250 Have a nice day!");
                        this.Reader = null;
                        break;
                    }

                    //Check for EHLO
                    if(!line.StartsWith("EHLO ") && !line.StartsWith("HELO ") && !this.HasGreeted)
                    {
                        this.WriteLine("503 Please start with EHLO");
                        continue;
                    }

                    //Handle EHLO
                    if(line.StartsWith("EHLO") || line.StartsWith("HELO"))
                    {
                        string senderServer = line.Substring(5);

                        try
                        {
                            this.Email.SenderServer = senderServer;
                            this.WriteLine($"250-Hello {senderServer}" + Environment.NewLine + "250 AUTH LOGIN");
                            this.HasGreeted = true;
                            continue;
                        }
                        catch(InvalidEhloException ex)
                        {
                            this.WriteLine(ex.Message);
                        }
                    }

                    //Handle Authentication
                    if(line.Equals(AUTH) && !this.HasAuthenticated)
                    {
                        bool authFail = false;
                        this.WriteLine("334 VXN1cm5hbWU6");
                        string username = this.Reader.ReadLine();

                        if(username != null)
                        {
                            Console.WriteLine($"C: {username}");

                            //Decode BASE64 received username
                            username = this.DecodeBase64(username);

                            Models.User user = null;

                            try
                            {
                                UserTable ut = new UserTable();
                                user = ut.GetUserByEmailAddress(username);
                                ut.Disconnect();
                            }
                            catch(NotFoundException)
                            {
                                authFail = true;
                            }

                            this.WriteLine("334 UGFzc3dvcmQ6");
                            string password = this.Reader.ReadLine();

                            if(password != null)
                            {
                                Console.WriteLine($"C: {password}");

                                //Decode BASE64 received password
                                password = this.DecodeBase64(password);

                                if(user != null && !authFail && password == user.Password)
                                {
                                    this.WriteLine("235 Authentication succesful");
                                    this.HasAuthenticated = true;
                                    continue;
                                }
                                else
                                {
                                    this.WriteLine("535 Authentication failed");
                                    this.Reader = null;
                                    break;
                                }
                            }
                        }

                    }

                    //Check Authentication
                    if(this.HasAuthenticated)
                    {
                        //Handle sender email
                        if(line.StartsWith(MAIL_FROM))
                        {
                            string from = line.Substring(MAIL_FROM.Length).Replace("<", "").Replace(">", "");

                            try
                            {
                                this.Email.From = from;
                                this.WriteLine("250 Ok");
                            }
                            catch(NotFoundException ex)
                            {
                                this.WriteLine(ex.Message);
                            }
                        }

                        //Handle recipient email
                        else if(line.StartsWith(RCPT_TO))
                        {
                            string recipient = line.Substring(RCPT_TO.Length).Replace("<", "").Replace(">", "");

                            try
                            {
                                this.Email.AddRecipient(recipient);
                                this.WriteLine("250 Ok");
                            }
                            catch(NotFoundException ex)
                            {
                                this.WriteLine(ex.Message);
                            }
                            catch(RemoteAddressException ex)
                            {
                                this.WriteLine(ex.Message);
                            }
                            catch(NoSenderException ex)
                            {
                                this.WriteLine(ex.Message);
                            }
                        }

                        //Handle Data
                        else if(line == "DATA")
                        {
                            if(this.Email.IsReadyForData())
                            {
                                this.WriteLine("354 Start input, end data with <CRLF>.<CRLF>");
                                StringBuilder data = new StringBuilder();

                                line = this.Reader.ReadLine();

                                while(line != null && line != ".")
                                {
                                    if(line.StartsWith(SUBJECT))
                                    {
                                        this.Email.Subject = line.Substring(SUBJECT.Length);
                                    }
                                    else if(line.StartsWith(MIME_VERSION))
                                    {
                                        this.Email.MimeVersion = line.Substring(MIME_VERSION.Length);
                                    }
                                    else if(line.StartsWith(DATE))
                                    {
                                        this.Email.Date = line.Substring(DATE.Length);
                                    }
                                    else if(line.StartsWith(CONTENT_TYPE))
                                    {
                                        this.Email.ContentType = line.Substring(CONTENT_TYPE.Length);
                                    }
                                    else if(line.StartsWith(CONTENT_TRANSFER_ENCODING))
                                    {
                                        this.Email.ContentTransferEncoding = line.Substring(CONTENT_TRANSFER_ENCODING.Length);
                                    }
                                    else
                                    {
                                        data.AppendLine(line);
                                    }

                                    line = this.Reader.ReadLine();
                                }

                                this.Email.Message = data.ToString();

                                this.Email.WriteMessage();
                                this.WriteLine("250 Message OK");
                            }
                            else
                            {
                                this.WriteLine("500 Invalid sequence; Sender and Recipient mails are required first");
                            }

                            break;
                        }

                        //Unsupported
                        else
                        {
                            this.WriteLine("502 Command not yet implemented");
                        }
                    }
                    else
                    {
                        this.WriteLine("503 Authentication required");
                    }
                }
            }
            catch(IOException ex)
            {
                Console.WriteLine("MailListener.RunThread IOException: Connection lost.");
                Console.WriteLine(ex);
            }
            catch(Exception ex)
            {
                Console.WriteLine($"MailListener.RunThread Exception: {ex}");
            }
            finally
            {
                this.Client.Close();
                this.Stop();
            }

        }

        private void WriteLine(string content)
        {
            if(this.Writer == null)
            {
                throw new InvalidOperationException("Writer has not been initialized yet");
            }

            this.Writer.WriteLine(content);

            Console.WriteLine($"S: {content}");
        }

        public bool IsThreadAlive => this.Thread.IsAlive;

        private string DecodeBase64(string s)
        {
            byte[] bytes = Convert.FromBase64String(s);
            return Encoding.UTF8.GetString(bytes);
        }
    }
}
