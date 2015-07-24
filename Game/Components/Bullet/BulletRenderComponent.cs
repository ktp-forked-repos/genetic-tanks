using System.Reflection;
using GeneticTanks.Game.Managers;
using log4net;
using SFML.Graphics;
using SFML.Window;

namespace GeneticTanks.Game.Components.Bullet
{
  // TODO: Replace me with a SimpleRenderComponent
  /// <summary>
  /// Draws a bullet as a simple black circle.
  /// </summary>
  sealed class BulletRenderComponent
    : RenderComponent
  {
    private static readonly ILog Log = LogManager.GetLogger(
      MethodBase.GetCurrentMethod().DeclaringType);

    private CircleShape m_shape;

    /// <summary>
    /// Create the component.
    /// </summary>
    /// <param name="parent"></param>
    public BulletRenderComponent(Entity parent) 
      : base(parent)
    {
      ZDepth = RenderDepth.Bullet;
      NeedsUpdate = false;
    }

    #region RenderComponent Implementation

    public override bool Initialize()
    {
      if (!base.Initialize())
      {
        return false;
      }

      BulletDataComponent data;
      if (!RetrieveSibling(out data))
      {
        return false;
      }

      m_shape = new CircleShape
      {
        Radius = data.Radius,
        FillColor = Color.Black,
        Origin = new Vector2f(data.Radius, data.Radius)
      };

      Initialized = true;
      return true;
    }

    public override void Enable()
    {
    }

    public override void Disable()
    {
    }

    public override void Deactivate()
    {
    }

    public override void Update(float deltaTime)
    {
    }

    public override void Draw(RenderTarget target)
    {
      if (!Initialized || target == null)
      {
        return;
      }

      RenderStates.Transform = Parent.Transform.GraphicsTransform;
      m_shape.Draw(target, RenderStates);
    }

    #endregion
    #region IDisposable Implementation

    private bool m_disposed = false;

    protected override void Dispose(bool disposing)
    {
      if (!Initialized || m_disposed)
      {
        return;
      }

      if (disposing)
      {
        m_shape.Dispose();
      }

      base.Dispose(disposing);
      m_disposed = true;
    }

    #endregion
  }
}
