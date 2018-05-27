using System;

namespace MailServer.Exceptions
{
    public class NoSenderException : Exception
    {
        public NoSenderException()
        {
        }

        public NoSenderException(string message) : base(message)
        {
        }

        public NoSenderException(string message, Exception inner) : base(message, inner)
        {
        }

    }
}
