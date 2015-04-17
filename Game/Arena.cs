
using System;
using System.Reflection;
using FarseerPhysics.Dynamics;
using FarseerPhysics.Factories;
using GeneticTanks.Extensions;
using GeneticTanks.Game.Components;
using GeneticTanks.Game.Managers;
using log4net;
using Microsoft.Xna.Framework;
using SFML.Graphics;
using SFML.Window;

namespace GeneticTanks.Game
{
  sealed class Arena
    : IDisposable
  {
    private static readonly ILog Log = LogManager.GetLogger(
      MethodBase.GetCurrentMethod().DeclaringType);

    private uint m_entity;

    public Arena(Vector2 dimensions)
    {
      Dimensions = dimensions;
    }

    public Vector2 Dimensions { get; private set; }

    public bool Initialize()
    {
      var halfWidth = Dimensions.X / 2f;
      var halfHeight = Dimensions.Y / 2f;
      var upperLeft = new Vector2(-halfWidth, halfHeight);
      var lowerLeft = new Vector2(-halfWidth, -halfHeight);
      var lowerRight = new Vector2(halfWidth, -halfHeight);
      var upperRight = new Vector2(halfWidth, halfHeight);

      var entity = new Entity(EntityManager.NextId, "wall");
      entity.AddComponent(new StaticPhysicsTransformComponent(
        entity, Globals.PhysicsManager, world =>
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
      var src = new SimpleRenderComponent(entity, RenderDepth.Terrain,
        () => new RectangleShape(Dimensions.ToVector2f())
        {
          Origin = new Vector2f(halfWidth, halfHeight)
        });
      entity.AddComponent(src);

      if (!entity.Initialize())
      {
        entity.Dispose();
        Log.Error("Failed to initialize arena");
        return false;
      }

      src.FillColor = Color.Transparent;
      src.OutlineColor = Color.Black;
      src.OutlineThickness = 1f;

      Globals.EntityManager.AddEntity(entity);
      m_entity = entity.Id;
      return true;
    }

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

      Globals.EntityManager.RemoveEntity(m_entity);

      m_disposed = true;
    }

    ~Arena()
    {
      Dispose(false);
    }

    #endregion
  }
}
