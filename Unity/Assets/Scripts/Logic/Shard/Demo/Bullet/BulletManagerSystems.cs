namespace Bliss
{
    public static partial class BulletManagerSystems
    {
        public static void Add(this BulletManager self, Bullet bullet)
        {
            self.DicEntityId2Bullet.Add(bullet.Id, bullet);
        }

        public static void Remove(this BulletManager self, long id)
        {
            self.DicEntityId2Bullet.Remove(id);
        }
    }
}