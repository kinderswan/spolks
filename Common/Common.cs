using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Xml.Serialization;

namespace Common
{
    public class Common
    {
    }

    public class Package
    {
        public string Message { get; set; }

        public byte[] File { get; set; }
    }

    public static class Extension
    {
        public static T Deserialize<T>(this string toDeserialize)
        {
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(T));
            using (StringReader textReader = new StringReader(toDeserialize))
            {
                return (T)xmlSerializer.Deserialize(textReader);
            }
        }

        public static string Serialize<T>(this T toSerialize)
        {
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(T));
            using (StringWriter textWriter = new StringWriter())
            {
                xmlSerializer.Serialize(textWriter, toSerialize);
                return textWriter.ToString();
            }
        }
    }


    public class UDPSocket
    {
        private readonly int listenPort = 0;
        private readonly int sendingPort = 0;

        private readonly string ipAddress = null;

        private UdpClient listener = null;

        private IPEndPoint receivingEp = null;

        private Socket socket = null;

        private IPAddress broadcast = null;

        private IPEndPoint sendingEp = null;

        public UDPSocket(int listenPort, int sendingPort, string address)
        {
            this.listenPort = listenPort;
            this.sendingPort = sendingPort;
            ipAddress = address;
            listener = new UdpClient(listenPort);
            receivingEp = new IPEndPoint(IPAddress.Any, listenPort);
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            broadcast = IPAddress.Parse(this.ipAddress);

            sendingEp = new IPEndPoint(broadcast, sendingPort);
        }

        public void StartListening()
        {
            try
            {
                while (true)
                {
                    Console.WriteLine("Waiting for broadcast");
                    byte[] bytes = listener.Receive(ref receivingEp);

                    Console.WriteLine($"Received broadcast from {receivingEp} :");
                    Console.WriteLine($" {Encoding.ASCII.GetString(bytes, 0, bytes.Length)}");

                    socket.SendTo(Encoding.ASCII.GetBytes("received"), sendingEp); // send that we have received the data
                }
            }
            catch (SocketException e)
            {
                Console.WriteLine(e);
            }
            finally
            {
                listener.Close();
            }
        }

        public void StartListeningToFile()
        {
            try
            {
                var totalChunks = new List<UDPFileChunk>();

                var parser = new UDPdgramFileParser();

                var watch = Stopwatch.StartNew();
                while (true)
                {
                    byte[] bytes = listener.Receive(ref receivingEp);
                    var encodedPackage = Encoding.ASCII.GetString(bytes, 0, bytes.Length);
                    if (encodedPackage.Contains("complete"))
                    {
                        var bitrate = watch.ElapsedMilliseconds;
                        Console.WriteLine($"bitrate = {totalChunks.Select(x => x.Data.Length).Sum() / bitrate}");
                        parser.WriteFile(totalChunks, encodedPackage.Split(':')[1]);
                        Console.WriteLine("Completed");
                        break;
                    }

                    var fileChunk = encodedPackage.Deserialize<UDPFileChunk>();

                    if (totalChunks.Any(x => x.NumberOfChunk == fileChunk.NumberOfChunk))
                    {
                        totalChunks.ForEach(x =>
                        {
                            if (x.NumberOfChunk == fileChunk.NumberOfChunk)
                            {
                                x = fileChunk; // replace it
                            }
                        });
                    }
                    else
                    {
                        totalChunks.Add(fileChunk);
                    }

                    socket.SendTo(Encoding.ASCII.GetBytes(Int64.Parse($"{fileChunk.NumberOfChunk}{parser.GetHashSum(fileChunk.Data)}").ToString()), sendingEp); // send that we have received the data
                }
            }
            catch (SocketException e)
            {
                Console.WriteLine(e);
            }
            finally
            {
                listener.Close();
            }
        }

        public byte[] SendMessage(byte[] message)
        {
            socket.SendTo(message, sendingEp);
            return listener.Receive(ref receivingEp);
        }
    }


    public class UDPFileChunk
    {
        public long HashSumTotal { get; set; }

        public long HashSumChunk { get; set; }

        public int NumberOfChunk { get; set; }

        public int TotalChunks { get; set; }

        public byte[] Data { get; set; }
    }

    public class UDPdgramFileParser
    {
        private string filename;

        public UDPdgramFileParser(string filename)
        {
            this.filename = filename;
        }

        public UDPdgramFileParser()
        {
            this.filename = "";
        }

        public void WriteFile(List<UDPFileChunk> chunks, string filename)
        {
            var orderedCollection = chunks.OrderBy(x => x.NumberOfChunk).ToList();
            var bytes = orderedCollection.SelectMany(x => x.Data).ToArray();

            File.WriteAllBytes(filename, bytes);

        }

        public List<UDPFileChunk> GetFileChunks()
        {
            var maxSizeOfChunk = 45000;

            var fileBytes = File.ReadAllBytes(filename);

            var numberOfChunks = (int)Math.Ceiling(fileBytes.Length / (double)maxSizeOfChunk);

            var list = new List<UDPFileChunk>();

            var totalhashsum = GetHashSum(fileBytes);

            for (var i = 0; i < numberOfChunks; i++)
            {
                var data = fileBytes.Skip(i * maxSizeOfChunk).Take(maxSizeOfChunk).ToArray();
                list.Add(new UDPFileChunk
                {
                    Data = data,
                    HashSumChunk = Int64.Parse($"{i}{GetHashSum(data)}"),
                    NumberOfChunk = i,
                    TotalChunks = numberOfChunks,
                    HashSumTotal = totalhashsum
                });
            }

            return list;
        }

        public long GetHashSum(byte[] bytes)
        {
            return bytes.Select(x => (long)x).Aggregate((acc, x) => acc + x);
        }
    }
}
