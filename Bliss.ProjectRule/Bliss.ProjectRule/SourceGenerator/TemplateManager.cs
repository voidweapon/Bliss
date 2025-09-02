using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Bliss.ProjectRule.SourceGenerator.Templates;
using Scriban;

namespace Bliss.ProjectRule.SourceGenerator;

internal class TemplateManager
{
    public static readonly string TemplateFolderPath = "Templates~";

    public static TemplateManager Instance { get; } = new(); 

    private readonly List<string> _templateSearchPaths = new();
    
    private readonly ConcurrentDictionary<string, string> _templateStrings = new();
    private readonly ConcurrentDictionary<string, Template> _templates = new();

    public TemplateManager()
    {
        var entitySystem = new EntitySystemTemplate().Get();
        _templates.TryAdd(entitySystem.Item1, entitySystem.Item2);
    }
    
    private void EnsureInit()
    {
        // if (_templateSearchPaths.Count == 0)
        // {
        //     string curDir = Path.GetDirectoryName(AppContext.BaseDirectory);
        //     AddTemplateSearchPath($"{curDir}/{TemplateFolderPath}");
        // }
    }
    
    public void AddTemplateSearchPath(string templateSearchPath, bool ensureExitPath = false)
    {
        if (_templateSearchPaths.Contains(templateSearchPath))
        {
            return;
        }
        
        if (!Directory.Exists(templateSearchPath))
        {
            if (ensureExitPath)
            {
                throw new Exception($"Template search path '{templateSearchPath}' does not exist.");
            }
            return;
        }
        
        _templateSearchPaths.Add(templateSearchPath);
    }
    
    public bool TryGetTemplateString(string templateName, out string result)
    {
        EnsureInit();
        
        if (_templateStrings.TryGetValue(templateName, out result))
        {
            return true;
        }
        foreach (var searchPath in _templateSearchPaths)
        {
            var fullPath = $"{searchPath}/{templateName}.sbn";
            if (File.Exists(fullPath))
            {
                result = File.ReadAllText(fullPath, Encoding.UTF8);
                _templateStrings[templateName] = result;
                return true;
            }
        }
        result = null;
        return false;
    }

    public string GetTemplateString(string templateName)
    {
        if (TryGetTemplateString(templateName, out var strTpl))
        {
            return strTpl;
        }
        throw new FileNotFoundException($"can't find {templateName}.sbn in paths:{string.Join(";", _templateSearchPaths)}");
    }

    public void AddTemplate(string templateName, Template template)
    {
        _templates.TryAdd(templateName, template);
    }
    
    public bool TryGetTemplate(string templateName, out Template template)
    {
        if (_templates.TryGetValue(templateName, out template))
        {
            return true;
        }
        if (TryGetTemplateString(templateName, out var strTpm))
        {
            template = _templates.GetOrAdd(templateName, tn => Template.Parse(strTpm));
            return true;
        }
        template = null;
        return false;
    }

    public string DebugSearch()
    {
        return $"SearchPath Count={_templateSearchPaths.Count}  {string.Join(";", _templateSearchPaths)}";
    }
}