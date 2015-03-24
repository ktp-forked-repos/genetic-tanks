using System;
using System.Reflection;
using System.Windows.Forms;
using log4net;

namespace GeneticTanks.UI
{
  public partial class MainWindow : Form
  {
    private static readonly ILog Log = LogManager.GetLogger(
      MethodBase.GetCurrentMethod().DeclaringType);

    public MainWindow()
    {
      InitializeComponent();

      // force the panel to get focus when the mouse is over it so that it 
      // will receive mouse scroll events
      drawingPanel.MouseHover += (sender, args) => drawingPanel.Focus();
    }

    public IntPtr DrawingPanelHandle
    {
      get { return drawingPanel.Handle; }
    }
  }
}
