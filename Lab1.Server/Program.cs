using System;

namespace Lab1.Server
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Server");
            Server.StartListening(args.Length > 0 ? args[0] : "127.0.0.1");
            Console.ReadLine();
        }
    }
}
