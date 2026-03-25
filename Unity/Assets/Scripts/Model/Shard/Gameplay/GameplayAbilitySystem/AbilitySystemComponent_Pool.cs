using System;
using System.Collections.Generic;
using Bliss.Pool;

namespace Bliss.Gameplay.GameplayAbilitySystem
{
    /// <summary>
    /// isolate not gameplay logic
    /// </summary>
    public partial class AbilitySystemComponent
    {
        public static T Rent<T>()
        {
            return ObjectPool.Instance.Fetch<T>();
        }

        public static void Return<T>(T instance)
        {
            ObjectPool.Instance.Recycle(instance);
        }

        
        public static T NewObject<T>(Type classType)
        {
            return (T)Activator.CreateInstance(classType);
        }
    }
}