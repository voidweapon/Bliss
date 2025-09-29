using System;
using System.Collections.Generic;
using System.Reflection;

namespace Bliss
{
    public class AssemblyHelper
    {
        public static Dictionary<string, Type> GetAssemblyTypes(params Assembly[] args)
        {
            Dictionary<string, Type> types = new Dictionary<string, Type>();

            foreach (Assembly assembly in args)
            {
                foreach (Type type in assembly.GetTypes())
                {
                    if(type.FullName == null) continue;
                    
                    types[type.FullName] = type;
                }
            }

            return types;
        }
        
        public static Dictionary<string, Type> GetAssemblyTypes(List<Assembly> args)
        {
            Dictionary<string, Type> types = new Dictionary<string, Type>();

            foreach (Assembly assembly in args)
            {
                foreach (Type type in assembly.GetTypes())
                {
                    if(type.FullName == null) continue;
                    
                    types[type.FullName] = type;
                }
            }

            return types;
        }
    }
}