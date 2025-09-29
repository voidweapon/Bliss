using System.IO;
using System.Xml;
using UnityEditor;

namespace Editor.BuildTools
{
    public class CSProjectProcessor: AssetPostprocessor
    {
        public static string OnGeneratedCSProject(string path, string content)
        {
            foreach (var relaventCspj in BuildDefine.HotfixProject)
            {
                if (path.Contains(relaventCspj))
                {
                    return GenerateCustomProject(content);
                }
            }
            return content;
        }

        private static string GenerateCustomProject(string content)
        {
            XmlDocument doc = new();
            doc.LoadXml(content);
            var rootNode = doc.GetElementsByTagName("Project")[0];
            
            //Add Copy Dll Action
            var postBuild_target =  doc.CreateElement("Target", doc.DocumentElement.NamespaceURI);
            postBuild_target.SetAttribute("Name", "PostBuild");
            postBuild_target.SetAttribute("AfterTargets", "PostBuildEvent");
            newCopyAction(doc, postBuild_target, BuildDefine.CodeDir, "dll", "dll.bytes");
            newCopyAction(doc, postBuild_target, BuildDefine.CodeDir, "pdb", "pdb.bytes");
            newCopyAction(doc, postBuild_target, BuildDefine.BuildOutputDir, "dll", "dll");
            newCopyAction(doc, postBuild_target, BuildDefine.BuildOutputDir, "pdb", "pdb");
            rootNode.AppendChild(postBuild_target);
            
            void newCopyAction(XmlDocument document, XmlElement parent, string outputPath, string sourceExtension, string targetExtension)
            {
                var copyAction = document.CreateElement("Copy", doc.DocumentElement.NamespaceURI);
                copyAction.SetAttribute("SourceFiles", $"$(TargetDir)/$(TargetName).{sourceExtension}");
                copyAction.SetAttribute("DestinationFiles", $"$(ProjectDir)/{outputPath}/$(TargetName).{targetExtension}");
                copyAction.SetAttribute("ContinueOnError", "false");
                parent.AppendChild(copyAction);
            }

            using StringWriter stringWriter = new();
            using XmlTextWriter xmlTextWriter = new(stringWriter);
            xmlTextWriter.Formatting = Formatting.Indented;
            doc.WriteTo(xmlTextWriter);
            xmlTextWriter.Flush();
            return stringWriter.GetStringBuilder().ToString();
        }
    }
}