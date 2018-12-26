using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;

namespace Serverv2
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                //Primo commit
                // set the TcpListener on port 8888
                int port = 8888;
                TcpListener server = new TcpListener(IPAddress.Any, port);

                // Start listening for client requests
                server.Start();


                // Buffer for reading data
                byte[] msgReceivedBytes = new byte[1024];
                string msgReceived;

                //Enter the listening loop
                while (true)
                {
                    Console.Write("Waiting for a connection... ");

                    // Perform a blocking call to accept requests.
                    // You could also user server.AcceptSocket() here.
                    TcpClient client = server.AcceptTcpClient();
                    Console.WriteLine("Connected!");

                    // Get a stream object for reading and writing
                    NetworkStream stream = client.GetStream();

                    int i;

                    // Loop to receive all the data sent by the client.
                    i = stream.Read(msgReceivedBytes, 0, msgReceivedBytes.Length);

                    if (i!=0)
                    {
                        // Translate data bytes to a ASCII string.
                        msgReceived = System.Text.Encoding.ASCII.GetString(msgReceivedBytes, 0, i);
                        Console.WriteLine(String.Format("Received: {0}", msgReceived));

                    }

                    
                        string msgToSend = "Data received";
                        //Translate String to ASCII
                        byte[] msgToSendBytes = System.Text.Encoding.ASCII.GetBytes(msgToSend);

                        // Send back a response.
                        stream.Write(msgToSendBytes, 0, msgToSendBytes.Length);
                    
                        Console.WriteLine(String.Format("Sent: {0}", msgToSend));

                    
                    

                        // Shutdown and end connection
                        client.Close();
                }
            }
            catch (SocketException e)
            {
                Console.WriteLine("SocketException: {0}", e);
            }


            Console.WriteLine("Hit enter to continue...");
            Console.Read();
        }
        
    }
}

