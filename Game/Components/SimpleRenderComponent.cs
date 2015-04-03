
using System;
using System.Reflection;
using log4net;
using SFML.Graphics;

namespace GeneticTanks.Game.Components
{
  /// <summary>
  /// Renders a simple primitive shape.  Creation of the shape is delegated to 
  /// a function for better flexibility.
  /// </summary>
  sealed class SimpleRenderComponent
    : RenderComponent
  {
    private static readonly ILog Log = LogManager.GetLogger(
      MethodBase.GetCurrentMethod().DeclaringType);

    #region Private Fields
    private readonly Func<Shape> m_shapeCreator; 
    private Shape m_shape;
    #endregion

    /// <summary>
    /// Create the component.
    /// </summary>
    /// <param name="parent"></param>
    /// <param name="shapeCreator"></param>
    public SimpleRenderComponent(Entity parent, Func<Shape> shapeCreator) 
      : base(parent)
    {
      if (shapeCreator == null)
      {
        throw new ArgumentNullException("shapeCreator");
      }

      m_shapeCreator = shapeCreator;
      NeedsUpdate = false;
    }

    public Color FillColor
    {
      get { return m_shape.FillColor; }
      set { m_shape.FillColor = value; }
    }

    public Color OutlineColor
    {
      get { return m_shape.OutlineColor; }
      set { m_shape.OutlineColor = value; }
    }

    public float OutlineThickness
    {
      get { return m_shape.OutlineThickness; }
      set { m_shape.OutlineThickness = value; }
    }

    #region RenderComponent Implementation

    public override bool Initialize()
    {
      if (!base.Initialize())
      {
        return false;
      }

      m_shape = m_shapeCreator();
      if (m_shape == null)
      {
        Log.ErrorFormat("{0} had shape creator fail to return shape",
          Parent.FullName);
        return false;
      }

      Initialized = true;
      return true;
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
      target.Draw(m_shape, RenderStates);
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
