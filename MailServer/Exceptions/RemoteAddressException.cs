using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MailServer.Exceptions
{
    public class RemoteAddressException : Exception
    {
        public RemoteAddressException()
        {
        }

        public RemoteAddressException(string message) : base(message)
        {
        }

        public RemoteAddressException(string message, Exception inner) : base(message, inner)
        {
        }

    }
}
