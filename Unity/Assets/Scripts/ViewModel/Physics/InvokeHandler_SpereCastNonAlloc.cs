using Bliss.Core.EventSystem;
using Bliss.CoroutineLocks;
using Bliss.Physics;
using Bliss.Timer;

namespace Bliss
{
    [Invoke(PhysicalInvokeTypes.Physical_Scene_Query)]
    public class InvokeHandler_SpereCastNonAlloc : AInvokeHandler<Invoke_SpereCastNonAlloc, int>
    {
        public override int Handle(Invoke_SpereCastNonAlloc args)
        {
            int count = PhysicsHelper.SpereCastNonAlloc(args.origin, args.radius, args.direction, args.maxDistance, args.layerMask, out var results);
            for (int i = 0; i < count; i++)
            {
                args.results[i] = new GameplayRaycastHit()
                {
                    point = results[i].point,
                    normal = results[i].normal,
                    distance = results[i].distance,
                    Collider = default
                };
            }
            return count;
        }
    }
}