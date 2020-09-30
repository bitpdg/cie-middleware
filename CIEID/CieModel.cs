using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CIEID
{
    class CieModel
    {
        public String SerialNumber { get; set; }
        public String Owner { get; set; }

        public CieModel(String serialNumber, String owner)
        {
            this.SerialNumber = serialNumber;
            this.Owner = owner;
        }
    }
}
