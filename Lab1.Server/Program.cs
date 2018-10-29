using System;

namespace Lab1.Server
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Server");
            Server.StartListening();
            Console.ReadLine();
        }
    }
}
