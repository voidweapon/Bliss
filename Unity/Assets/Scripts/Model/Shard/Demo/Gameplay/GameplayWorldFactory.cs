using System;

namespace Bliss
{
    public static class GameplayWorldFactory
    {
        public static GameplayWorld Create(Entity parent, long id, long instanceId, string name)
        {
            if (parent == null)
            {
                throw new ArgumentNullException(nameof(parent));
            }

            var gameplayWorld = new GameplayWorld(
                parent.RootScene().Root,
                id,
                instanceId,
                name);
            parent.AddChild(gameplayWorld);
            return gameplayWorld;
        }
    }
}
