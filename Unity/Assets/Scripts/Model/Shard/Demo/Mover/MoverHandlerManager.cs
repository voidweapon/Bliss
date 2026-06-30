using System;
using System.Collections.Generic;
using System.Reflection;
using Bliss.Core;
using Bliss.Mathematics;
using Unity.Mathematics;

namespace Bliss
{
    [SingletonManger]
    public class MoverHandlerManager : Singleton<MoverHandlerManager>, ISingletonInit
    {
        private readonly Dictionary<Type, AMoverStageHandler> _dicConfigType2Handler = new();
        public void Init()
        {
            var handlerTypes = TypeManager.Instance.GetTypes(typeof(MoverStageHandlerAttribute));
            foreach (var handlerType in handlerTypes)
            {
                var attribute = handlerType.GetCustomAttribute(typeof(MoverStageHandlerAttribute)) as MoverStageHandlerAttribute;
                if (attribute == null)
                {
                    Log.Error($"Mover Stage Handler is have {nameof(MoverStageHandlerAttribute)}: {handlerType.Name}");
                    continue;
                }
                
                var handler = Activator.CreateInstance(handlerType) as AMoverStageHandler;
                if (handler == null)
                {
                    Log.Error($"Mover Stage Handler is  not {nameof(AMoverStageHandler)}: {handlerType.Name}");
                    continue;
                }
                this._dicConfigType2Handler.Add(attribute.ConfigType, handler);
            }
        }

        public AMoverStageHandler GetHandler(Type configType)
        {
            return _dicConfigType2Handler[configType];
        }
    }
    
    [AttributeUsage(AttributeTargets.Class, Inherited = true)]
    public class MoverStageHandlerAttribute : BaseAttribute
    {
        public Type ConfigType { get; private set; }
        
        public MoverStageHandlerAttribute(Type configType)
        {
            ConfigType = configType;
        }
    }

    public abstract class AMoverStageHandler : HandlerObject
    {
        public abstract DeltaMovement Calculate(MoverStageRuntime stageRuntime, long deltaMS, float weight);

        public abstract bool IsComplete(MoverStageRuntime stageRuntime);

    }

    public struct DeltaMovement
    {
        public float3 Position;
        public quaternion Rotation;

        public DeltaMovement(float3 pos)
        {
            Position = pos;
            Rotation = quaternion.identity;
        }
        
        public DeltaMovement(float3 pos, quaternion rot)
        {
            Position = pos;
            Rotation = rot;
        }
    }
}