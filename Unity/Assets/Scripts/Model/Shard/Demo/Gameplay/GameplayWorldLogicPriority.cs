namespace Bliss
{
    /// <summary>Named ordering points for GameplayWorld simulation callbacks.</summary>
    public static class GameplayWorldLogicPriority
    {
        /// <summary>Bullet firing-point overlap checks before movement.</summary>
        public const int BULLET_SPAWN_CHECK = 100;
        /// <summary>Character movement and prediction before shared movement simulation.</summary>
        public const int CHARACTER_MOVEMENT = 200;
        /// <summary>Shared priority for mover and kinematic simulation, ordered by registration.</summary>
        public const int MOVEMENT = 300;
        /// <summary>Bullet path collision checks after movement.</summary>
        public const int BULLET_PATH_CHECK = 400;
        /// <summary>Reserved final priority for server physics simulation.</summary>
        public const int PHYSICS = int.MaxValue;
    }
}
