using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Serverv2
{
    public sealed class PacketFactory
    {
        private static PacketFactory instance = null;
        private String connectionString = "Server=localhost;Database=PacketDB;Uid=root;Pwd=";
        private int numEsp32;

        private PacketFactory()
        {
        }

        public static PacketFactory Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new PacketFactory();
                }
                return instance;
            }
        }

        public void InsertPacket(Packet packet)
        {

            try
            {
                MySqlConnection databaseConnection = new MySqlConnection(connectionString);
                databaseConnection.Open();
                String sqlInsert = "insert into `packets`(`Id`, `ssid`, `channel`, `rssi`, `source_mac`, `esp32_mac`, `timestamp`, `hash`) values (@Id,@SID,@Cha,@RS,@MACs,@MACe,@Time,@H)";
                MySqlCommand cmd = new MySqlCommand(sqlInsert, databaseConnection);
                cmd.Parameters.Add("@Id", MySqlDbType.Int32).Value = packet.Id;
                cmd.Parameters.Add("@Time", MySqlDbType.VarChar).Value = packet.Timestamp;
                cmd.Parameters.Add("@Cha", MySqlDbType.Int32).Value = packet.Channel;
                cmd.Parameters.Add("@SID", MySqlDbType.VarChar).Value = packet.Ssid;
                cmd.Parameters.Add("@RS", MySqlDbType.Int32).Value = packet.Rssi;
                cmd.Parameters.Add("@MACs", MySqlDbType.VarChar).Value = packet.MacSource;
                cmd.Parameters.Add("@MACe", MySqlDbType.VarChar).Value = packet.MacEsp32;
                cmd.Parameters.Add("@H", MySqlDbType.VarChar).Value = packet.Hash;
                int i = cmd.ExecuteNonQuery();
                databaseConnection.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("error " + ex.Message);
                return;
            }
        }

        public int GetPacketMaxId()
        {
            int result = -1;
            try
            {
                MySqlConnection databaseConnection = new MySqlConnection(connectionString);
                databaseConnection.Open();
                String sqlInsert = "select max(id) from packets";
                MySqlCommand cmd = new MySqlCommand(sqlInsert, databaseConnection);
                MySqlDataReader reader = cmd.ExecuteReader();
                if (reader != null && reader.HasRows && reader.Read())
                {
                    if (!reader.IsDBNull(0))
                        result = reader.GetInt32(0);

                }
                reader.Close();
                databaseConnection.Close();

                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine("error " + ex.Message);
                return -1;
            }
        }

        //Get list of hash of packets received from both esp32
        public List<String> GetListHashFiltered() {
            List<String> listHash = new List<String>();

            try
            {
                MySqlConnection databaseConnection = new MySqlConnection(connectionString);
                databaseConnection.Open();

                String sqlQuery = "select hash from (select* from packets group by hash,esp32_mac) as filteredPackets group by hash having count(*) = " + NumEsp32;

                MySqlCommand cmd = new MySqlCommand(sqlQuery, databaseConnection);
                MySqlDataReader reader = cmd.ExecuteReader();

                if (reader.HasRows)
                {
                    //while cicle to read the data
                    while (reader.Read())
                    {
                        //each row from the data matched by the query
                        listHash.Add(reader.GetString(0));

                    }
                }
                else
                {
                    Console.WriteLine("No rows found.");
                }

                //int i = cmd.ExecuteNonQuery();
                databaseConnection.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("error " + ex.Message);
                return null;
            }

            return listHash;
        }

        public int GetCountFromPacket(Packet p)
        {
            int numb = 0;
            //To convert SQLdataReader into String 
            try
            {
                MySqlConnection databaseConnection = new MySqlConnection(connectionString);
                databaseConnection.Open();

                String sqlQuery = "SELECT COUNT(*) from packets WHERE hash ='" + p.Hash + "' GROUP by esp32_mac";


                MySqlCommand cmd = new MySqlCommand(sqlQuery, databaseConnection);
                MySqlDataReader reader = cmd.ExecuteReader();

                if (reader.HasRows)
                {
                    //while cicle to read the data
                    while (reader.Read())
                    {
                        //each row from the data matched by the query
                        //string []  row ={ reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetString(3) }; //depends by the number of the coloumn
                        numb = reader.GetInt32(0);

                    }

                }
                else
                {
                    Console.WriteLine("No rows found.");
                }

                //int i = cmd.ExecuteNonQuery();
                databaseConnection.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("error " + ex.Message);
                return -1;
            }

            return numb;
        }

        public List<Packet> GetListPkFromPacket(Packet p)
        {
            List<Packet> listPackets = new List<Packet>();

            try
            {
                MySqlConnection databaseConnection = new MySqlConnection(connectionString);
                databaseConnection.Open();

                String sqlQuery = "select*  from 'packets' where hash=" + p.Hash + " group by esp32_mac";

                MySqlCommand cmd = new MySqlCommand(sqlQuery, databaseConnection);
                MySqlDataReader reader = cmd.ExecuteReader();

                if (reader.HasRows)
                {
                    //while cicle to read the data
                    while (reader.Read())
                    {
                        //each row from the data matched by the query
                        Packet tmp = new Packet();

                        tmp.Id = reader.GetInt32(0);
                        tmp.Ssid = reader.GetString(1);
                        tmp.Channel = reader.GetInt32(2);
                        tmp.Rssi = reader.GetInt32(3);
                        tmp.MacSource = reader.GetString(4);
                        tmp.MacEsp32 = reader.GetString(5);
                        tmp.Timestamp = reader.GetString(6);
                        tmp.Hash = reader.GetString(7);

                        listPackets.Add(tmp);

                    }
                }
                else
                {
                    Console.WriteLine("No rows found.");
                }

                //int i = cmd.ExecuteNonQuery();
                databaseConnection.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("error " + ex.Message);
                return null;
            }

            return listPackets;
        }

        public string ConnectionString { get => connectionString; set => connectionString = value; }
        public int NumEsp32 { get => numEsp32; set => numEsp32 = value; }
    }
}
