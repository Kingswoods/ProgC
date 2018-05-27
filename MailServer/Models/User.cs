using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MailServer.Models
{
    public class User
    {
        public int? Id { get; private set; }
        public string FirstName { get; private set; }
        public string LastName { get; private set; }
        public string FullName { get; private set; }
        public string Email { get; private set; }
        public string Password { get; private set; }
        public string EncryptionKey { get; private set; }

        public User() {}

        public User(int? id, string firstName, string lastName, string email, string password, string encryptionKey)
        {
            this.Id = id;
            this.FirstName = firstName;
            this.LastName = lastName;
            this.FullName = firstName + " " + lastName;
            this.Email = email;
            this.Password = password;
            this.EncryptionKey = encryptionKey;
        }
    }
}
