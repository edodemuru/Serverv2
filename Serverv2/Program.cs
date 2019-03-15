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
                // set the TcpListener on port 8888
                int port = 8888;
                TcpListener server = new TcpListener(IPAddress.Any, port);

                // Start listening for client requests
                server.Start();


                // Buffer for reading data
                byte[] msgReceivedBytes = new byte[1024];
                string dataReceived;
                string dataToSend = "";
                string[] dataReceivedParsed;
                DateTime timestamp = DateTime.Now;
                DateTime timestampModified = timestamp.AddMinutes(1.00);

                //Enter the listening loop
                while (true)
                {
                    Console.Write("Waiting for a connection... ");

                    // Perform a blocking call to accept requests.
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
                        dataReceived = System.Text.Encoding.ASCII.GetString(msgReceivedBytes, 0, i);
                        dataReceivedParsed = dataReceived.Split(';');

                        Console.WriteLine(String.Format("Received: {0}", dataReceived));

                        //First Connection
                        if(dataReceivedParsed[1] == "R")
                        {
                            dataToSend = timestamp.ToString() + " ;" + timestampModified.ToString();
                            
                        }
                        else
                        {
                            dataToSend = timestamp.ToString();
                        }


                    }

                    if (dataToSend != "")
                    {
                        //Translate String to ASCII
                        byte[] dataToSendBytes = System.Text.Encoding.ASCII.GetBytes(dataToSend);

                        // Send back a response.
                        stream.Write(dataToSendBytes, 0, dataToSendBytes.Length);

                        Console.WriteLine(String.Format("Sent: {0}", dataToSend));
                    }
                    else
                    {
                        Console.WriteLine("Errore nessun dato inviato");
                    }                
                    

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


        public static DateTime GetNetworkTime()
        {
            //default Windows time server
            const string ntpServer = "time.windows.com";

            // NTP message size - 16 bytes of the digest (RFC 2030)
            var ntpData = new byte[48];

            //Setting the Leap Indicator, Version Number and Mode values
            ntpData[0] = 0x1B; //LI = 0 (no warning), VN = 3 (IPv4 only), Mode = 3 (Client Mode)

            var addresses = Dns.GetHostEntry(ntpServer).AddressList;

            //The UDP port number assigned to NTP is 123
            var ipEndPoint = new IPEndPoint(addresses[0], 123);
            //NTP uses UDP

            using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            {
                socket.Connect(ipEndPoint);

                //Stops code hang if NTP is blocked
                socket.ReceiveTimeout = 3000;

                socket.Send(ntpData);
                socket.Receive(ntpData);
                socket.Close();
            }

            //Offset to get to the "Transmit Timestamp" field (time at which the reply 
            //departed the server for the client, in 64-bit timestamp format."
            const byte serverReplyTime = 40;

            //Get the seconds part
            ulong intPart = BitConverter.ToUInt32(ntpData, serverReplyTime);

            //Get the seconds fraction
            ulong fractPart = BitConverter.ToUInt32(ntpData, serverReplyTime + 4);

            //Convert From big-endian to little-endian
            intPart = SwapEndianness(intPart);
            fractPart = SwapEndianness(fractPart);

            var milliseconds = (intPart * 1000) + ((fractPart * 1000) / 0x100000000L);

            //**UTC** time
            var networkDateTime = (new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc)).AddMilliseconds((long)milliseconds);

            return networkDateTime.ToLocalTime();
        }

        // stackoverflow.com/a/3294698/162671
        static uint SwapEndianness(ulong x)
        {
            return (uint)(((x & 0x000000ff) << 24) +
                           ((x & 0x0000ff00) << 8) +
                           ((x & 0x00ff0000) >> 8) +
                           ((x & 0xff000000) >> 24));
        }

    }
}

