using Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Lab1.Server
{
    public class Server
    {
        // Incoming data from the client.  
        public static string data = null;

        private static string lastUserCommand = "";

        private static Socket listener = null;

        private static Socket handler = null;

        private static Stopwatch watch = null;

        public static void StartListening()
        {
            // Data buffer for incoming data.  
            byte[] bytes = new Byte[2048];

            // Establish the local endpoint for the socket.  
            // Dns.GetHostName returns the name of the   
            // host running the application.  
            IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
            IPAddress ipAddress = ipHostInfo.AddressList[0];
            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, 11000);

            // Create a TCP/IP socket.  
            listener = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            // Bind the socket to the local endpoint and   
            // listen for incoming connections.  
            try
            {
                listener.Bind(localEndPoint);
                listener.Listen(0);
                Console.WriteLine("Waiting for a connection...");
                // Program is suspended while waiting for an incoming connection.  
                handler = listener.Accept();
                data = null;

                // An incoming connection needs to be processed.  
                while (true)
                {
                    if (watch == null)
                    {
                        watch = Stopwatch.StartNew();
                    }
                    int bytesRec = handler.Receive(bytes);
                    data += Encoding.ASCII.GetString(bytes, 0, bytesRec);
                    if (bytesRec < bytes.Length)
                    {
                        var bitrate = watch.ElapsedMilliseconds;
                        Console.WriteLine($"bitrate = {data.Length / bitrate}");
                        DoOnReceive(data);
                        data = null;
                    }

                }


            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            Console.WriteLine("\nPress ENTER to continue...");
            Console.Read();

        }

        private static void DoOnReceive(string data)
        {
            var receivedPackage = data.Deserialize<Package>();

            if (receivedPackage.Message == "ECHO")
            {
                byte[] msg = Encoding.ASCII.GetBytes(lastUserCommand);

                handler.Send(msg);
            }
            else if (receivedPackage.Message == "CLOCK")
            {
                byte[] msg = Encoding.ASCII.GetBytes(DateTime.Now.ToLongDateString());

                handler.Send(msg);
            }
            else if (receivedPackage.Message == "CLOSE")
            {
                CloseConnection();
            }
            else if (receivedPackage.Message.Contains("DOWNLOAD"))
            {
                var fileName = receivedPackage.Message.Split(":")[1];

                var file = File.ReadAllBytes(fileName);

                var p = new Package
                {
                    File = file,
                    Message = fileName
                };

                byte[] msg = Encoding.ASCII.GetBytes(p.Serialize());

                handler.Send(msg);
            }
            else if (receivedPackage.File != null)
            {
                File.WriteAllBytes(receivedPackage.Message, receivedPackage.File);

                byte[] msg = Encoding.ASCII.GetBytes($"{receivedPackage.Message} has been uploaded");

                handler.Send(msg);
            }
            else
            {
                byte[] msg = Encoding.ASCII.GetBytes("200");

                handler.Send(msg);
            }


            // Show the data on the console.  
            Console.WriteLine("Text received : {0}", receivedPackage.Message);
            lastUserCommand = receivedPackage.Message;

        }

        public static void CloseConnection()
        {
            handler.Shutdown(SocketShutdown.Both);
            handler.Close();
        }

    }
}
