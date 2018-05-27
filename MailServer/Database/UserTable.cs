using MailServer.Exceptions;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MailServer.Database
{
    public class UserTable : Database
    {
        public UserTable()
        {
            this.TableName = "users";
        }

        public Models.User GetUserById(int id)
        {
            MySqlCommand cmd = new MySqlCommand($"SELECT * FROM {TableName} WHERE id=@id LIMIT 1", this.Conn);
            cmd.Parameters.AddWithValue("@id", id);
            cmd.Prepare();
            MySqlDataReader result = Query(cmd);

            while(result.Read())
            {
                return new Models.User(id, (string)result["first_name"], (string)result["last_name"], (string)result["email"], (string)result["password"], (string)result["encryption_key"]);
            }

            throw new NotFoundException("Could not get User with GetUserById");
        }

        public bool CheckIfEmailExists(string emailAddress)
        {
            MySqlCommand cmd = new MySqlCommand($"SELECT COUNT(*) AS count FROM {TableName} WHERE email=@email", this.Conn);
            cmd.Parameters.AddWithValue("@email", emailAddress);
            cmd.Prepare();
            MySqlDataReader result = Query(cmd);

            while(result.Read())
            {
                return Convert.ToInt32(result["count"]) > 0;
            }

            return false;
        }

        public Models.User GetUserByEmailAddress(string emailAddress)
        {
            MySqlCommand cmd = new MySqlCommand($"SELECT * FROM {TableName} WHERE email=@email LIMIT 1", this.Conn);
            cmd.Parameters.AddWithValue("@email", emailAddress);
            cmd.Prepare();
            MySqlDataReader result = Query(cmd);

            while(result.Read())
            {
                return new Models.User(Convert.ToInt32(result["id"]), (string)result["first_name"], (string)result["last_name"], (string)result["email"], (string)result["password"], (string)result["encryption_key"]);

            }

            throw new NotFoundException("Could not get User with GetUserByEmailAddress");
        }
    }
}
