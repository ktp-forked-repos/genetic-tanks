using System;
using GeneticTanks.Game.Managers;
using SFML.Graphics;

namespace GeneticTanks.Game
{
  static class Globals
  {
    public static EntityManager EntityManager { get; private set; }
    public static EventManager EventManager { get; private set; }
    public static InputManager InputManager { get; private set; }
    public static PhysicsManager PhysicsManager { get; private set; }
    public static RenderManager RenderManager { get; private set; }
    public static ViewManager ViewManager { get; private set; }

    public static void Initialize(RenderWindow window)
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
    }
  }
}
