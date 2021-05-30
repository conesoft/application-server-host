using System.Drawing;
using System.Windows.Forms;

namespace Conesoft.Host.UI
{
    public class MyDarkColorTable : ProfessionalColorTable
    {
        public override Color MenuItemBorder => Color.Black;
        public override Color MenuItemSelected => Color.Black;
        public override Color ToolStripDropDownBackground => Color.Black;
        public override Color ImageMarginGradientBegin => Color.Black;
        public override Color ImageMarginGradientMiddle => Color.Black;
        public override Color ImageMarginGradientEnd => Color.Black;
        public override Color MenuBorder => Color.Black;
        public override Color ToolStripBorder => Color.Black;
        public override Color SeparatorDark => Color.Black;
        public override Color SeparatorLight => Color.Black;
    }
}
