using System;
using System.Diagnostics;
using System.Reflection;
using FarseerPhysics.Dynamics;
using log4net;

namespace GeneticTanks.Game.Components
{
  /// <summary>
  /// Holds a simple static physics object.  Creation is done with a delegate 
  /// function to make the class more flexible.
  /// </summary>
  sealed class StaticPhysicsTransformComponent
    : PhysicsTransformComponent
  {
    private static readonly ILog Log = LogManager.GetLogger(
      MethodBase.GetCurrentMethod().DeclaringType);

    #region Private Fields
    private readonly Func<World, Body> m_bodyCreator;
    #endregion

    /// <summary>
    /// Create the component.
    /// </summary>
    /// <param name="parent"></param>
    /// <param name="pm"></param>
    /// <param name="bodyCreator"></param>
    public StaticPhysicsTransformComponent(Entity parent, PhysicsManager pm,
      Func<World, Body> bodyCreator) 
      : base(parent, pm)
    {
      if (bodyCreator == null)
      {
        throw new ArgumentNullException("bodyCreator");
      }

      m_bodyCreator = bodyCreator;
      NeedsUpdate = false;
    }

    #region PhysicsTransformComponent Implementation

    public override bool Initialize()
    {
      Body = m_bodyCreator(World);
      if (Body == null)
      {
        Log.ErrorFormat("{0} had body creator fail to return a body",
          Parent.FullName);
        return false;
      }
      
      if (Body.BodyType != BodyType.Static)
      {
        Log.WarnFormat("{0} created a body that is not static", 
          Parent.FullName);
      }

      Initialized = true;
      return true;
    }

    #endregion
  }
}
