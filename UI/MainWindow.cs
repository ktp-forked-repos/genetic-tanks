using System;
using System.Windows.Forms;

namespace GeneticTanks.UI
{
  public partial class MainWindow : Form
  {
    public MainWindow()
    {
      InitializeComponent();
    }

    public IntPtr DrawingPanelHandle
    {
      get { return drawingPanel.Handle; }
    }
  }
}
