using System.Windows.Forms;

namespace CIEID.Controls
{
    abstract class BaseItemControl : TableLayoutPanel
    {
        abstract public void UpdateChildrenSizeFactor(float factor);
    }
}
