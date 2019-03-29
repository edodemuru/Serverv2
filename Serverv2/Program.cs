using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;


using MySql.Data.MySqlClient;
using System.IO;

namespace Serverv2
{


    class Program
    {

        static void Main(string[] args)
        {
            /*Packet pk = new Packet();
            pk.Hash = "hash prova";
            pk.Id = 0;
            pk.MacEsp32 = "macesp32prova";
            pk.MacSource = "macsourceprova";
            pk.Rssi = 100;
            pk.Ssid = "ssidprova";
            pk.Timestamp = "12/01/95";

            String conn = "Server=localhost;Database=PacketDB;Uid=root;Pwd=";

            try
            {
                PacketFactory instance = PacketFactory.Instance;
                instance.ConnectionString = conn;
                instance.InsertPacket(pk);
            }catch(MySqlException e)
            {
                Console.WriteLine(e.ToString());
            }*/
            try
            {
                // set the TcpListener on port 8888
                int port = 8888;
                TcpListener server = new TcpListener(IPAddress.Any, port);

                // Start listening for client requests
                server.Start();


                // Buffer for reading data
                //byte[] msgReceivedBytes = new byte[1024];
                //Data received through socket
                string dataReceived = "";
                //Data to send to esp 32
                string dataToSend = "";
                //Data parsed
                string[] dataReceivedParsed;
                //Number of esp32 devices
                int numEspTemp = 0;
                //Timestamp necessary for sync
                DateTime timestamp = new DateTime();
                DateTime timestampModified = new DateTime();
                //Mac of esp32 connected (max 10)
                List<string> macEsp32 = new List<string>();

                //Get last id from db
                int lastIdDatabase = PacketFactory.Instance.getPacketMaxId() + 1;


                //Enter the listening loop
                while (true)
                {
                    Console.Write("Waiting for a connection... ");

                    // Perform a blocking call to accept requests.
                    TcpClient client = server.AcceptTcpClient();
                    Console.WriteLine("Connected!");

                    // Get a stream object for reading and writing
                    NetworkStream stream = client.GetStream();

                    //int i = 0;
                    //int i;
                    // Loop to receive all the data sent by the client.
                    //i = stream.Read(msgReceivedBytes, 0, msgReceivedBytes.Length);
                    //dataReceived = ReadData(stream);
                    if (stream.CanRead)
                    {
                        int totalNum = 0;
                        byte[] myReadBuffer = new byte[1024*1024];
                        StringBuilder myCompleteMessage = new StringBuilder();
                        int numberOfBytesRead = 0;
                        
                        // Incoming message may be larger than the buffer size. 
                        do
                        {
                            Console.WriteLine("Lettura");
                            numberOfBytesRead = stream.Read(myReadBuffer, 0, myReadBuffer.Length);
                            totalNum += numberOfBytesRead;
                            myCompleteMessage.AppendFormat("{0}", Encoding.ASCII.GetString(myReadBuffer, 0, numberOfBytesRead));

                        }
                        while (stream.DataAvailable);
                        dataReceived = myCompleteMessage.ToString();
                        Console.WriteLine("Dimensione dati: " + totalNum);
                    }
                    else
                    {
                        Console.WriteLine("Sorry.  You cannot read from this NetworkStream.");
                    }

                    /*do
                    {
                        msgReceivedBytes = ReadLine(stream, 1024);
                        if (msgReceivedBytes != null && msgReceivedBytes.Length != 0)
                        {
                            i += msgReceivedBytes.Length;
                            dataReceived = dataReceived + System.Text.Encoding.ASCII.GetString(msgReceivedBytes, 0, msgReceivedBytes.Length);
               
                        }
                    } while (stream.DataAvailable);*/

                    if (dataReceived!= null)
                    {
                        // Translate data bytes to a ASCII string.
                        //dataReceived = System.Text.Encoding.ASCII.GetString(msgReceivedBytes, 0, i);
                        dataReceivedParsed = dataReceived.Split(';');

                        Console.WriteLine(String.Format("Received: {0}", dataReceived));

                        //First Connection
                        if(dataReceivedParsed[1] == "R")
                        {
                            //No esp32 connected
                            if(macEsp32.Count == 0)
                            {
                                //This is the first esp32 to connect to Server, so I calculate the timestamp
                                Console.WriteLine("First Connection");
                                timestamp = GetNetworkTime();
                                Console.WriteLine("Current time " + timestamp.ToString());
                                timestampModified = timestamp.AddMinutes(1);
                                Console.WriteLine("Time when esp32 will start working " + timestampModified.ToString());
                                macEsp32.Add(dataReceivedParsed[0]);
                            }
                            //List has at least one element
                            else
                            {
                                Console.WriteLine("Other Connection");
                                //Mac is not inside list
                                if (!macEsp32.Contains(dataReceivedParsed[0]))
                                {
                                    macEsp32.Add(dataReceivedParsed[0]);

                                }
                                //Mac is inside list
                                else
                                {
                                    Console.WriteLine("Esp32 riconnected");
                                    timestamp = GetNetworkTime();
                                    timestampModified = timestamp.AddMinutes(1.5);
                                    numEspTemp = 1;
                                }
                            }                          
                            dataToSend = timestampModified.ToString();
                            Console.WriteLine("Esp32 Connected:");
                            foreach (var item in macEsp32)
                            {
                                Console.WriteLine(item.ToString());
                            }

                        }
                        else
                        {
                            Console.WriteLine("Next Connection");
                            //First esp32 to connect after first connection
                            if (numEspTemp == 0)
                            {
                                Console.WriteLine("First esp32 to connect after first connection");
                                timestamp = GetNetworkTime();
                                Console.WriteLine("Current time " + timestamp.ToString());
                                timestampModified = timestamp.AddMinutes(1);
                                Console.WriteLine("Time when esp32 will start working " + timestampModified.ToString());

                            }
                            numEspTemp++;
                            //Last esp32
                            if(numEspTemp == macEsp32.Count)
                            {
                                Console.WriteLine("Last esp32 to connect after first connection");
                                numEspTemp = 0;
                            }
                            dataToSend = timestampModified.ToString();

                            Console.WriteLine("Esp32 Connected:");
                            foreach (var item in macEsp32)
                            {
                                Console.WriteLine(item.ToString());
                            }

                            //Packets Managment
                            foreach(String packetData in dataReceivedParsed)
                            {
                                //Mac information must not be memorized into db
                                if (packetData.Equals(dataReceivedParsed[0]) || packetData.Length == 0)
                                    continue;
                                Packet packet = new Packet();
                                String[] packetDataParsed = packetData.Split(',');
                                packet.Ssid = packetDataParsed[0];
                                packet.Channel = Int32.Parse(packetDataParsed[1]);
                                packet.Rssi = Int32.Parse(packetDataParsed[2]);
                                packet.MacSource = packetDataParsed[3];
                                packet.Timestamp = packetDataParsed[4];
                                packet.Hash = packetDataParsed[5];
                                packet.MacEsp32 = dataReceivedParsed[0];
                                packet.Id = lastIdDatabase;
                                lastIdDatabase++;

                                try
                                {
                                    PacketFactory instance = PacketFactory.Instance;
                                    instance.InsertPacket(packet);
                                }
                                catch (MySqlException e)
                                {
                                    Console.WriteLine(e.ToString());
                                }



                            }
                            Console.WriteLine("Insert into DB completed");
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

        static String ReadData(NetworkStream stream) {
            byte[] msgReceivedBytes = new byte[1024];
            MemoryStream ms = new MemoryStream();
            StringBuilder dataComplete = new StringBuilder();
            int numBytesRead;
            while ((numBytesRead = stream.Read(msgReceivedBytes, 0, msgReceivedBytes.Length)) > 0)
            {
                ms.Write(msgReceivedBytes, 0, numBytesRead);


            }

            String data = System.Text.Encoding.ASCII.GetString(ms.ToArray(), 0, (int)ms.Length);
            
            Console.WriteLine("Lunghezza dati: " + data.Length);
            return data;
        }

       /* static byte[] ReadLine(NetworkStream stream, int maxlen)
        {

            int n, rc;
            //byte[] readBytes = new byte[1024];
            Console.WriteLine("BEGIN ReadLine");
            List<byte> readBytes = new List<byte>();
            byte[] charReadByte = new byte[1];
            for (n = 0; n < maxlen; n++)
            {
                rc = stream.Read(charReadByte, 0, 1);
                //Get only one char
                if (rc == 1)
                {
                    Console.WriteLine("Primo carattere letto");
                    char[] charRead = new char[1];
                    charRead = System.Text.Encoding.ASCII.GetChars(charReadByte);
                    if (charRead[0] == ';')
                    {
                        Console.WriteLine("New line founded");
                        readBytes.Add(charReadByte[0]);
                        break;
                    }
                    else
                    {
                        //Console.Write(c);
                        readBytes.Add(charReadByte[0]);
                        Console.WriteLine("Non è stata trovata una nuova linea" + charRead[0] );
                    }
                }
                else if (rc == 0)
                {
                    if (n == 0) {
                        Console.WriteLine("EOF");
                        return readBytes.ToArray(); // EOF, no data read 
                        
                    }
                    else
                        break; // EOF, some data was read 
                }
                else
                    return null; // error, errno set by read() 
            }
            Console.Write("Letto riga funzione: ");
            Console.WriteLine(System.Text.Encoding.ASCII.GetString(readBytes.ToArray(), 0, n));
            return readBytes.ToArray();
        }*/

    }
}

