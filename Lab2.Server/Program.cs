using Common;
using System;

namespace Lab2.Server
{
    class Program
    {
        static void Main(string[] args)
        {
            var ip = args.Length > 0 ? args[0] : "127.0.0.1";
            var udp = new UDPSocket(11001, 11000, ip);
            udp.Start();

            Console.ReadKey();
        }
    }
}
