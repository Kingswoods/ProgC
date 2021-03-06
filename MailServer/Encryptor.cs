﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace MailServer
{
    class Encryptor
    {
        string XmlFile = "C:\\mails\\";     //Path to mail directory
        XDocument XmlDocument;

        byte[] MailMessageEncrypted;        //Encrypted message
        string MailMessagePlainText;        //Plaintext message

        //Bytes
        byte[] IV;
        byte[] Key;

        string EncryptedText;
        public string DecryptedText { get; private set; }
        string IvText;

        public Encryptor(string xmlFile)
        {
            try
            {
                this.XmlFile += xmlFile;
                this.XmlDocument = XDocument.Load(this.XmlFile);

                if(this.XmlDocument.Root.Element("IsEncrypted").Value == "true")
                {
                    MailMessageEncrypted = this.XmlDocument.Root.Element("Message").Value.Split().Select(i => byte.Parse(i)).ToArray();
                    IV = this.XmlDocument.Root.Element("SecretIV").Value.Split().Select(i => byte.Parse(i)).ToArray();
                }
                else if(this.XmlDocument.Root.Element("IsEncrypted").Value == "false")
                {
                    MailMessagePlainText = this.XmlDocument.Root.Element("Message").Value;
                }
            }
            catch(Exception e)
            {
                Console.WriteLine("Error: " + e);
            }
        }

        public void EncryptXmlMessage(string userKey)
        {
            try
            {
                using(AesManaged aes = new AesManaged())
                {
                    Key = ASCIIEncoding.UTF8.GetBytes(userKey);     //Users key, 32 byte array (256 bit)
                    IV = aes.IV;                                    //Autogenerated IV (Initialization vector), 16 byte array

                    //Convert IV values to IV Text
                    foreach(int i in this.IV)
                    {
                        IvText += i.ToString() + " ";
                    }

                    //Create the encryptor to perform the stream transform
                    ICryptoTransform encryptor = aes.CreateEncryptor(Key, IV);

                    //Create streams for encryption
                    using(MemoryStream msEncrypt = new MemoryStream())
                    {
                        using(CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                        {
                            using(StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                            {
                                //Write the cipher to stream
                                swEncrypt.Write(this.MailMessagePlainText);
                            }

                            this.MailMessageEncrypted = msEncrypt.ToArray();
                        }
                    }

                    //Cipher values to Encrypted Text
                    foreach(int i in this.MailMessageEncrypted)
                    {
                        EncryptedText += i.ToString() + " ";
                    }
                }
            }
            catch(Exception e)
            {
                Console.WriteLine("Error: " + e);
            }

        }

        public void DecryptXmlMessage(string userKey)
        {
            try
            {
                using (AesManaged aes = new AesManaged())
                {
                    Key = ASCIIEncoding.UTF8.GetBytes(userKey);

                    //Convert IV values to IV Text
                    foreach (int i in this.IV)
                    {
                        IvText += i.ToString() + " ";
                    }

                    //Cipher values to Encrypted Text
                    foreach (int i in this.MailMessageEncrypted)
                    {
                        EncryptedText += i.ToString() + " ";
                    }

                    //Create the encryptor to perform the stream transform
                    ICryptoTransform decryptor = aes.CreateDecryptor(Key, IV);

                    //Create streams for decryption
                    using (MemoryStream msDecrypt = new MemoryStream(MailMessageEncrypted))
                    {
                        using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                        {
                            using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                            {
                                //Read decrypted bytes from the decryption stream
                                DecryptedText = srDecrypt.ReadToEnd();
                            }
                        }
                    }

                }
            }
            catch(Exception e)
            {
                Console.WriteLine("Error: " + e);
            }
        }

        public void SaveEncryptedMessage()
        {
            try
            {
                this.XmlDocument.Root.Element("Message").Value = EncryptedText.TrimEnd();
                this.XmlDocument.Root.Element("SecretIV").Value = IvText.TrimEnd();
                this.XmlDocument.Root.Element("IsEncrypted").Value = "true";
                this.XmlDocument.Save(this.XmlFile);
            }
            catch(Exception e)
            {
                Console.WriteLine("Error: " + e);
            }
        }

        public void SaveDecryptedMessage()
        {
            try
            {
                this.XmlDocument.Root.Element("Message").Value = DecryptedText;
                this.XmlDocument.Root.Element("SecretIV").Value = "";
                this.XmlDocument.Root.Element("IsEncrypted").Value = "false";
                this.XmlDocument.Save(this.XmlFile);
            }
            catch(Exception e)
            {
                Console.WriteLine("Error: " + e);
            }
        }
    }
}
