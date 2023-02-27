using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KPI_measuring_software
{
    internal class DeviceInfo
    {
        public string description { get;}
        public string value { get;}
        public DeviceInfo(string description, string value) 
        {
            this.description = description;
            this.value = value;
        }
    }
}
