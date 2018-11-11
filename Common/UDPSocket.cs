using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Common
{
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


        public void Start()
        {
            try
            {
                Console.WriteLine("Waiting for broadcast");
                while (true)
                {
                    byte[] bytes = listener.Receive(ref receivingEp);
                    var encodedPackage = Encoding.ASCII.GetString(bytes, 0, bytes.Length);
                    var decodedPackage = encodedPackage.Deserialize<UDPDataChunk>();
                    if (decodedPackage.Type == "message")
                    {
                        HandleMessage(decodedPackage);
                    }
                    if (decodedPackage.Type == "file")
                    {
                        if (previousMessage.Contains("DOWNLOAD"))
                        {

                            HandleFile(decodedPackage);
                        }

                        if (previousMessage.Contains("UPLOAD"))
                        {
                            HandleFile(decodedPackage);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private string previousMessage = "";

        private void HandleMessage(UDPDataChunk chunk)
        {
            var message = Encoding.ASCII.GetString(chunk.Data, 0, chunk.Data.Length);
            Console.WriteLine($"Received message: {message}");

            if (message.Contains("DOWNLOAD") || message.Contains("UPLOAD"))
            {
                var split = message.Split(':');
                previousMessage = split[0];
                previousFileName = split[1];
                socket.SendTo(Encoding.ASCII.GetBytes(message), sendingEp);
                return;
            }

            string dataBack = "";

            switch (message)
            {
                case "CLOCK":
                    {
                        dataBack = DateTime.Now.ToLongDateString(); break;
                    }
                case "ECHO":
                    {
                        dataBack = previousMessage;
                        break;
                    }
                case "CLOSE":
                    {
                        this.socket.Close();
                        break;
                    }
                case "SAVE":
                    {
                        this.SaveFile();
                        break;
                    }
                default: dataBack = message; break;
            }

            socket.SendTo(Encoding.ASCII.GetBytes(dataBack), sendingEp);
            previousMessage = message;
        }

        private string previousFileName = "";

        private List<UDPDataChunk> filechunks = new List<UDPDataChunk>();

        private void HandleFile(UDPDataChunk chunk)
        {
            if (filechunks.Any(x => x.NumberOfChunk == chunk.NumberOfChunk))
            {
                var index = filechunks.FindIndex(x => x.NumberOfChunk == chunk.NumberOfChunk);
                filechunks[index] = chunk;
            }

            filechunks.Add(chunk);

            var backMessage = Encoding.ASCII.GetBytes(chunk.HashSumChunk.ToString());

            socket.SendTo(backMessage, sendingEp);
        }

        private void SaveFile()
        {
            var fileBytes = this.filechunks.OrderBy(x => x.NumberOfChunk).SelectMany(x => x.Data).ToArray();

            File.WriteAllBytes(this.previousFileName, fileBytes);

            socket.SendTo(Encoding.ASCII.GetBytes("DONE"), sendingEp);
        }

        public byte[] SendMessageFromClientToServer(string message)
        {
            var messageBytes = Encoding.ASCII.GetBytes(message);
            var parser = new UDPdgramDataParser();
            var messageChunk = parser.GetDataChunks(messageBytes, "message")[0];
            var serializedChunk = messageChunk.Serialize();
            var encoded = Encoding.ASCII.GetBytes(serializedChunk);
            socket.SendTo(encoded, sendingEp);
            return listener.Receive(ref receivingEp);
        }
               
        private bool SendFile(byte[] fileBytes)
        {
            var parser = new UDPdgramDataParser();
            var fileChunks = parser.GetDataChunks(fileBytes, "file");
            bool totalSend = false;
            int counter = 0;
            while (!totalSend)
            {
                if (SendFileChunk(fileChunks[counter]))
                {
                    counter++;
                    totalSend = counter == fileChunks.Count;
                    Console.WriteLine($"{counter} of {fileChunks.Count}");
                }
            }
            return true;
        }

        private bool SendFileChunk(UDPDataChunk chunk)
        {
            var serialized = chunk.Serialize();
            var encoded = Encoding.ASCII.GetBytes(serialized);
            socket.SendTo(encoded, sendingEp);
            var result = listener.Receive(ref receivingEp);
            var hashReceived = Encoding.ASCII.GetString(result, 0, result.Length);
            long longResult;

            long.TryParse(hashReceived, out longResult);

            return chunk.HashSumChunk == longResult;
        }

        public void SendFile(byte[] file, string message)
        {
            this.SendMessageFromClientToServer(message);
            var result = this.SendFile(file);
            this.SendMessageFromClientToServer("SAVE");

        }
    }
}
