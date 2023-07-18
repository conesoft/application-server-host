using System.Drawing;
using System.Windows.Forms;

namespace Conesoft.Server_Host.UI;

public class MyLightColorTable : ProfessionalColorTable
{
    public override Color MenuItemBorder => Color.White;
    public override Color MenuItemSelected => Color.White;
    public override Color ToolStripDropDownBackground => Color.White;
    public override Color ImageMarginGradientBegin => Color.White;
    public override Color ImageMarginGradientMiddle => Color.White;
    public override Color ImageMarginGradientEnd => Color.White;
    public override Color MenuBorder => Color.White;
    public override Color ToolStripBorder => Color.White;
    public override Color SeparatorDark => Color.White;
    public override Color SeparatorLight => Color.White;
}