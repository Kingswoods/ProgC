using System;

namespace MailServer.Exceptions
{
    public class InvalidRecipientException : Exception
    {
        public InvalidRecipientException()
        {
        }

        public InvalidRecipientException(string message) : base(message)
        {
        }

        public InvalidRecipientException(string message, Exception inner) : base(message, inner)
        {
        }

    }
}
