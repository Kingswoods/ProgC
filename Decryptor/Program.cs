using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Decryptor
{
    class Program
    {
        static void Main(string[] args)
        {
            //Output help
            if(args.Length == 0 || args[0] == "help" || args[0] == "h")
            {
                Console.WriteLine("Usage: Decryptor.exe <email> <folder> <message-id> <encryption-key>");
                Console.WriteLine("Example: Decryptor.exe mg@experimentum.xyz inbox 607e124527885a5187b388dd3404d589 661c59c01660c710293bc6280879effd");
                Console.ReadKey();
                Environment.Exit(0);
            }

            //Save old console stream
            TextWriter oldOut = Console.Out;

            //Kill output
            Console.SetOut(new StreamWriter(Stream.Null));

            string path = $"{args[0]}\\{args[1]}\\{args[2]}.xml";

            Encryptor encryptor = new Encryptor(path);

            encryptor.DecryptXmlMessage(args[3]);

            //Restore original console stream
            Console.SetOut(oldOut);
            Console.Write(encryptor.DecryptedText);
        }
    }
}
