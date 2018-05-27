using MailServer.Database;
using MailServer.Exceptions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace MailServer
{
    [Serializable]
    public class Mail
    {
        private string _senderServer;
        private string _messageId;
        private string _from;
        private string _message;
        private string _mimeVersion;
        private string _date;
        private string _contentType;

        public string SenderServer
        {
            get => this._senderServer;
            set
            {
                if(string.IsNullOrEmpty(value))
                {
                    throw new InvalidEhloException("503 Invalid EHLO");
                }

                if(value.Contains(" "))
                {
                    throw new InvalidEhloException("503 Invalid EHLO");
                }

                if(!value.Contains("."))
                {
                    throw new InvalidEhloException("503 Invalid EHLO");
                }

                this._senderServer = value;
            }
        }

        public string MessageId
        {
            get => this._messageId;
            set
            {
                this._messageId = value;
                this.MessageHeaders.Add("Message-ID: " + this._messageId);
            }
        }

        public string Subject { get; set; }

        public string From
        {
            get => this._from;
            set
            {
                //Get email
                try
                {
                    value = value.Substring(value.IndexOf("<"), value.IndexOf(">"));
                }
                catch(ArgumentOutOfRangeException)
                {
                }

                //Strip the <> around the email
                value = value.Replace("<", "").Replace(">", "");

                UserTable ut = new UserTable();

                if(ut.CheckIfEmailExists(value))
                {
                    this._from = value;
                    this.MessageHeaders.Add("From: " + this._from);
                }
                else
                {
                    throw new NotFoundException("550 No such user");
                }
            }
        }

        public readonly List<string> RecipientList = new List<string>();

        public readonly List<string> MessageHeaders = new List<string>();

        public string Message
        {
            get => this._message;
            set
            {
                this._message = value;
            }
        }

        public string MimeVersion
        {
            get => this._mimeVersion;
            set
            {
                this._mimeVersion = value;
                this.MessageHeaders.Add("MIME-Version: " + this._mimeVersion);
            }
        }

        public string Date
        {
            get => this._date;
            set
            {
                this._date = value;
                this.MessageHeaders.Add("Date: " + this._date);
            }
        }

        public bool IsEncrypted = false;

        public string SecretIV = "";

        public bool Read = false;

        public string ContentType
        {
            get => _contentType;
            set
            {
                this._contentType = value;
                this.MessageHeaders.Add("Content-Type: " + this._contentType);
            }
        }

        public string ContentTransferEncoding { get; set; }

        public void AddRecipient(string recipient)
        {
            if(!string.IsNullOrEmpty(this.From))
            {
                //Get email
                try
                {
                    recipient = recipient.Substring(recipient.IndexOf("<"), recipient.IndexOf(">"));
                }
                catch(ArgumentOutOfRangeException)
                {
                }

                //Strip the <> from the email
                recipient = recipient.Replace("<", "").Replace(">", "");

                if (recipient.Contains("@experimentum.xyz"))
                {
                    UserTable ut = new UserTable();

                    if(ut.CheckIfEmailExists(recipient))
                    {
                        this.RecipientList.Add(recipient);
                        
                        if(this.RecipientList.Count == 1)
                        {
                            this.MessageHeaders.Add("To: " + recipient);
                        }
                        else
                        {
                            this.MessageHeaders.Add("Cc: " + recipient);
                        }
                    }
                    else
                    {
                        throw new NotFoundException("550 No such user");
                    }
                }
                else
                {
                    throw new RemoteAddressException("553 Remote addresses are not supported");
                }
            }
            else
            {
                throw new NoSenderException("503 Sender email is required before recipient");
            }
        }

        public string GetRecipient(int index)
        {
            return this.RecipientList[index];
        }

        public bool IsReadyForData()
        {
            return !string.IsNullOrEmpty(this.From) && this.RecipientList.Count != 0;
        }

        public bool WriteMessage()
        {
            if(this.ContentTransferEncoding == "quoted-printable")
            {
                this.DecodeQuotedPrintable();
            }

            //Generate unique message ID
            this.GenerateAndSetMessageId();

            //Add received header
            this.CreateReceivedHeader();

            //Prepend the headers to the message
            this.AddHeadersToMessage();

            this.WriteFile();

#if DEBUG
            Console.WriteLine($"DEBUG: Received email from {this.SenderServer}");
            Console.WriteLine($"DEBUG: Type: " + this.ContentType);
            Console.WriteLine($"DEBUG: Encoding: " + this.ContentTransferEncoding);
            Console.WriteLine($"DEBUG: MIME Version: " + this.MimeVersion);
            Console.WriteLine($"DEBUG: Date: " + this.Date);
            Console.WriteLine($"DEBUG: ID: " + this.MessageId);
            Console.WriteLine($"DEBUG: From: " + this.From);
            Console.Write("DEBUG: To: ");

            foreach(string to in this.RecipientList)
            {
                Console.Write(to, " ");
            }

            Console.Write(Environment.NewLine);
            Console.WriteLine("DEBUG: Subject: " + this.Subject);
            Console.WriteLine("-----------------------------------------------------------------------------");
            Console.WriteLine("DEBUG: Message: " + Environment.NewLine + this.Message);
            Console.WriteLine("=============================================================================");
            Console.WriteLine("");
#endif

            return true;
        }

        private void GenerateAndSetMessageId()
        {
            if (string.IsNullOrEmpty(this.From) || string.IsNullOrEmpty(this.RecipientList[0]))
            {
                throw new InvalidOperationException();
            }

            string input = this.From;

            foreach(string recipient in this.RecipientList)
            {
                input += recipient;
            }

            input += DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds;

            StringBuilder hash = new StringBuilder();
            MD5CryptoServiceProvider md5Provider = new MD5CryptoServiceProvider();

            byte[] bytes = md5Provider.ComputeHash(new UTF8Encoding().GetBytes(input));

            foreach(byte b in bytes)
            {
                hash.Append(b.ToString("x2"));
            }

            this.MessageId = hash.ToString();
        }

        private void CreateReceivedHeader()
        {
            string date = DateTime.UtcNow.ToString("ddd, dd MMM yyyy hh:mm:ss") + "(UTC)";
            this.MessageHeaders.Insert(0, "Received: by " + IPAddress.Loopback + " with SMTP id " + this.MessageId + ";" +
                                          Environment.NewLine
                                          + "\t" + date);
        }

        private void DecodeQuotedPrintable()
        {
            Regex occurences = new Regex(@"(=[0-9A-Z][0-9A-Z])+", RegexOptions.Multiline);
            MatchCollection matches = occurences.Matches(this.Message);

            foreach(Match m in matches)
            {
                byte[] bytes = new byte[m.Value.Length / 3];

                for(int i = 0; i < bytes.Length; i++)
                {
                    string hex = m.Value.Substring(i * 3 + 1, 2);
                    int hexInt = Convert.ToInt32(hex, 16);
                    bytes[i] = Convert.ToByte(hexInt);
                }

                this.Message = this.Message.Replace(m.Value, Encoding.Default.GetString(bytes));
            }

            this.Message.Replace("=\r\n", "");
        }

        private void AddHeadersToMessage()
        {
            string tempVal = "";
            string lastHeader = this.MessageHeaders.Last();

            foreach(string header in this.MessageHeaders)
            {
                tempVal += header + Environment.NewLine;

                if(header == lastHeader)
                {
                    tempVal += Environment.NewLine;
                }
            }

            this.Message = tempVal + this.Message;
        }

        private void WriteFile()
        {
            XmlSerializer serializer = new XmlSerializer(typeof(Mail));
            {
                using (StringWriter sw = new StringWriter())
                {
                    XmlWriterSettings settings = new XmlWriterSettings
                    {
                        OmitXmlDeclaration = true,
                        Indent = true,
                        NewLineOnAttributes = true
                    };

                    using (XmlWriter writer = XmlWriter.Create(sw, settings))
                    {
                        serializer.Serialize(writer, this);
                        string xml = sw.ToString();

                        //Create directory if it doesn't exist
                        string dir = $"C:\\mails\\{this.RecipientList[0]}\\inbox";
                        string relativeDir = $"{this.RecipientList[0]}\\inbox";
                        Directory.CreateDirectory(dir);
                        string filename = $"{this.MessageId}.xml";
                        string path = dir + $"\\{filename}";

                        //Write the file
                        using (StreamWriter outputWriter = new StreamWriter(path))
                        {
                            outputWriter.Write(xml);
                        }

                        //Get recipients encryption key
                        UserTable ut = new UserTable();
                        Models.User user = ut.GetUserByEmailAddress(this.RecipientList[0]);

                        //Encrypt message
                        Encryptor encryptor = new Encryptor(relativeDir + "\\" + filename);
                        encryptor.EncryptXmlMessage(user.EncryptionKey);
                        encryptor.SaveEncryptedMessage();
                    }
                }
            }
        }
    }
}
