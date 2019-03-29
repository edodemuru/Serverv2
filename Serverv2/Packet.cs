using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Serverv2
{
    public class Packet
    {
        private String ssid;
        private int channel;
        private int rssi;
        private String macSource;
        private String macEsp32;
        private String timestamp;
        private String hash;
        private int id;
        
        //Constructor
        public Packet()
        {
            Ssid = "";
            Channel = -1;
            Rssi = 0;
            MacSource = "";
            MacEsp32 = "";
            Timestamp = "";
            Hash = "";
            Id = -1;
        }

        //Properties
        public string Ssid { get => ssid; set => ssid = value; }
        public int Channel { get => channel; set => channel = value; }
        public int Rssi { get => rssi; set => rssi = value; }
        public string MacSource { get => macSource; set => macSource = value; }
        public string MacEsp32 { get => macEsp32; set => macEsp32 = value; }
        public string Timestamp { get => timestamp; set => timestamp = value; }
        public string Hash { get => hash; set => hash = value; }
        public int Id { get => id; set => id = value; }
    }
}
