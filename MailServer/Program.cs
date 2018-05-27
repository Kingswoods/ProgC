using System;
using System.Net;
using System.Threading;

namespace MailServer
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            AppDomain domain = AppDomain.CreateDomain("experimentum.xyz");

            Program program = new Program();
            program.RunServer();

            AppDomain.Unload(domain);
        }

        private void RunServer()
        {
            SmtpListener listener = null;

            do
            {
                Console.WriteLine("New Mail Listener Thread started");
                listener = new SmtpListener(this, IPAddress.Loopback, 25);
                listener.Start();

                while (listener.IsThreadAlive)
                {
                    Thread.Sleep(500);
                }
            }
            while (listener != null);
        }
    }
}
