using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
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
        
        /// <summary>
        /// 判断类是否为partial类
        /// </summary>
        public static bool IsPartial(this ClassDeclarationSyntax classDeclaration)
        {
            foreach (var modifier in classDeclaration.Modifiers)
            {
                if (modifier.IsKind(SyntaxKind.PartialKeyword))
                {
                    return true;
                }
            }

            return false;
        }

        public static string? GetNameSpace(this ITypeSymbol namedTypeSymbol)
        {
            INamespaceSymbol? namespaceSymbol = namedTypeSymbol.ContainingNamespace;
            string? namespaceName = namespaceSymbol?.Name;
            while (namespaceSymbol?.ContainingNamespace != null)
            {
                namespaceSymbol = namespaceSymbol.ContainingNamespace;
                if (string.IsNullOrEmpty(namespaceSymbol.Name))
                {
                    break;
                }

                namespaceName = $"{namespaceSymbol.Name}.{namespaceName}";
            }

            if (string.IsNullOrEmpty(namespaceName))
            {
                return null;
            }

            return namespaceName;
        }
        
        
        /// <summary>
        ///     INamedTypeSymbol 获取指定类型的第一个Attribute
        /// </summary>
        public static AttributeData? GetFirstAttribute(this INamedTypeSymbol namedTypeSymbol, string attributeFullName, bool fullName = false)
        {
            foreach (AttributeData? attributeData in namedTypeSymbol.GetAttributes())
            {
                if (fullName)
                {
                    if (attributeData.AttributeClass?.ToString() == attributeFullName)
                    {
                        return attributeData;
                    }
                }
                if (attributeData.AttributeClass?.Name == attributeFullName)
                {
                    return attributeData;
                }
            }

            return null;
        }
        
        /// <summary>
        ///     INamedTypeSymbol 获取指定类型的Attribute
        /// </summary>
        public static List<AttributeData> GetAttribute(this INamedTypeSymbol namedTypeSymbol, string attributeFullName, bool fullName = false)
        {
            List<AttributeData> result = new List<AttributeData>();
            foreach (AttributeData? attributeData in namedTypeSymbol.GetAttributes())
            {
                if (fullName)
                {
                    if (attributeData.AttributeClass?.ToString() == attributeFullName)
                    {
                        result.Add(attributeData);
                    }
                }
                if (attributeData.AttributeClass?.Name == attributeFullName)
                {
                    result.Add(attributeData);
                }
            }

            return result;
        }

        /// <summary>
        ///     INamedTypeSymbol 是否含有指定接口
        /// </summary>
        public static bool HasInterface(this INamedTypeSymbol namedTypeSymbol, string InterfaceName)
        {
            foreach (INamedTypeSymbol? iInterface in namedTypeSymbol.AllInterfaces)
            {
                if (iInterface.IsInterface(InterfaceName))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 某个接口symbol 是否是指定的接口
        /// </summary>

        public static bool IsInterface(this INamedTypeSymbol namedTypeSymbol, string InterfaceName)
        {
            return $"{namedTypeSymbol.GetNameSpace()}.{namedTypeSymbol.Name}" == InterfaceName;
        }


        /// <summary>
        /// Gets the set of interfaces that this type directly implements, and sub of <paramref name="parentInterfaceName"/>
        /// </summary>
        /// <param name="namedTypeSymbol"></param>
        /// <param name="parentInterfaceName"></param>
        public static ImmutableArray<INamedTypeSymbol> GetInterfacesSubOf(this INamedTypeSymbol namedTypeSymbol, string parentInterfaceName)
        {
            var resultBuilder = ImmutableArray.CreateBuilder<INamedTypeSymbol>();
            foreach (INamedTypeSymbol? directInterface in namedTypeSymbol.Interfaces)
            {
                foreach (INamedTypeSymbol? parentInterface in directInterface.AllInterfaces)
                {
                    if (parentInterface.Name == parentInterfaceName)
                    {
                        resultBuilder.Add(directInterface);
                    }
                }
            }
            
            return resultBuilder.ToImmutable();
        }

        
        public static string GetMethodNameByInterface(this INamedTypeSymbol namedTypeSymbol)
        {
            if (namedTypeSymbol.TypeKind != TypeKind.Interface)
            {
                throw new ArgumentException("this INamedTypeSymbol is not interface", nameof(namedTypeSymbol));
            }
            
            string methodName = namedTypeSymbol.Name;
            if (methodName.StartsWith("I"))
            {
                methodName = methodName.Substring(1);
            }
            return methodName;
        }
        
        /// <summary>
        /// 包含命名空间的名字
        /// </summary>
        /// <param name="namedTypeSymbols"></param>
        /// <returns></returns>
        public static string GetFullName(this INamedTypeSymbol namedTypeSymbols)
        {
            return namedTypeSymbols.ToDisplayString();
        }
        
        /// <summary>
        /// 不包含命名空间的名字
        /// </summary>
        /// <param name="namedTypeSymbols"></param>
        /// <returns></returns>
        public static string GetName(this INamedTypeSymbol namedTypeSymbols)
        {
            return namedTypeSymbols.Name;
        }

        public static void GetTypeArgumentFullNames(this INamedTypeSymbol namedTypeSymbol, List<string> names)
        {
            foreach (var typeArgument in namedTypeSymbol.TypeArguments)
            {
                names.Add(typeArgument.ToDisplayString());
            }
        }
        
        public static void GetTypeArgumentNames(this INamedTypeSymbol namedTypeSymbol, List<string> names)
        {
            foreach (var typeArgument in namedTypeSymbol.TypeArguments)
            {
                if (typeArgument.IsValueType && typeArgument.SpecialType != SpecialType.None)
                {
                    names.Add(typeArgument.ToString());
                }
                else
                {
                    names.Add(typeArgument.Name);
                }
            }
        }

        public static void GetParamFullNames(this IMethodSymbol methodSymbol, List<string> names)
        {
            foreach (var parameter in methodSymbol.Parameters)
            {
                if (parameter == null)
                {
                    continue;
                }
                
                if (parameter.IsThisParameter())
                {
                    names.Add($"this {parameter.Type.ToDisplayString()}");
                }
                else
                {
                    names.Add(parameter.Type.ToDisplayString());

                }
            }
        }
        
        public static void GetParamNames(this IMethodSymbol methodSymbol, List<string> names)
        {
            foreach (var parameter in methodSymbol.Parameters)
            {
                if (parameter == null)
                {
                    continue;
                }
                
                if (parameter.IsThisParameter())
                {
                    names.Add($"this {parameter.ToDisplayString()}");
                }
                else
                {
                    names.Add(parameter.ToDisplayString());

                }
            }
        }
        
        /// <summary>
        /// 是不是 this 参数 (this T name)
        /// </summary>
        /// <param name="parameterSymbol"></param>
        /// <returns></returns>
        public static bool IsThisParameter(this IParameterSymbol parameterSymbol)
        {
            if (!parameterSymbol.DeclaringSyntaxReferences.IsEmpty)
            {
                SyntaxReference? syntaxRef = parameterSymbol.DeclaringSyntaxReferences.FirstOrDefault();
                if (syntaxRef != null)
                {
                    if (syntaxRef.GetSyntax() is ParameterSyntax parameterSyntax)
                    {
                        if (parameterSyntax.Modifiers.IndexOf(SyntaxKind.ThisKeyword) != -1)
                        {
                           return true;
                        }
                    }
                }
            }
            return false;
        }

        
        public static string GetAttributeClassNameWithoutNamespace(this AttributeSyntax attributeSyntax)
        {
            string attributeName = attributeSyntax.Name.ToString();
            if (attributeName.EndsWith("Attribute"))
            {
                return attributeName;
            }

            return $"{attributeName}Attribute";
        }
        
        /// <summary>
        ///     INamedTypeSymbol 是否有指定的Attribute
        /// <param name="attributeFullName">full name</param>
        /// </summary>
        public static bool HasAttribute(this ITypeSymbol typeSymbol, string attributeFullName)
        {
            foreach (AttributeData? attributeData in typeSymbol.GetAttributes())
            {
                if (attributeData.AttributeClass?.ToString() == attributeFullName)
                {
                    return true;
                }
            }

            return false;
        }
        
        public static bool HasAttributeInTypeAndBaseTypes(this ITypeSymbol typeSymbol, string attributeName)
        {
            if (typeSymbol.HasAttribute(attributeName))
            {
                return true;
            }

            foreach (var baseType in typeSymbol.BaseTypes())
            {
                if (baseType.HasAttribute(attributeName))
                {
                    return true;
                }
            }
            return false;
        }

        public static bool TryGetAttributeInDeepestBassType(this ITypeSymbol typeSymbol,
            string attributeFullName,
            out ITypeSymbol deepestBassType)
        {
            deepestBassType = null;
            if (typeSymbol.HasAttribute(attributeFullName))
            {
                deepestBassType = typeSymbol;
            }
            
            INamedTypeSymbol? symbolNode = typeSymbol.BaseType;
            while (symbolNode != null)
            {
                if (symbolNode is ITypeSymbol)
                {
                    if (symbolNode.HasAttribute(attributeFullName))
                    {
                        deepestBassType = symbolNode;
                    }
                }
                symbolNode =  symbolNode.BaseType;   
            }
            
            return deepestBassType != null;
        }
        
        /// <summary>
        ///     INamedTypeSymbol 获取指定类型的第一个Attribute
        /// </summary>
        public static AttributeData? GetFirstAttribute(this INamedTypeSymbol namedTypeSymbol, string attributeFullName)
        {
            foreach (AttributeData? attributeData in namedTypeSymbol.GetAttributes())
            {
                if (attributeData.AttributeClass?.ToString() == attributeFullName)
                {
                    return attributeData;
                }
            }

            return null;
        }
        
        public static IEnumerable<ITypeSymbol> BaseTypes(this ITypeSymbol typeSymbol)
        {
            ITypeSymbol? baseType = typeSymbol.BaseType;
            while (baseType!=null)
            {
                yield return baseType;
                baseType = baseType.BaseType;
            }
        }

        public static bool HasAttributeDeclaredOnly(this MemberDeclarationSyntax syntaxNode, string attributeName)
        {
            //Syntax Node上只能获取字面值，无法解析出Full Name，所以统一用Short Name
            int dotIndex = attributeName.LastIndexOf('.');
            if (dotIndex != -1)
            {
                attributeName = attributeName.Substring(dotIndex + 1);
            }
            
            foreach (AttributeListSyntax attributeListSyntax in syntaxNode.AttributeLists)
            {
                AttributeSyntax? attribute = attributeListSyntax.Attributes.FirstOrDefault();
                if (attribute == null)
                {
                    continue;
                }
                
                string attributeClassName = attribute.GetAttributeClassNameWithoutNamespace();
                if (attributeClassName == attributeName)
                {
                    return true;
                }
            }
            return false;
        }

        public static bool HasAttributeInInheritance(this ClassDeclarationSyntax syntaxNode, string attributeName)
        {
            ClassDeclarationSyntax? node = syntaxNode;
            while (node != null)
            {
                if (node.HasAttributeDeclaredOnly(attributeName))
                {
                    return true;
                }
                node = node.Parent as ClassDeclarationSyntax;
            }

            return false;
        }

        public static bool TryGetBaseClassHasAttributeInInheritance(this ClassDeclarationSyntax syntaxNode, 
            string attributeName, 
            out ClassDeclarationSyntax baseClassSyntaxNode)
        {
            baseClassSyntaxNode = null;
            ClassDeclarationSyntax? node = syntaxNode;
            while (node != null)
            {
                if (node.HasAttributeDeclaredOnly(attributeName))
                {
                    baseClassSyntaxNode = node;
                    return true;
                }

                if (node.BaseList != null && node.BaseList.Types.Count > 0)
                {
                    var baseType = node.BaseList.Types[0];
                }
                
                node = node.Parent as ClassDeclarationSyntax;
            }

            return false;
        }
        
        public static string GetFullName(this ClassDeclarationSyntax classDeclaration)
        {
           StringBuilder sb = new StringBuilder();
           sb.Append(classDeclaration.Identifier.Text);
           SyntaxNode node = classDeclaration.Parent;
           while (node != null)
           {
               if (node is NamespaceDeclarationSyntax namespaceDeclaration)
               {
                   sb.Insert(0, $"{namespaceDeclaration.Name.ToFullString()}.");
               }
                node = node.Parent;
           }
           return sb.ToString();
        }
    }
}