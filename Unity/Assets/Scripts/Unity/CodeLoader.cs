using System.Reflection;

namespace Bliss.Core
{
    public class CodeLoader : Singleton<CodeLoader>
    {
        private Assembly ModelAssembly;
        private Assembly ViewModelAssembly;
    }
}