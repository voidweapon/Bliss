using System;

namespace Bliss
{
    public class GamePlayEntity : Entity
    {
        protected override void RegisterToSystem()
        {
            if (LogicScene is not GameplayWorld gameplayWorld)
            {
                string sceneType = LogicScene?.GetType().FullName ?? "<null>";
                throw new InvalidOperationException(
                    $"Gameplay entity '{GetType().FullName}' must belong to '{nameof(GameplayWorld)}', but its scene is '{sceneType}'.");
            }

            gameplayWorld.Register(this);
        }
    }
}