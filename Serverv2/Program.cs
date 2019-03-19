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
                //Data received through socket
                string dataReceived;
                //Data to send to esp 32
                string dataToSend = "";
                //Data parsed
                string[] dataReceivedParsed;
                //Number of esp32 devices
                int numEsp = 0;
                int numEspTemp = 0;
                //Timestamp necessary for sync
                DateTime timestamp = new DateTime();
                DateTime timestampModified = new DateTime();
                //Mac of esp32 connected (max 10)
                List<string> macEsp32 = new List<string>();


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
                        Console.WriteLine("Dati ricevuti: " + dataReceivedParsed[1] + " Fine dati");

                        //First Connection
                        if(dataReceivedParsed[1] == "R")
                        {
                            Console.WriteLine("First Connection");
                            //No esp32 connected
                            if(macEsp32.Count == 0)
                            {
                                //This is the first esp32 to connect to Server, so I calculate the timestamp
                                timestamp = GetNetworkTime();
                                timestampModified = timestamp.AddMinutes(1);
                                macEsp32.Add(dataReceivedParsed[0]);
                            }
                            //List has at least one element
                            else
                            {
                                //Mac is not inside list
                                if (!macEsp32.Contains(dataReceivedParsed[0]))
                                {
                                    macEsp32.Add(dataReceivedParsed[0]);
                                }
                                timestamp = GetNetworkTime();
                                timestampModified = timestamp.AddMinutes(1);
                            }                          
                            dataToSend = timestampModified.ToString();
                            
                        }
                        else
                        {
                            Console.WriteLine("Next Connection");
                            //First esp32 to connect after first connection
                            if (numEspTemp == 0)
                            {
                                timestamp = GetNetworkTime();
                                timestamp.AddMinutes(1);
                            }
                            //Last esp32
                            if(numEspTemp == macEsp32.Count)
                            {
                                numEspTemp = 0;
                            }
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

