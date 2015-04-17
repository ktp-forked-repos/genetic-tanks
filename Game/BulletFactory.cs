using System.Reflection;
using GeneticTanks.Extensions;
using GeneticTanks.Game.Components.Bullet;
using GeneticTanks.Game.Events;
using GeneticTanks.Game.Managers;
using log4net;
using Microsoft.Xna.Framework;

namespace GeneticTanks.Game
{
  static class BulletFactory
  {
    private static readonly ILog Log = LogManager.GetLogger(
      MethodBase.GetCurrentMethod().DeclaringType);

    public static Entity CreateBullet(uint shooter, float damage, 
      Vector2 position, Vector2 velocity)
    {
      var id = EntityManager.NextId;
      Entity bullet = new Entity(id, "bullet");
      bullet.AddComponent(new BulletPhysicsTransformComponent(bullet,
        Globals.PhysicsManager, Globals.EventManager));
      bullet.AddComponent(new BulletRenderComponent(bullet));
      bullet.AddComponent(new BulletDataComponent(bullet)
      {
        Damage = damage,
        FiringEntity = shooter,
        Radius = 0.25f,
        Velocity = velocity
      });

      if (!bullet.Initialize())
      {
        Log.ErrorFmt("Failed to initialize {0}, discarding", bullet.FullName);
        bullet.Dispose();
        return null;
      }

      bullet.Transform.Position = position;

      Globals.EntityManager.AddEntity(bullet);
      Globals.EventManager.QueueEvent(new ShotFiredEvent(shooter, bullet.Id));
      return bullet;
    }
  }
}
