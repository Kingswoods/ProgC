using System.Data;
using System.Xml.Linq;
using MySql.Data.MySqlClient;

namespace MailServer.Database
{
    public abstract class Database
    {
        protected MySqlConnection Conn { get; private set; }

        private string ConnectionString;

        protected string TableName;

        public Database()
        {
            XDocument XmlDbConf = XDocument.Load("C:\\prog\\db_config.xml");

            string host = XmlDbConf.Root.Element("host").Value;
            string user = XmlDbConf.Root.Element("user").Value;
            string pass = XmlDbConf.Root.Element("password").Value;
            string db = XmlDbConf.Root.Element("db").Value;

            this.Conn = new MySqlConnection();
            this.ConnectionString = $"server={host};uid={user};pwd={pass};database={db};";
            this.Conn.ConnectionString = this.ConnectionString;
            Connect();
        }

        ~Database()
        {
            if(this.Conn.State == ConnectionState.Open)
            {
                Disconnect();
            }
        }

        private void Connect()
        {
            this.Conn.Open();
        }

        public void Disconnect()
        {
            this.Conn.Close();
        }

        protected MySqlDataReader Query(MySqlCommand cmd)
        {
            return cmd.ExecuteReader();
        }
    }
}
