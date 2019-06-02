using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Serverv2
{
    public class Device : IComparable
    {
        private String mac = "";
        private double x = 0;
        private double y = 0;
        private String time = "";

        //Constructor
        public Device(String mac, double xc, double yc)
        {
            Mac = mac;
            X = xc;
            Y = yc;
            
        }

        public Device(String mac, double xc, double yc, String time)
        {
            Mac = mac;
            X = xc;
            Y = yc;
            Time = time;

        }

        public Device(double xc, double yc)
        {
            X = xc;
            Y = yc;
        }

        //Properties
        public string Mac { get => mac; set => mac = value; }
        public double X { get => x; set => x = value; }
        public double Y { get => y; set => y = value; }
        public string Time { get => time; set => time = value; }

        //Method to compare 2 strings, derived from 
        public int CompareTo(object obj)
        {
            Device d = obj as Device;
            return this.Mac.CompareTo(d.Mac);
        }

    }
}
