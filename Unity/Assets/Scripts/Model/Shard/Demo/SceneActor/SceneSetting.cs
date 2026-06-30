using System;
using System.Collections.Generic;
using Bliss.Base.Collections;
using Bliss.Core;
using MemoryPack;
using Unity.Mathematics;

namespace Bliss
{
    using SerializeInclude = MemoryPackIncludeAttribute;
    
    [MemoryPackable]
    public partial class SceneSetting
    {
		public List<SceneActorData> LevelActors = new List<SceneActorData>();
    }
    
    [MemoryPackable]
    public partial class SceneActorData
    {
        public string PrefabPath;
        public long NetworkIdentity;
        
        /// <summary>
        /// ASceneActorConfig Type Stable Hash
        /// </summary>
        public int TypeIdentifier;
        
        /// <summary>
        /// ASceneActorConfig Raw Data
        /// </summary>
        public byte[] Data;
    }
    
    [AttributeUsage(AttributeTargets.Class)]
    public class SceneActorMarkAttribute : BaseAttribute
    {
       
    }


    public abstract class ASceneActorConfig
    {
        public float3 Position;
        public quaternion Rotation;
    }

    public abstract class ASceneActorCmp : Entity, ISupportSerialize
    {
        public SceneActorData RawConfigData { get; private set; }
        public ASceneActorConfig Config { get; private set; }
        
        public void SetConfig(ASceneActorConfig config)
        {
            Config = config;
        }

        public void SetData(SceneActorData data)
        {
            RawConfigData = data;
        }
        
        public void EndSceneInit()
        {
            GetEntitySystem().CallEventSystem<IAfterSceneInit, IAfterSceneInitSystem>(this);
        }
    }

    public interface IAfterSceneInit : IEntitySystemConstraint
    {
        
    }
    
    /// <summary>
    /// pair <see cref="IAwake"/>
    /// </summary>
    /// <typeparam name="TParam"></typeparam>
    public interface IAfterSceneInitSystem : IEventSystem
    {
    }
    
    public abstract class AfterSceneInitSystem<TEntity> : SystemObject, IAfterSceneInitSystem where TEntity : Entity, IDestroy
    {
        public Type GetEntityType()
        {
            return typeof(TEntity);
        }

        public Type GetSystemCategoryType()
        {
            return typeof(IAfterSceneInitSystem);
        }

        public ESystemUpdateGroup GetUpdateGroup()
        {
            return ESystemUpdateGroup.None;
        }
        
        public virtual int GetPriority()
        {
            return 0;
        }
        
        public int CompareTo(ISystem other)
        {
            return GetPriority() - other.GetPriority();
        }

        public void Run(Entity entity)
        {
            AfterSceneInit ((TEntity)entity);
        }
        
        protected abstract void AfterSceneInit (TEntity entity);
    }
    
    [SceneActorMark]
    public abstract class ASceneActorCmp<TActor, TConfig> : ASceneActorCmp
        where TActor : ASceneActorCmp<TActor, TConfig> 
        where TConfig : ASceneActorConfig
    {
        public TConfig SceneActorCmpConfig => (TConfig)Config;
        public static Type ConfigType => typeof(TConfig);
    }

    [SingletonManger]
    public class SceneActorDataTypeManager : Singleton<SceneActorDataTypeManager>, ISingletonInit
    {
        private Dictionary<int, Type> typeIdentifierToType = new Dictionary<int, Type>();
        private Dictionary<Type, Type> configTypeToSceneActorType = new Dictionary<Type, Type>();
        
        public void Init()
        {
            typeIdentifierToType.Clear();
            
            var sceneActorTypes = TypeManager.Instance.GetTypes(typeof(SceneActorMarkAttribute));
            foreach (var sceneActorType in sceneActorTypes)
            {
                if (!sceneActorType.IsClass || sceneActorType.IsAbstract)
                {
                    continue;
                }

                Type baseType = sceneActorType.BaseType;
                if (baseType == null || !baseType.IsGenericType || baseType.GetGenericTypeDefinition() != typeof(ASceneActorCmp<,>))
                {
                    continue;
                }

                Type configType = baseType.GetGenericArguments()[1];
                int typeIdentifier = configType.FullName.GetStableHashCode();
                typeIdentifierToType.Add(typeIdentifier, configType);
                configTypeToSceneActorType.Add(configType, sceneActorType);
                Log.Info($"[SceneActorDataTypeManager] Bind {configType} : {sceneActorType}");
            }
        }
        
        public Type GetConfigType(int typeIdentifier)
        {
            if (typeIdentifierToType.TryGetValue(typeIdentifier, out var type))
            {
                return type;
            }

            return null;
        }
        
        public Type GetSceneActorType(Type configType)
        {
            configTypeToSceneActorType.TryGetValue(configType, out var sceneActorType);
            return sceneActorType;
        }
    }
}