namespace Bliss.ProjectRule.Analyzer
{
    public static class AnalyzeAssembly
    {
        private const string Base = "Bliss.Base";
        private const string Core = "Bliss.Core";
        private const string Model = "Bliss.Model";
        private const string Logic = "Bliss.Logic";
        private const string ViewModel = "Bliss.ViewModel";
        private const string ViewLogic = "Bliss.ViewLogic";

        /// <summary>
        /// 需要分析的程序集清单
        /// </summary>
        public static readonly string[] All = new string[]
        {
            Core, 
            Model, Logic,
            ViewModel, ViewLogic,
            "Bliss.ProjectRule.TestData",
        };
    }
}