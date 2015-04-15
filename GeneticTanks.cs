using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using FarseerPhysics;
using FarseerPhysics.Dynamics;
using FarseerPhysics.Factories;
using GeneticTanks.Extensions;
using GeneticTanks.Game;
using GeneticTanks.Game.Components;
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
    private static readonly Vector2 ArenaSize = 
      new Vector2(500, 500);

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


      TankFactory.EntityManager = m_entityManager;
      TankFactory.EventManager = m_eventManager;
      TankFactory.PhysicsManager = m_physicsManager;

      BulletFactory.EntityManager = m_entityManager;
      BulletFactory.EventManager = m_eventManager;
      BulletFactory.PhysicsManager = m_physicsManager;

      m_physicsManager.CreateWorld();
      CreateArena();

      //TankFactory.CreateControlledTestTank(Vector2.Zero, 0);
      TankFactory.CreateTestTank(new Vector2(-200, -200), 90);
      TankFactory.CreateTestTank(new Vector2(200, -200), 180);
      TankFactory.CreateTestTank(new Vector2(0, 100), 90);
      TankFactory.CreateTestTank(new Vector2(-100, 200), 0);
      TankFactory.CreateTestTank(new Vector2(-100, -100), 135);
      TankFactory.CreateTestTank(new Vector2(-200, 0), -90);
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

    private void CreateArena()
    {
      var halfWidth = ArenaSize.X / 2f;
      var halfHeight = ArenaSize.Y / 2f;
      var upperLeft = new Vector2(-halfWidth, halfHeight);
      var lowerLeft = new Vector2(-halfWidth, -halfHeight);
      var lowerRight = new Vector2(halfWidth, -halfHeight);
      var upperRight = new Vector2(halfWidth, halfHeight);

      var entity = new Entity(EntityManager.NextId, "wall");
      entity.AddComponent(new StaticPhysicsTransformComponent(
        entity, m_physicsManager, world =>
        {
          var body = BodyFactory.CreateBody(world, Vector2.Zero, entity.Id);
          FixtureFactory.AttachEdge(upperLeft, lowerLeft, body, entity.Id);
          FixtureFactory.AttachEdge(lowerLeft, lowerRight, body, entity.Id);
          FixtureFactory.AttachEdge(lowerRight, upperRight, body, entity.Id);
          FixtureFactory.AttachEdge(upperRight, upperLeft, body, entity.Id);          

          body.BodyType = BodyType.Static;
          body.Rotation = 0;
          body.CollisionCategories = PhysicsManager.TerrainCategory;
          body.CollidesWith = Category.All;

          return body;
        }));
      var src = new SimpleRenderComponent(entity, 
        () => new RectangleShape(ArenaSize.ToVector2f())
        {
          Origin = new Vector2f(halfWidth, halfHeight)
        });
      entity.AddComponent(src);

      if (!entity.Initialize())
      {
        Log.Error("Failed to init walls");
        return;
      }

      src.ZDepth = 100;
      src.FillColor = Color.Transparent;
      src.OutlineColor = Color.Black;
      src.OutlineThickness = 1f;
      m_entityManager.AddEntity(entity);
    }

    #endregion
  }
}
