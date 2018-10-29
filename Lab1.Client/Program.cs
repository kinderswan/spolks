using System;
using System.Threading;

namespace Lab1.Client
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Client");
            Client.OpenConnection();
            while (true)
            {

                var cmd = Console.ReadLine();

                if (cmd.Contains("upload"))
                {
                    var fileName = cmd.Split(':')[1];
                    Client.SendFile(fileName);
                }

                Client.SendMessage(cmd);



                if (cmd == "exit")
                {
                    break;
                }


            }



            Client.CloseConnection();

            Console.ReadLine();
        }
    }
}
