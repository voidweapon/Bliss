namespace Bliss.ProjectRule.Analyzer
{
    public static class AnalyzeAssembly
    {
        private const string Unity_Base = "Bliss.Base";
        private const string Unity_Core = "Bliss.Core";
        private const string Unity_Model = "Bliss.Model";
        private const string Unity_Logic = "Bliss.Logic";
        private const string Unity_ViewModel = "Bliss.ViewModel";
        private const string Unity_ViewLogic = "Bliss.ViewLogic";
        
        private const string Base = "Base";
        private const string Core = "Core";
        private const string Model = "Model";
        private const string Logic = "Logic";


        /// <summary>
        /// 需要分析的程序集清单
        /// </summary>
        public static readonly string[] All = new string[]
        {
            Core, 
            Model, Logic,
            Unity_Core, 
            
            Unity_Model, Unity_Logic,
            Unity_ViewModel, Unity_ViewLogic,
            
            "Bliss.ProjectRule.TestData",
        };

        public static readonly string[] AllModel = new string[]
        {
            Model,
            Unity_Model,
        };
    }
}