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
                String sqlInsert = "insert into `packets`(`Id`, `ssid`, `channel`, `rssi`, `source mac`, `esp32 mac`, `timestamp`, `hash`) values (@Id,@SID,@Cha,@RS,@MACs,@MACe,@Time,@H)";
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

        public int getPacketMaxId()
        {
            int result = -1;
            try
            {
                MySqlConnection databaseConnection = new MySqlConnection(connectionString);
                databaseConnection.Open();
                String sqlInsert = "select * from packets where id=( select max(id) from packets)";
                MySqlCommand cmd = new MySqlCommand(sqlInsert, databaseConnection);
                MySqlDataReader reader = cmd.ExecuteReader();
                if (reader.Read())
                {
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

        public string ConnectionString { get => connectionString; set => connectionString = value; }
    }
}
