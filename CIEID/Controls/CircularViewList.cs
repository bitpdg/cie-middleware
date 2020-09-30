using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CIEID.Controls
{
    class CircularViewList : List<BaseItemControl>
    {
        public void ShiftLeft()
        {
            BaseItemControl control = this.FirstOrDefault();

            if (control == null) return;

            this.Remove(control);
            this.Add(control);
        }

        public void ShiftRight()
        {
            BaseItemControl control = this.LastOrDefault();

            if (control == null) return;

            this.Remove(control);
            this.Insert(0, control);
        }
    }
}
