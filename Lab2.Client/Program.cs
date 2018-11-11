using Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Lab2.Client
{
    class Program
    {
        static void Main(string[] args)
        {
            var ip = args.Length > 0 ? args[0] : "127.0.0.1";

            var udp = new UDPSocket(11000, 11001, ip);

            while (true)
            {
                var x = Console.ReadLine();

                if(x.Contains("UPLOAD"))
                {
                    SendFile(udp, x);
                }
                if (x.Contains("DOWNLOAD"))
                {
                    ReceiveFile(udp, x);
                }
                else
                {
                    var result = SendMessage(udp, x);
                    Console.WriteLine(result);
                }
            }
        }

        private static void ReceiveFile(UDPSocket udp, string x)
        {

        }

        private static string SendMessage(UDPSocket udp, string message)
        {           
            var result = udp.SendMessageFromClientToServer(message);
            return Encoding.ASCII.GetString(result, 0, result.Length);
        }

        private static void SendFile(UDPSocket udp, string message)
        {
            var fileName = message.Split(':')[1];
            var bytes = File.ReadAllBytes(fileName);
            udp.SendFile(bytes, message);
        }
    }
}

