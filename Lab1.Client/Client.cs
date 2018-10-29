using Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Lab1.Client
{
    public class Client
    {

        private static Socket socket = null;

        public static void StartClient()
        {

        }

        public static void SendMessage(string message)
        {
            var package = new Package
            {
                Message = message
            };            

            byte[] msg = Encoding.ASCII.GetBytes(package.Serialize());
            Post(msg);
        }

        public static void SendFile(string filename)
        {
            var bytes = File.ReadAllBytes(filename);

            var package = new Package
            {
                Message = filename,
                File = bytes
            };

            byte[] msg = Encoding.ASCII.GetBytes(package.Serialize());

            Post(msg);
        }

        public static void DownloadFile(string filename)
        {
            var package = new Package
            {
                Message = filename
            };

            byte[] msg = Encoding.ASCII.GetBytes(package.Serialize());

            Post(msg);

            byte[] bytes = new Byte[2048];

            string data = null;

            while (true)
            {
                int bytesRec = socket.Receive(bytes);
                data += Encoding.ASCII.GetString(bytes, 0, bytesRec);
                if (bytesRec < bytes.Length)
                {
                    var response = data.Deserialize<Package>();
                    File.WriteAllBytes(response.Message, response.File);
                    data = null;
                    break;
                }

            }
        }

        private static void Post(byte[] toSend)
        {
            int bytesSent = socket.Send(toSend);

            var buffer = new byte[1024];

            int bytesRec = 0;

            try
            {
                bytesRec = socket.Receive(buffer);
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex);
            }


            Console.WriteLine("Server says: {0}",
                Encoding.ASCII.GetString(buffer, 0, bytesRec));

        }

        public static void OpenConnection()
        {
            try
            {
                // Establish the remote endpoint for the socket.  
                // This example uses port 11000 on the local computer.  
                IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
                IPAddress ipAddress = ipHostInfo.AddressList[0];
                IPEndPoint remoteEP = new IPEndPoint(ipAddress, 11000);

                // Create a TCP/IP  socket.  
                socket = new Socket(ipAddress.AddressFamily,
                    SocketType.Stream, ProtocolType.Tcp);

                // Connect the socket to the remote endpoint. Catch any errors.  
                try
                {
                    socket.Connect(remoteEP);

                    Console.WriteLine("Socket connected to {0}",
                        socket.RemoteEndPoint.ToString());

                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }


            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        public static void CloseConnection()
        {
            try
            {
                // Release the socket.  
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

    }
}
