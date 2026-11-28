namespace Bliss;

// This test project links Core sources without running the EntitySystem source generator.
/// <summary>Provides the generated internal-message update system to the source-linked test project.</summary>
public static partial class ProcessInnerMessageProxySystems
{
    /// <summary>Runs the same proxy update as the generated production system.</summary>
    [EntitySystem]
    public sealed class ProcessInnerMessageProxy_UpdateSystem : UpdateSystem<ProcessInnerMessageProxy>
    {
        /// <inheritdoc />
        protected override void Update(ProcessInnerMessageProxy entity)
        {
            entity.Update();
        }
    }
}
