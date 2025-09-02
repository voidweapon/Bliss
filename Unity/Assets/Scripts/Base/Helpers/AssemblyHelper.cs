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
                    types[type.FullName] = type;
                }
            }

            return types;
        }
    }
}