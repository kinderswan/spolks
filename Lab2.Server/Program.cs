using Common;
using System;

namespace Lab2.Server
{
    class Program
    {
        static void Main(string[] args)
        {
            var udp = new UDPSocket(11001, 11000, "172.23.222.1");
            udp.StartListeningToFile();
            Console.ReadKey();
        }
    }
}
