using System;
using System.Reflection;
using FarseerPhysics.Dynamics;
using GeneticTanks.Game.Events;
using log4net;
using Microsoft.Xna.Framework;

namespace GeneticTanks.Game.Managers
{
  /// <summary>
  /// Manages the physics world and simulation.
  /// </summary>
  sealed class PhysicsManager
    : IDisposable
  {
    private static readonly ILog Log = LogManager.GetLogger(
      MethodBase.GetCurrentMethod().DeclaringType);

    // Step the physics system at 60 fps
    private const float UpdateInterval = 1f / 60f;

    public static readonly Category TerrainCategory = Category.Cat1;
    public static readonly Category TankCategory = Category.Cat2;
    public static readonly Category BulletCategory = Category.Cat3;
    public static readonly Category SensorCategory = Category.Cat4;
    
    /// <summary>
    /// Handles the PostStep event.
    /// </summary>
    /// <param name="deltaTime">
    /// The time in seconds that will be simulated in the physics step.
    /// </param>
    public delegate void PreStepHandler(float deltaTime);
    
    /// <summary>
    /// Handles the PreStep event.
    /// </summary>
    /// <param name="deltaTime">
    /// The time in seconds that was simulated in the physics step.
    /// </param>
    public delegate void PostStepHandler(float deltaTime);

    #region Private Fields
    private readonly EventManager m_eventManager;
    private float m_timeSinceLastStep = 0f;
    #endregion

    /// <summary>
    /// Create the manager.
    /// </summary>
    /// <param name="em"></param>
    public PhysicsManager(EventManager em)
    {
      if (em == null)
      {
        throw new ArgumentNullException("em");
      }

      m_eventManager = em;
    }

    /// <summary>
    /// Event fires before the physics simulation step.
    /// </summary>
    public event PreStepHandler PreStep;
    
    /// <summary>
    /// Event fires after the physics simulation step.
    /// </summary>
    public event PostStepHandler PostStep;

    /// <summary>
    /// The physics world.
    /// </summary>
    public World World { get; private set; }

    /// <summary>
    /// Updates the physics world.
    /// </summary>
    /// <param name="deltaTime">
    /// The time since update was last called.
    /// </param>
    public void Update(float deltaTime)
    {
      m_timeSinceLastStep += deltaTime;
      while (m_timeSinceLastStep >= UpdateInterval)
      {
        m_timeSinceLastStep -= UpdateInterval;
        StepWorld(UpdateInterval);
      }
    }

    /// <summary>
    /// Creates a new physics world.  Obviously everything should be cleared 
    /// from the existing world before this is called.
    /// </summary>
    public void CreateWorld()
    {
      var world = new World(Vector2.Zero);
      m_eventManager.TriggerEvent(new NewPhysicsWorld(world));
      World = world;
    }

    #region Private Methods

    /// <summary>
    /// Steps the physics simulation by the specified time.
    /// </summary>
    /// <param name="deltaTime"></param>
    private void StepWorld(float deltaTime)
    {
      if (World == null)
      {
        return;
      }
      
      OnPreStep(deltaTime);
      World.Step(deltaTime);
      World.ClearForces();
      OnPostStep(deltaTime);
    }

    /// <summary>
    /// Fires the PreStep event.
    /// </summary>
    /// <param name="deltaTime"></param>
    private void OnPreStep(float deltaTime)
    {
      if (PreStep != null)
      {
        PreStep(deltaTime);
      }
    }
    
    /// <summary>
    /// Fires the PostStep event.
    /// </summary>
    /// <param name="deltaTime"></param>
    private void OnPostStep(float deltaTime)
    {
      if (PostStep != null)
      {
        PostStep(deltaTime);
      }
    }

    #endregion

    #region IDisposable Implementation

    private bool m_disposed = false;

    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
      if (m_disposed)
      {
        return;
      }

      World = null;

      m_disposed = true;
    }

    ~PhysicsManager()
    {
      Dispose(false);
    }

    #endregion
  }
}
