using System.Collections.Generic;
using Bliss;
using Unity;

namespace Editor.BuildTools
{
    public  static class BuildDefine
    {
        public const string CodeDir = GlobalDefine.CodeDir;
        public const string BuildOutputDir = "Temp/Bin/Debug";

        public static readonly HashSet<string> HotfixProject = new HashSet<string>()
        {
            "Bliss.CoreModel.csproj",
            "Bliss.CoreLogic.csproj",
            "Bliss.Model.csproj",
            "Bliss.Logic.csproj",
            "Bliss.ViewModel.csproj",
            "Bliss.ViewLogic.csproj",
        };
    }
}