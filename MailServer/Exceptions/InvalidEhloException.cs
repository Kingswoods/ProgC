using System;

namespace MailServer.Exceptions
{
    public class InvalidEhloException : Exception
    {
        public InvalidEhloException()
        {
        }

        public InvalidEhloException(string message) : base(message)
        {
        }

        public InvalidEhloException(string message, Exception inner) : base(message, inner)
        {
        }

    }
}
