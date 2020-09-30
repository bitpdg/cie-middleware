using System.Windows.Forms;

namespace CIEID.Controls
{
    abstract class BaseItemControl : TableLayoutPanel
    {
        public const int IMAGE_WIDTH = 120;
        public const int IMAGE_HEIGHT = 70;

        abstract public void UpdateChildrenSizeFactor(float factor);
    }
}
