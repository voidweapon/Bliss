using System;

namespace Bliss
{
    /// <summary>Creates GameplayWorld instances with their Root update drivers.</summary>
    public static class GameplayWorldFactory
    {
        /// <summary>Default world identifier within its parent.</summary>
        public const long DEFAULT_WORLD_ID = 1;
        /// <summary>Default world name.</summary>
        public const string DEFAULT_WORLD_NAME = "GameplayWorld";

        /// <summary>Creates a world under the supplied parent and connects it to Root updates.</summary>
        /// <param name="parent">Parent entity on the owning Root.</param>
        /// <param name="id">World identifier within the parent.</param>
        /// <param name="instanceId">World instance identifier.</param>
        /// <param name="name">World name.</param>
        /// <returns>The attached world.</returns>
        /// <exception cref="ArgumentNullException">The parent is null.</exception>
        /// <exception cref="AggregateException">Driver creation and world cleanup both fail.</exception>
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
            try
            {
                gameplayWorld.AddComponent<GameplayWorldDriver>();
                gameplayWorld.AddComponent<GameplayWorldSimulationDriver>();
            }
            catch (Exception exception)
            {
                try
                {
                    gameplayWorld.Dispose();
                }
                catch (Exception cleanupException)
                {
                    throw new AggregateException("GameplayWorld driver creation and cleanup failed.", exception, cleanupException);
                }

                throw;
            }

            return gameplayWorld;
        }
    }
}
