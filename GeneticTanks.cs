using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using GeneticTanks.UI;

namespace GeneticTanks
{
  static class GeneticTanks
  {
    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
      // farseer configuration
      FarseerPhysics.Settings.UseFPECollisionCategories = true;
      FarseerPhysics.Settings.VelocityIterations = 10;
      FarseerPhysics.Settings.PositionIterations = 8;
      
      Application.EnableVisualStyles();
      Application.SetCompatibleTextRenderingDefault(false);
      Application.Run(new Form1());
    }
  }
}
