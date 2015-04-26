using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using FarseerPhysics;
using GeneticTanks.Extensions;
using GeneticTanks.Game;
using GeneticTanks.Game.Events;
using GeneticTanks.UI;
using log4net;
using Microsoft.Xna.Framework;
using SFML.Graphics;
using SFML.Window;

namespace GeneticTanks
{
  class GeneticTanks
  {
    private static readonly ILog Log = LogManager.GetLogger(
      MethodBase.GetCurrentMethod().DeclaringType);

    private const float MaxFrameTime = 1f / 30f;

    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
      Log.InfoFmt("PID {0}", Process.GetCurrentProcess().Id);

      // ensure all unhandled exceptions are logged
      AppDomain.CurrentDomain.UnhandledException +=
        (sender, args) => Log.Error(args.ExceptionObject);

      // farseer configuration
      Settings.AllowSleep = true;
      Settings.UseFPECollisionCategories = true;
      Settings.VelocityIterations = 10;
      Settings.PositionIterations = 8;
      
      Application.EnableVisualStyles();
      Application.SetCompatibleTextRenderingDefault(false);

      new GeneticTanks().Run();
    }

    #region Private Fields
    private MainWindow m_window;
    private RenderWindow m_renderWindow;
    #endregion

    public void Run()
    {
      Initialize();
      MainLoop();
      if (Globals.Initialized)
      {
        Globals.Dispose();
      }
    }

    #region Private Methods

    private void Initialize()
    {
      m_window = new MainWindow
      {
        Text = "Genetic Tanks PID " + Process.GetCurrentProcess().Id
      };
      m_window.Show();

      m_renderWindow = new RenderWindow(m_window.DrawingPanelHandle,
        new ContextSettings { AntialiasingLevel = 8 });
      
      Globals.Initialize(m_renderWindow);

      //TankFactory.CreateControlledTestTank(Vector2.Zero, 0);
      TankFactory.CreateTestTank(new Vector2(-200, -200), 90);
      TankFactory.CreateTestTank(new Vector2(200, -200), 180);
      TankFactory.CreateTestTank(new Vector2(0, 100), 90);
      TankFactory.CreateTestTank(new Vector2(-100, 200), 0);
      TankFactory.CreateTestTank(new Vector2(-100, -100), 135);
      TankFactory.CreateTestTank(new Vector2(-200, 0), -90);

      if (Properties.Settings.Default.PauseOnStart)
      {
        Globals.EventManager.QueueEvent(new PauseGameEvent(true));
      }
    }

    private void MainLoop()
    {
      Stopwatch frameTime = new Stopwatch();
      
      while (m_window.Visible)
      {
        float lastFrameTime = (float)frameTime.Elapsed.TotalSeconds;
        frameTime.Restart();

        m_renderWindow.SetView(Globals.ViewManager.View);
        if (Globals.RenderManager.Update(lastFrameTime, m_renderWindow))
        {
          m_renderWindow.Display();
        }

        Application.DoEvents();
        Globals.InputManager.Update(lastFrameTime);
        Globals.EntityManager.Update(lastFrameTime);
        Globals.PhysicsManager.Update(lastFrameTime);

        var maxEventTime = MaxFrameTime - (float)frameTime.Elapsed.TotalSeconds;
        Globals.EventManager.Update(maxEventTime);

        if (frameTime.Elapsed.TotalSeconds < MaxFrameTime)
        {
          Thread.Sleep(1);
        }
      }
    }

    #endregion
  }
}
