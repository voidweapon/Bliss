using System.ComponentModel;
using System.Reflection;

namespace Bliss.Core
{
    public abstract class IStaticMethod
    {
        public abstract void Run();
        public abstract void Run(object a);
        public abstract void Run(object a, object b);
        public abstract void Run(object a, object b, object c);
    }
    
    
    public class StaticMethod : IStaticMethod
    {
        private readonly MethodInfo methodInfo;

        private readonly object[] param;

        public StaticMethod(Assembly assembly, string typeName, string methodName)
        {
            methodInfo = assembly.GetType(typeName).GetMethod(methodName);
            param = new object[methodInfo.GetParameters().Length];
        }

        public override void Run()
        {
            if (methodInfo.GetParameters().Length != 0)
            {
                throw new InvalidEnumArgumentException();
            }
            methodInfo.Invoke(null, param);
        }

        public override void Run(object a)
        {
            if (methodInfo.GetParameters().Length != 1)
            {
                throw new InvalidEnumArgumentException();
            }
            
            param[0] = a;
            methodInfo.Invoke(null, param);
        }

        public override void Run(object a, object b)
        {
            if (methodInfo.GetParameters().Length != 2)
            {
                throw new InvalidEnumArgumentException();
            }
            
            param[0] = a;
            param[1] = b;
            methodInfo.Invoke(null, param);
        }

        public override void Run(object a, object b, object c)
        {
            if (methodInfo.GetParameters().Length != 3)
            {
                throw new InvalidEnumArgumentException();
            }
            
            param[0] = a;
            param[1] = b;
            param[2] = c;
            methodInfo.Invoke(null, param);
        }
    }
}