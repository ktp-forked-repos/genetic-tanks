using System;
using System.Reflection;
using log4net;
using SFML.Graphics;

namespace GeneticTanks.Game.Components
{
  /// <summary>
  /// Provides a common base for all renderable components.  An entity that 
  /// contains any RenderComponent must also contain a TransformComponent.
  /// </summary>
  abstract class RenderComponent
    : Component, IComparable<RenderComponent>
  {
    private static readonly ILog Log = LogManager.GetLogger(
      MethodBase.GetCurrentMethod().DeclaringType);

    /// <summary>
    /// Create the component.
    /// </summary>
    /// <param name="parent"></param>
    protected RenderComponent(Entity parent) 
      : base(parent)
    {
    }

    /// <summary>
    /// The depth that this component renders at, with smaller numbers being 
    /// closer to the camera and larger numbers farther away.
    /// </summary>
    public int ZDepth { get; protected set; }
    
    /// <summary>
    /// Draw the component onto the provided target.
    /// </summary>
    /// <param name="target"></param>
    public abstract void Draw(RenderTarget target);

    #region Component Implementation
    /// <summary>
    /// Initializes the component, must always be called by subclasses.
    /// </summary>
    /// <returns>
    /// Success or failure of initialization.
    /// </returns>
    public override bool Initialize()
    {
      return Parent.Transform != null;
    }

    #endregion
    #region IComparable Implementation

    /// <summary>
    /// Compares render components based on their Z depth, with far objects 
    /// ordered first.
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public int CompareTo(RenderComponent other)
    {
      if (other == null)
      {
        throw new ArgumentNullException("other");
      }

      if (ZDepth > other.ZDepth)
      {
        return -1;
      }
      else if (ZDepth == other.ZDepth)
      {
        return 0;
      }
      else
      {
        return 1;
      }
    }

    #endregion
  }
}
