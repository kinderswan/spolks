using Common;
using System;
using System.Collections.Generic;
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
            var udp = new UDPSocket(11000, 11001, "172.23.222.1");
            SendFile(udp);
            Console.ReadKey();

        }

        private static void SendMessage(UDPSocket udp)
        {
            while (true)
            {
                var x = Console.ReadLine();
                var result = udp.SendMessage(Encoding.ASCII.GetBytes(x));
                Console.WriteLine($" {Encoding.ASCII.GetString(result, 0, result.Length)}");
            }
        }

        private static void SendFile(UDPSocket udp)
        {
            var parser = new UDPdgramFileParser("1.pdf");
            var fileChunks = parser.GetFileChunks();
            var serializedChunks = fileChunks.Select(x => x.Serialize()).ToList();
            var hashReceived = new List<long>();
            fileChunks.ForEach(x => SendChunk(x, udp, hashReceived));

            if (!ConfirmReceived(fileChunks, hashReceived))
            {
                var notReceived = new Dictionary<UDPFileChunk, bool>();

                fileChunks.ForEach(x =>
                {
                    notReceived.Add(x, hashReceived.Contains(x.HashSumChunk));
                });

                notReceived.Where(x => !x.Value).Select(x => x.Key).ToList().ForEach(x => SendChunk(x, udp, new List<long>()));
            }

            udp.SendMessage(Encoding.ASCII.GetBytes("complete:1.pdf"));

        }

        private static void SendChunk(UDPFileChunk chunk, UDPSocket udp, List<long> hashReceived)
        {
            var result = udp.SendMessage(Encoding.ASCII.GetBytes(chunk.Serialize()));
            var res = Encoding.ASCII.GetString(result, 0, result.Length);


            hashReceived.Add(Int64.Parse(res));
        }

        private static bool ConfirmReceived(List<UDPFileChunk> original, List<long> received)
        {
            return original.Count == received.Count && original.Select(x => x.HashSumChunk).All(x => received.Contains(x));
        }
    }
}

