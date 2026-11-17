using System;

namespace Bliss
{
    /// <summary>
    /// Reports a failed LogicUpdate entry; the callback exception is available as the inner exception.
    /// </summary>
    public sealed class GameplayWorldLogicUpdateException : Exception
    {
        internal GameplayWorldLogicUpdateException(GameplayWorld world, string entryName, Exception innerException)
            : base($"LogicUpdate entry '{entryName}' failed in {nameof(GameplayWorld)} '{world.Name}'.", innerException)
        {
        }
    }
}
