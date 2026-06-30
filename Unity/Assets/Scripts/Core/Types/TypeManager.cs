using System;
using System.Collections.Generic;
using System.Reflection;
using Bliss.Base.Collections;

namespace Bliss.Core
{
    public class TypeManager : Singleton<TypeManager>, ISingletonInit<List<Assembly>>
    {
        /// <summary>
        /// Type Full Name -> Type
        /// </summary>
        private Dictionary<string, Type> _types = new Dictionary<string, Type>();
        
        /// <summary>
        /// Attribute Type -> Type
        /// </summary>
        private MultiMapSet<Type, Type> _typeCategorySet = new MultiMapSet<Type, Type>();

        public void Init(List<Assembly> assemblies)
        {
            var addTypes = AssemblyHelper.GetAssemblyTypes(assemblies);
            foreach ((string fullName, Type type) in addTypes)
            {
                _types[fullName] = type;
                if (type.IsAbstract)
                {
                    continue;
                }
                
                object[] attributes = type.GetCustomAttributes(typeof(BaseAttribute), true);
                foreach (BaseAttribute attribute in attributes)
                {
                    _typeCategorySet.Add(attribute.GetType(), type);
                }
            }
        }

        /// <summary>
        /// 根据属性类型获取类型集合
        /// </summary>
        /// <param name="attributeType">属性类型</param>
        /// <returns>具有指定属性的类型集合</returns>
        public HashSet<Type> GetTypes(Type attributeType)
        {
            if (!_typeCategorySet.ContainsKey(attributeType))
            {
                return new HashSet<Type>();
            }
            
            return _typeCategorySet[attributeType];
        }
        
        public Type GetType(string typeName)
        {
            return _types[typeName];
        }

        public Dictionary<string, Type> GetAllTypes()
        {
            return _types;
        }

        /// <summary>
        /// 重新创建被标记为<see cref="SingletonMangerAttribute"/>到Singleton
        /// </summary>
        public void CreateAutoSingletonManager()
        {
            var hashSet = GetTypes(typeof (SingletonMangerAttribute));
            
            //排序SingletonManger
            using var managerTypes = ListComponent<Type>.Create();
            managerTypes.AddRange(hashSet);
            managerTypes.Sort(_sortTypeSorter);
            
            foreach (Type type in managerTypes)
            {
                object obj = Activator.CreateInstance(type);
                ISingletonInit singletonInit = (ISingletonInit)obj;
                if (singletonInit == null)
                {
                    World.Instance.AddSingleton((ASingleton)obj);
                    Log.Info($"Singleton manager {type.FullName} does not implement {nameof(ISingletonInit)}");
                    continue;
                }
                
                singletonInit.Init();
                World.Instance.AddSingleton((ASingleton)obj);
            }
        }
        
        private static SingletonMangerTypeSorter _sortTypeSorter = new SingletonMangerTypeSorter();
        private class SingletonMangerTypeSorter : IComparer<Type>
        {
            public int Compare(Type x, Type y)
            {
                int orderX = 0;
                int orderY = 0;

                object[] attrsX = x.GetCustomAttributes(typeof(SingletonMangerAttribute), true);
                if (attrsX.Length > 0)
                {
                    SingletonMangerAttribute attrX = attrsX[0] as SingletonMangerAttribute;
                    orderX = attrX.Priority;
                }

                object[] attrsY = y.GetCustomAttributes(typeof(SingletonMangerAttribute), true);
                if (attrsY.Length > 0)
                {
                    SingletonMangerAttribute attrY = attrsY[0] as SingletonMangerAttribute;
                    orderY = attrY.Priority;
                }

                return orderX.CompareTo(orderY);
            }
        }
    }
}
