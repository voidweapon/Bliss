using System;
using System.Collections.Generic;
using System.Reflection;
using Bliss.Base.Collections;

namespace Bliss.Core
{
    public class TypeManager : Singleton<TypeManager>, ISingletonInit<Assembly[]>
    {
        /// <summary>
        /// Type Full Name -> Type
        /// </summary>
        private Dictionary<string, Type> _types = new Dictionary<string, Type>();
        
        /// <summary>
        /// Attribute Type -> Type
        /// </summary>
        private MultiMapSet<Type, Type> _typeCategorySet = new MultiMapSet<Type, Type>();

        public void Init(Assembly[] assemblies)
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
            return this._types[typeName];
        }
    }
}
