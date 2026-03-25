using System;

namespace Bliss.Base
{
    public class SubClassOf<T>
    {
        public Type Class { get; private set; }

        private SubClassOf()
        {
            
        }

        public SubClassOf(Type from)
        {
            if (!from.IsSubclassOf(typeof(T)))
            {
                throw new ArgumentException($"{from.FullName} is not a subclass of {typeof(T).FullName}");
            }
            
            Class = from;
        }
        
        public static implicit operator SubClassOf<T>(Type from)
        {
            SubClassOf<T> result = new SubClassOf<T>(from);
            return result;
        }
    }
}