using System;

namespace GeneticTanks.Game.Components
{
  /// <summary>
  /// The base class for all entity components.   
  /// </summary>
  /// <remarks>
  /// All component subclasses must implement IDisposable so that it is safe to 
  /// dispose the component even if it wasn't initialized, or initialization 
  /// failed.
  /// </remarks>
  abstract class Component 
    : IDisposable
  {
    /// <summary>
    /// Creates a new component.
    /// </summary>
    /// <param name="parent">The entity that contains this component.</param>
    /// <exception cref="ArgumentNullException">parent is null</exception>
    protected Component(Entity parent)
    {
      if (parent == null)
      {
        throw new ArgumentNullException("parent");
      }

      Parent = parent;
      Initialized = false;
      NeedsUpdate = false;
    }

    /// <summary>
    /// The parent entity that this component is contained within.
    /// </summary>
    public Entity Parent { get; private set; }

    /// <summary>
    /// Tracks the initialization state of the component.  Must be set by the 
    /// subclass.
    /// </summary>
    public bool Initialized { get; protected set; }

    /// <summary>
    /// Signifies that the component requires logic updates.  Must be set by 
    /// the subclass or Update will not be called.
    /// </summary>
    public bool NeedsUpdate { get; protected set; }

    /// <summary>
    /// Initialize the component to a useable state.  Is called after all 
    /// components have been added to an entity.
    /// </summary>
    /// <returns>
    /// The success or failure of initialization.
    /// </returns>
    public abstract bool Initialize();

    /// <summary>
    /// Performs a logic update on the component.
    /// </summary>
    /// <param name="deltaTime">
    /// The seconds elapsed since the last update.
    /// </param>
    public abstract void Update(float deltaTime);

    #region IDisposable Implementation

    private bool m_disposed = false;

    /// <summary>
    /// Clean up the component's resources
    /// </summary>
    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Perform the actual cleanup.  Subclasses must call the base 
    /// implementation.
    /// </summary>
    /// <param name="disposing">
    /// When true, dispose all managed resources.
    /// </param>
    protected virtual void Dispose(bool disposing)
    {
      if (m_disposed || !Initialized)
      {
        return;
      }

      Parent = null;
      m_disposed = true;
    }
    
    #endregion

    ~Component()
    {
      Dispose(false);
    }
  }
}
