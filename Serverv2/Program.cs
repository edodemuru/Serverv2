using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;


using MySql.Data.MySqlClient;
using System.IO;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Complex;

namespace Serverv2
{


    class Program
    {
        //Number of esp32
        public const int numEsp32 = 3;

        static void Main(string[] args)
        {
            try
            {
                //Set ip endpoint for socket
                IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse("192.168.137.1"), 8888);
                // Create a TCP/IP socket.  
                Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                // Bind the socket to the local endpoint and   
                // listen for incoming connections.  
                listener.Bind(endPoint);
                listener.Listen(10);

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
                //Mac of esp32 connected
                List<string> macEsp32 = new List<string>();

                //Get last id from db
                int firstIdDB = PacketFactory.Instance.GetPacketMaxId() + 1;
                int lastIdDB = firstIdDB;
                PacketFactory.Instance.NumEsp32 = numEsp32;

                //List of esp32
                List<Device> esp32 = new List<Device>();
                /*Device esp0 = new Device(5, 5);
                Device esp1 = new Device(-5, 5);
                Device esp2 = new Device(-5, -5);
                Device esp3 = new Device(5, -5);
                esp32.Add(esp0);
                esp32.Add(esp1);
                esp32.Add(esp2);
                esp32.Add(esp3);*/
                //Define esp32 for position
                Device esp0 = new Device(10, 0);
                Device esp1 = new Device(-10, 0);
                Device esp2 = new Device(0, 10);
                esp32.Add(esp0);
                esp32.Add(esp1);
                esp32.Add(esp2);
              


                //Enter the listening loop
                while (true)
                {
                    Console.Write("Waiting for a connection... ");

                    // Perform a blocking call to accept requests.
                    Socket handler = listener.Accept();
                    Console.WriteLine("Connected!");

                    int numberOfBytesRead = 0;
                    byte[] myReadBuffer = new byte[1024];
                    StringBuilder myCompleteMessage = new StringBuilder();
                    int totalNum = 0;


                    // An incoming connection needs to be processed.  
                    while ((numberOfBytesRead = handler.Receive(myReadBuffer, myReadBuffer.Length, 0)) > 0)
                    {
                        totalNum += numberOfBytesRead;
                        myCompleteMessage.AppendFormat("{0}", Encoding.ASCII.GetString(myReadBuffer, 0, numberOfBytesRead));
                        
                        if (myCompleteMessage.ToString().EndsWith("EOF"))
                        {
                            break;
                        }
                    }

                    dataReceived = myCompleteMessage.ToString();
                    Console.WriteLine("Dimensione dati: " + totalNum);

                    if (dataReceived!= null)
                    {
                        //Parse packets
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
                                timestampModified = timestamp.AddMinutes(0.2);
                                Console.WriteLine("Time when esp32 will start working " + timestampModified.ToString());

                                macEsp32.Add(dataReceivedParsed[0]);
                                esp32[macEsp32.Count-1].Mac = dataReceivedParsed[0];
                                
                                
                            }
                            //List has at least one element
                            else
                            {
                                Console.WriteLine("Other Connection");
                                //Mac is not inside list
                                if (!macEsp32.Contains(dataReceivedParsed[0]))
                                {
                                    macEsp32.Add(dataReceivedParsed[0]);
                                   
                                    esp32[macEsp32.Count-1].Mac = dataReceivedParsed[0];
                                    if (macEsp32.Count == numEsp32)
                                        //Sort all esp32 devices based on mac
                                        esp32.Sort();


                                    

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
                                timestampModified = timestamp.AddMinutes(0.2);
                                Console.WriteLine("Time when esp32 will start working " + timestampModified.ToString());

                            }
                            numEspTemp++;
                            
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
                                if (packetData.Equals(dataReceivedParsed[0]) || packetData.Equals("EOF"))
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
                                packet.Id = lastIdDB;
                                lastIdDB++;

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

                            //Last esp32
                            if (numEspTemp == macEsp32.Count)
                            {
                                Console.WriteLine("Last esp32 to connect after first connection");
                                numEspTemp = 0;


                                //DATA ANALYSIS
                                //Obtain packets received from all esp32
                                List<String> hashPkFiltered = PacketFactory.Instance.GetListHashFiltered(firstIdDB);
                                //create list of current position
                                List<Device> list_devices = new List<Device>();
                                //For each hash, obtain the most recent packets received from all esp32
                                foreach (String hash in hashPkFiltered)
                                {
                                    //Packet List Ordered by mac esp32
                                    List<Packet> pkFiltered = PacketFactory.Instance.GetListPkFilteredFromHash(hash,firstIdDB);
                                    //Console.WriteLine("Id packets filtered:");

                                    //**LEAST SQUARE ESTIMATION METHOD**/ ->see page 9 of the pdf: "sensors"

                                    double[] startVectorb = new double[numEsp32 - 1]; //b vector

                                    Matrix<double> aMatrix = Matrix<double>.Build.Dense(numEsp32-1,2); //matrix A
                                    Vector<double> coordinate = Vector<double>.Build.Dense(2); //row of Matrix A
                                    double rm = GetDistanceFromRssi(pkFiltered[numEsp32 - 1].Rssi); //distance from the last esp32
                                    //TO REMEMBER: WE ARE USING PKFILTERED AS REFERENCE VECTOR WITH THE POSITIONS
                                   /* int q = 0; //index of the last esp32
                                    for (int t = 0; t < numEsp32 - 1; t++)
                                    {
                                        if (esp32[t].Mac.Equals(pkFiltered[numEsp32-1].MacEsp32))
                                            q = t;
                                    }*/


                                    for (int i = 0; i < numEsp32 - 1; i++)
                                    {
                                        //distanza i-esima rilevata dalla i-esima esp32
                                        double ri = GetDistanceFromRssi(pkFiltered[i].Rssi);

                                        /*int s = 0; //index of the current esp32 that contain the device
                                        for (int k = 0; k < numEsp32 - 1; k++)
                                        {
                                            if (esp32[k].Mac.Equals(pkFiltered[i].MacEsp32))
                                                s = k;
                                        }*/

                                       

                                        startVectorb[i] = Math.Pow(esp32[i].X, 2) - Math.Pow(esp32[numEsp32 - 1].X, 2) + Math.Pow(esp32[i].Y, 2) - Math.Pow(esp32[numEsp32 - 1].Y, 2) + Math.Pow(ri, 2) - Math.Pow(rm, 2);
                                        
                                       // System.Console.WriteLine("Riga " + i + " vettore b " + startVectorb[i]);
                                        coordinate[0] = 2 * (esp32[i].X - esp32[numEsp32 - 1].X);
                                        coordinate[1] = 2 * (esp32[i].Y - esp32[numEsp32 - 1].Y);
                                       // System.Console.WriteLine("Coordinata 0 + " + coordinate[0] + " Coordinata 1 + " + coordinate[1]);
                                        //aMatrix = aMatrix.InsertRow(i, coordinate); //built A matrix by row
                                        aMatrix.SetRow(i, coordinate);
                                       // System.Console.WriteLine("Costruziona Matrice A : " + aMatrix.);
                                    }
                                    
                                    Vector<double> bVector = Vector<double>.Build.Dense(startVectorb);

                                    var posResult = ((aMatrix.Transpose() * aMatrix).Inverse()) * (aMatrix.Transpose()) * bVector; // to check NAN 

                                    //xy cordinate based on the 4 most recent packets sniffed with the same hash
                                    double[] position = posResult.ToArray();

                                    //now we verify the identity of the device by selecting the mac_source of one of the 4 packets
                                    //if alredy present into list of position we update otherwise insert new device position

                                    //no matter which packet consider because they have the same mac source, to simplify choose 0
                                    Device tmp_p = new Device((pkFiltered[0].MacSource), position[0], position[1], pkFiltered[0].Timestamp);
                                    int found = 0;

                                    list_devices.Add(tmp_p);
                                    Console.WriteLine("ID: " + pkFiltered[0].Id +  " Mac device: " + tmp_p.Mac + " Coordinate: " + tmp_p.X + " " + tmp_p.Y);


/*
                                    foreach (Device p in list_devices)
                                     {
                                         if (p.Mac.Equals(tmp_p.Mac))
                                         {

                                             found++;
                                             //check the timestamp of the previuos one to verify if is greater or less
                                             DateTime oldtime = DateTime.Parse(p.Time);
                                             DateTime newtime = DateTime.Parse(tmp_p.Time);

                                             if (DateTime.Compare(newtime, oldtime) > 0)
                                             {
                                                 p.X = tmp_p.X;
                                                 p.Y = tmp_p.Y;

                                             }
                                         }

                                     }*/

                                    /*if (found == 0)
                                    {
                                        list_devices.Add(tmp_p); //insert the position of the new device sniffed 
                                        Console.WriteLine("Mac device: " + tmp_p.Mac + "Coordinate: " + tmp_p.X + " " + tmp_p.Y);
                                    }*/


                                   /* if (pkFiltered[0].MacSource == "24:18:1d:e3:6d:c3" || pkFiltered[0].MacSource == "d0:57:7b:f0:d2:0a")
                                    {
                                        Console.WriteLine("Id: " + pkFiltered[0].Id + " Distanza da scheda 1 " + GetDistanceFromRssi(pkFiltered[0].Rssi) + " Metri");
                                       // Console.WriteLine("Id: " + pkFiltered[1].Id + " Distanza da scheda 2 " + GetDistanceFromRssi(pkFiltered[1].Rssi) + " Metri");
                                        //Console.WriteLine("Id: " + pkFiltered[2].Id + " Distanza da scheda 2 " + GetDistanceFromRssi(pkFiltered[2].Rssi) + " Metri");
                                    }*/
                                }

                                firstIdDB = lastIdDB;

                            }
                            
                        }


                    }

                    if (dataToSend != "")
                    {
                        //Translate String to ASCII
                        byte[] dataToSendBytes = System.Text.Encoding.ASCII.GetBytes(dataToSend);

                        // Send back a response.
                        handler.Send(dataToSendBytes, dataToSendBytes.Length, 0);

                        Console.WriteLine(String.Format("Sent: {0}", dataToSend));
                    }
                    else
                    {
                        Console.WriteLine("Errore nessun dato inviato");
                    }

                    // Shutdown and end connection
                    handler.Shutdown(SocketShutdown.Both);
                    handler.Close();
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
                var attempts = 0;
                var times = 4;
                var delay = 1000;
                do
                {
                    try
                    {
                        attempts++;
                        socket.Connect(ipEndPoint);

                        //Stops code hang if NTP is blocked
                        socket.ReceiveTimeout = 3000;

                        socket.Send(ntpData);
                        socket.Receive(ntpData);
                        socket.Close();
                        break;
                    }
                    catch (SocketException ex)
                    {
                        if (attempts == times)
                            throw;
                        Console.WriteLine("Exception" + ex.Message +  " caught on attempt " + attempts + " will retry after delay");

                        Task.Delay(delay).Wait();
                    }
                } while (true);
               /* try
                {
                    socket.Connect(ipEndPoint);

                    //Stops code hang if NTP is blocked
                    socket.ReceiveTimeout = 3000;

                    socket.Send(ntpData);
                    socket.Receive(ntpData);
                    socket.Close();
                }
                catch
                {

                }*/
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

        static double GetDistanceFromRssi(double rssi)
        {
            //double measuredPower = -39 -45 -51 -69 -42;
            double measuredPower = -42;
            double n = 2;
            //double n = 4;
            return Math.Pow(10, (measuredPower - rssi) / (10 * n));
        }

    }
}

