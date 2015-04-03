﻿using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using FarseerPhysics;
using GeneticTanks.Game;
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
      Log.InfoFormat("PID {0}", Process.GetCurrentProcess().Id);

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

    private EventManager m_eventManager;
    private EntityManager m_entityManager;
    private RenderManager m_renderManager;
    private InputManager m_inputManager;
    private ViewManager m_viewManager;
    private PhysicsManager m_physicsManager;
    
    private TankFactory m_tankFactory;
    #endregion

    public void Run()
    {
      Initialize();
      MainLoop();
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
      
      m_eventManager = new EventManager();
      m_entityManager = new EntityManager(m_eventManager);
      m_renderManager = new RenderManager(m_eventManager);
      m_inputManager = new InputManager(m_renderWindow, m_eventManager);
      m_viewManager = new ViewManager(m_eventManager, m_renderWindow);
      m_physicsManager = new PhysicsManager(m_eventManager);

      m_tankFactory = new TankFactory
      {
        EntityManager = m_entityManager,
        EventManager = m_eventManager,
        PhysicsManager = m_physicsManager
      };
      
      m_physicsManager.CreateWorld();

      m_tankFactory.CreateControlledTestTank(Vector2.Zero);
      //m_tankFactory.CreateTestTank(Vector2.Zero);
      m_tankFactory.CreateTestTank(new Vector2(50, 0));
    }

    private void MainLoop()
    {
      Stopwatch frameTime = new Stopwatch();
      
      while (m_window.Visible)
      {
        float lastFrameTime = (float)frameTime.Elapsed.TotalSeconds;
        frameTime.Restart();

        m_renderWindow.SetView(m_viewManager.View);
        if (m_renderManager.Update(lastFrameTime, m_renderWindow))
        {
          m_renderWindow.Display();
        }

        Application.DoEvents();
        m_inputManager.Update(lastFrameTime);
        m_entityManager.Update(lastFrameTime);
        m_physicsManager.Update(lastFrameTime);

        var maxEventTime = MaxFrameTime - (float)frameTime.Elapsed.TotalSeconds;
        m_eventManager.Update(maxEventTime);

        if (frameTime.Elapsed.TotalSeconds < MaxFrameTime)
        {
          Thread.Sleep(1);
        }
      }
    }

    #endregion
  }
}
