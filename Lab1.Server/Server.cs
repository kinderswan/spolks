using System;
using System.Collections.Generic;
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

        public static void StartListening()
        {
            // Data buffer for incoming data.  
            byte[] bytes = new Byte[1024];

            // Establish the local endpoint for the socket.  
            // Dns.GetHostName returns the name of the   
            // host running the application.  
            IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
            IPAddress ipAddress = ipHostInfo.AddressList[0];
            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, 11000);

            // Create a TCP/IP socket.  
            listener = new Socket(ipAddress.AddressFamily,
                SocketType.Stream, ProtocolType.Tcp);

            // Bind the socket to the local endpoint and   
            // listen for incoming connections.  
            try
            {
                listener.Bind(localEndPoint);
                listener.Listen(10);

                // Start listening for connections.  
                while (true)
                {
                    Console.WriteLine("Waiting for a connection...");
                    // Program is suspended while waiting for an incoming connection.  
                    handler = listener.Accept();
                    data = null;
                    var fileMetaData = new List<byte>();
                    var filename = string.Empty;
                    long fileSize = 0;

                    // An incoming connection needs to be processed.  
                    while (true)
                    {
                        int bytesRec = handler.Receive(bytes);
                        data += Encoding.ASCII.GetString(bytes, 0, bytesRec);
                        if (data.IndexOf("\r\n") > -1)
                        {
                            if (data == "ECHO\r\n")
                            {
                                byte[] msg = Encoding.ASCII.GetBytes(lastUserCommand);

                                handler.Send(msg);
                            }
                            else if (data == "CLOCK\r\n")
                            {
                                byte[] msg = Encoding.ASCII.GetBytes(DateTime.Now.ToLongDateString());

                                handler.Send(msg);
                            }
                            else if (data == "CLOSE\r\n")
                            {
                                CloseConnection();
                            }
                            else if (data.StartsWith("file"))
                            {

                                filename = data.Split(':')[1];
                                fileSize = Int64.Parse(data.Split(':')[2]);

                            }
                            else if(filename != "")
                            {
                                if(fileMetaData.Count < fileSize)
                                {
                                    fileMetaData.AddRange(bytes.Take(bytesRec));
                                    continue;
                                } else
                                {
                                    File.WriteAllBytes(filename, fileMetaData.ToArray());
                                    filename = string.Empty;
                                    fileSize = 0;
                                    fileMetaData = null;
                                }
                            }
                            else
                            {
                                byte[] msg = Encoding.ASCII.GetBytes("200");

                                handler.Send(msg);
                            }


                            // Show the data on the console.  
                            Console.WriteLine("Text received : {0}", data);
                            lastUserCommand = data;
                            data = string.Empty;
                        }
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

        public static void CloseConnection()
        {
            handler.Shutdown(SocketShutdown.Both);
            handler.Close();
        }

    }
}
