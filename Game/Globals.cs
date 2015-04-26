using System;
using GeneticTanks.Game.Managers;
using Microsoft.Xna.Framework;
using SFML.Graphics;

namespace GeneticTanks.Game
{
  static class Globals
  {
    public static bool Initialized { get; private set; }

    public static EntityManager EntityManager { get; private set; }
    public static EventManager EventManager { get; private set; }
    public static InputManager InputManager { get; private set; }
    public static PhysicsManager PhysicsManager { get; private set; }
    public static RenderManager RenderManager { get; private set; }
    public static ViewManager ViewManager { get; private set; }
    public static ProcessManager ProcessManager { get; private set; }

    public static Arena Arena { get; private set; }

    public static bool Initialize(RenderWindow window)
    {
      if (window == null)
      {
        throw new ArgumentNullException("window");
      }

      EventManager = new EventManager();
      EntityManager = new EntityManager(EventManager);
      RenderManager = new RenderManager(EventManager);
      InputManager = new InputManager(window, EventManager);
      ViewManager = new ViewManager(EventManager, window);
      PhysicsManager = new PhysicsManager(EventManager);
      ProcessManager = new ProcessManager();

      PhysicsManager.CreateWorld();

      var dimensions = new Vector2(
        Properties.Settings.Default.ArenaWidth,
        Properties.Settings.Default.ArenaHeight
        );
      Arena = new Arena(dimensions);
      if (!Arena.Initialize())
      {
        return false;
      }

      Initialized = true;
      return true;
    }

    public static void Dispose()
    {
      Arena.Dispose();

      ProcessManager.Dispose();
      EntityManager.Dispose();
      InputManager.Dispose();
      ViewManager.Dispose();
      RenderManager.Dispose();
      PhysicsManager.Dispose();
    }
  }
}
