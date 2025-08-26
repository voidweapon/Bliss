using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Bliss.ProjectRule.Analyzer
{
    public static class AnalyzerHelper
    {
        /// <summary>
        /// 判断指定的程序集是否需要分析
        /// </summary>
        /// <param name="assemblyName"></param>
        /// <param name="targetAssemblyNames"></param>
        /// <returns></returns>
        public static bool IsAssemblyNeedAnalyze(string assemblyName, params string[] targetAssemblyNames)
        {
#if PROJECT_RULE_TEST
            return true;
#endif            
            if (assemblyName == null)
            {
                return false;
            }

            foreach (string analyzeAssemblyName in targetAssemblyNames)
            {
                if (assemblyName == analyzeAssemblyName)
                {
                    return true;
                }
            }

            return false;
        }
        
        /// <summary>
        /// 获取所有指定类型的子节点
        /// </summary>
        public static IEnumerable<T> DescendantNodes<T>(this SyntaxNode syntaxNode) where T : SyntaxNode
        {
            foreach (var descendantNode in syntaxNode.DescendantNodes())
            {
                if (descendantNode is T node)
                {
                    yield return node;
                }
            }
        }
        
        /// <summary>
        /// 获取语法节点所属的ClassDeclarationSyntax
        /// </summary>
        public static ClassDeclarationSyntax? GetParentClassDeclaration(this SyntaxNode syntaxNode)
        {
            SyntaxNode? parentNode = syntaxNode.Parent;
            while (parentNode != null)
            {
                if (parentNode is ClassDeclarationSyntax classDeclarationSyntax)
                {
                    return classDeclarationSyntax;
                }

                parentNode = parentNode.Parent;
            }

            return null;
        }
        
    }
}