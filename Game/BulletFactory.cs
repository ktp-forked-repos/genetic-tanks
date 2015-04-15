using System.Reflection;
using GeneticTanks.Game.Components.Bullet;
using GeneticTanks.Game.Events;
using log4net;
using Microsoft.Xna.Framework;

namespace GeneticTanks.Game
{
  static class BulletFactory
  {
    private static readonly ILog Log = LogManager.GetLogger(
      MethodBase.GetCurrentMethod().DeclaringType);

    public static EntityManager EntityManager { get; set; }
    public static PhysicsManager PhysicsManager { get; set; }
    public static EventManager EventManager { get; set; }

    public static Entity CreateBullet(uint shooter, float damage, 
      Vector2 position, Vector2 velocity)
    {
      var id = EntityManager.NextId;
      var name = string.Format("bullet {0}", id);
      Entity bullet = new Entity(id, name);
      bullet.AddComponent(new BulletPhysicsTransformComponent(bullet, 
        PhysicsManager, EventManager));
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
        Log.ErrorFormat("Failed to initialize {0}, discarding", bullet.FullName);
        bullet.Dispose();
        return null;
      }

      bullet.Transform.Position = position;

      EntityManager.AddEntity(bullet);
      EventManager.QueueEvent(new ShotFiredEvent(shooter, bullet.Id));
      return bullet;
    }
  }
}
