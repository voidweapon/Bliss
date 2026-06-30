using System.Text;
using System.Text.RegularExpressions;

namespace ProtoGenerator;

public class ProtoSyntaxParser
{
    private string? _syntax = null;
    private Stack<EMode> _modeStack = new Stack<EMode>();
    private ProtoGenContext _result;
    public ProtoGenContext Parser(string protoFilePath)
    {
        _result = new ProtoGenContext();
        _modeStack.Push(EMode.None);
        int line = 1;
        foreach (var lineContent in File.ReadLines(protoFilePath))
        {
            string trimLineContent = lineContent;
            var mode = _modeStack.Peek();
            if (mode != EMode.Comment)
            {
                trimLineContent = lineContent.Trim();
            }
            if (string.IsNullOrWhiteSpace(trimLineContent))
            {
                continue;
            }

            if (mode == EMode.None)
            {
                if (_syntax == null)
                {
                    ParserSyntax(trimLineContent, line);
                }
                else if (trimLineContent.StartsWith("//") || trimLineContent.StartsWith("/*"))
                {
                    ParserCommentStart(trimLineContent, line);
                }
                else if (trimLineContent.StartsWith(Symbol_Namespace))
                {
                    ParserNamespace(trimLineContent, line);
                }
                else if (trimLineContent.StartsWith(Symbol_Class))
                {
                    ParserClassMetaStart(trimLineContent, line);
                }
            }
            else if (_modeStack.Peek() == EMode.Comment)
            {
                ParserCommentEnd(trimLineContent, line);
            }
            else if (_modeStack.Peek() == EMode.ClassMeta)
            {
                ParserClassMetaEnd(trimLineContent, line);
            }
            else if (_modeStack.Peek() == EMode.Class)
            {
                ParserClassProperty(trimLineContent, line);
            }

            
            line++;
        }
        
        return _result;
    }

    #region Syntax Parsing

    private const string Symbol_Syntax = "syntax";
    private const string Symbol_End = ";";
    private void ParserSyntax(string lineContent, int lineNumber)
    {
        _modeStack.Push(EMode.Syntax);
        if (!lineContent.StartsWith(Symbol_Syntax, StringComparison.OrdinalIgnoreCase))
        {
            throw new Exception($"Not Found Syntax: {lineContent}: {lineNumber}");
        }
        if (!lineContent.EndsWith(Symbol_End))
        {
            throw new Exception($"Invalid Proto Syntax: {lineNumber}");
        }

        int startStr = lineContent.IndexOf('"', Symbol_Syntax.Length);
        int endStr = lineContent.LastIndexOf('"');

        if (startStr == -1 || endStr == -1 || startStr == endStr)
        {
            throw new Exception($"Invalid Proto Syntax: {lineContent}");
        }
        _syntax = lineContent.Substring(startStr, endStr - startStr);
        _modeStack.Pop();
    }

    #endregion
    
    private const string Symbol_Namespace = "package";
    private Regex _namespaceRegex =  new Regex("\\b\\w+\\b");

    private void ParserNamespace(string lineContent, int lineNumber)
    {
        if (!string.IsNullOrEmpty(_result.namesapce))
        {
            ReportLineError(lineContent, lineNumber);
        }

       var matches = _namespaceRegex.Matches(lineContent);
       if (matches.Count != 2)
       {
            ReportLineError(lineContent, lineNumber);
       }
        string namespaceName = matches[1].Value;
        _result.namesapce = namespaceName;
    }

    #region Comment Parsing

    private readonly StringBuilder _commentBuilder = new StringBuilder();
    private void ParserCommentStart(string lineContent, int lineNumber)
    {
        _modeStack.Push(EMode.Comment);
        _commentBuilder.Clear();
        
        
        if (lineContent.StartsWith("//"))
        {
            _commentBuilder.Append(lineContent);
            _modeStack.Pop();
        }
        else
        {
            _commentBuilder.AppendLine(lineContent);
        }
    }
    
    private void ParserCommentEnd(string lineContent, int lineNumber)
    {
        _commentBuilder.AppendLine(lineContent);
        if (lineContent.EndsWith("*/"))
        {
            _modeStack.Pop();
        }
    }

    #endregion

    #region Class Parsing

    private const string Symbol_Class = "message";
    private const string Symbol_Response = "response";
    private const string Symbol_BegainBraces = "{";
    private const string Symbol_EndBraces = "}";
    private const string Symbol_Colon = ":";
    private const string Symbol_Comma = ",";
    private ProtoClassInfo _classBuilder;
    private StringBuilder _classMetaBuilder = new StringBuilder();
    private Regex _classMetaRegex =  new Regex("\\b\\w+(\\.\\w+)*\\b|:|,");

    private void ParserClassMetaStart(string lineContent, int lineNumber)
    {
        _classBuilder = new ProtoClassInfo();
        if (_commentBuilder.Length > 0)
        {
            _classBuilder.comment = _commentBuilder.ToString();
            _commentBuilder.Clear();
        }
        _modeStack.Push(EMode.ClassMeta);
        _classMetaBuilder.Clear();
        _classMetaBuilder.AppendLine(lineContent);
    }

    private void ParserClassMetaEnd(string lineContent, int lineNumber)
    {
        if (lineContent.StartsWith(Symbol_BegainBraces))
        {
            //End ClassMeta Mode
            _modeStack.Pop();
            
            var matches = _classMetaRegex.Matches(_classMetaBuilder.ToString());
            if (matches.Count < 0)
            {
                throw new Exception($"Invalid Proto ClassMeta: {_classMetaBuilder}:[{lineNumber }-{lineNumber}]");
            }

            EClassMetaMode metaMode = EClassMetaMode.ClassKeyWorld;
            bool enterResponseSubMode = false;
            bool enterParentsSubMode = false;
            bool parentItemEnd = false;
            for (int i = 0; i < matches.Count; i++)
            {
                var world = matches[i].Value;
                if (metaMode == EClassMetaMode.ClassKeyWorld)
                {
                    if (!world.Equals(Symbol_Class, StringComparison.InvariantCultureIgnoreCase))
                    {
                        throw new Exception($"Invalid Proto ClassMeta: {_classMetaBuilder}:{lineNumber }"); 
                    }
                    metaMode =  EClassMetaMode.ClassName;
                }
                else if (metaMode == EClassMetaMode.ClassName)
                {
                    if (world.Contains(':') || world.Contains(','))
                    {
                        throw new Exception($"Invalid Proto ClassMeta: {_classMetaBuilder}:{lineNumber  + i}"); 
                    }
                    _classBuilder.class_name = world;
                    metaMode = EClassMetaMode.Parents;
                    enterParentsSubMode = false;
                    parentItemEnd = false;
                }
                else if (metaMode == EClassMetaMode.Parents)
                {
                    if (!enterParentsSubMode)
                    {
                        if (!world.Equals(Symbol_Colon))
                        {
                            throw new Exception($"Invalid Proto ClassMeta: {_classMetaBuilder}:{lineNumber  + i}"); 
                        }
                        enterParentsSubMode = true;
                    }
                    else
                    {
                        if (!parentItemEnd)
                        {
                            if (world.Equals(Symbol_Comma))
                            {
                                throw new Exception($"Invalid Proto ClassMeta: {_classMetaBuilder}:{lineNumber  + i}"); 
                            }
                            _classBuilder.parents.Add(world);
                            parentItemEnd = true;
                        }
                        else
                        {
                            if (world.Equals(Symbol_Response))
                            {
                                metaMode = EClassMetaMode.Response;
                                enterResponseSubMode = false;
                                continue;
                            }
                            
                            if (!world.Equals(Symbol_Comma))
                            {
                                throw new Exception($"Invalid Proto ClassMeta: {_classMetaBuilder}:{lineNumber  + i}"); 
                            }

                            parentItemEnd = false;
                        }
                    }
                }
                else if (metaMode == EClassMetaMode.Response)
                {
                    if (!enterResponseSubMode)
                    {
                        if (!world.Equals(Symbol_Colon))
                        {
                            throw new Exception($"Invalid Proto ClassMeta: {_classMetaBuilder}:{lineNumber  + i}"); 
                        }
                        enterResponseSubMode = true;
                    }
                    else
                    {
                        if (world.Contains(':') || world.Contains(','))
                        {
                            throw new Exception($"Invalid Proto ClassMeta: {_classMetaBuilder}:{lineNumber  + i}"); 
                        }
                        _classBuilder.response_class_name = world;   
                    }
                }
            }
            
            _modeStack.Push(EMode.Class);
            return;
        }
        
        _classMetaBuilder.AppendLine(lineContent);
    }

    private const string Symbol_Option = "option";
    private Regex _propertyRegex =  new Regex("\\b[\\w.]+\\b|=");
    private ProtoPropertyInfo _protoPropertyBuilder;

    private Dictionary<string, ESupportedCollection> _supportCollection = new ()
    {
        {"repeated", ESupportedCollection.List},
        {"map", ESupportedCollection.Dictionary},
    };
    private void ParserClassProperty(string lineContent, int lineNumber)
    {
        if (lineContent.StartsWith("//") || lineContent.StartsWith("/*"))
        {
            ParserCommentStart(lineContent, lineNumber);
            return;
        }

        if (lineContent.StartsWith(Symbol_EndBraces))
        {
            //End Class Mode
            _modeStack.Pop();
            _result.proto_items.Add(_classBuilder);
            return;
        }
        
        if (!lineContent.EndsWith(';'))
        {
            ReportLineError(lineContent, lineNumber);
        }
        
        var matches = _propertyRegex.Matches(lineContent);
        if (matches.Count < 0)
        {
            ReportLineError(lineContent, lineNumber);
        }

        _protoPropertyBuilder = new ProtoPropertyInfo();
        if (_commentBuilder.Length > 0)
        {
            _protoPropertyBuilder.comment = _commentBuilder.ToString();
            _commentBuilder.Clear();
        }
        
        EPropertyMode propertyMode = EPropertyMode.None;
        ESupportedCollection collectionMode = ESupportedCollection.None;
        bool enterOrderMode = false;
        for (int i = 0; i < matches.Count; i++)
        {
            var world = matches[i].Value;
            
            if (world.Equals(Symbol_Option))
            {
                if (_protoPropertyBuilder.optional)
                {
                    ReportLineError(lineContent, lineNumber);
                }
                _protoPropertyBuilder.optional = true;
            }
            else if (_supportCollection.TryGetValue(world, out var mode))
            {
                if (collectionMode != ESupportedCollection.None)
                {
                    ReportLineError(lineContent, lineNumber);
                }

                collectionMode = mode;
            }
            else if (propertyMode <= EPropertyMode.Type)
            {
                if (!string.IsNullOrWhiteSpace(_protoPropertyBuilder.property_type))
                {
                    ReportLineError(lineContent, lineNumber);
                }
                _protoPropertyBuilder.property_type = ConvertType(world, ref collectionMode);
                propertyMode = EPropertyMode.Name;
            }
            else if (propertyMode <= EPropertyMode.Name)
            {
                if (!string.IsNullOrWhiteSpace(_protoPropertyBuilder.name))
                {
                    ReportLineError(lineContent, lineNumber);
                }
                
                _protoPropertyBuilder.name =  world;
                propertyMode =  EPropertyMode.Order;
                enterOrderMode = false;
            }
            else if (propertyMode == EPropertyMode.Order)
            {
                if (!enterOrderMode)
                {
                    if (!world.Equals("="))
                    {
                        ReportLineError(lineContent, lineNumber);
                    }

                    enterOrderMode = true;
                }
                else
                {
                    if (int.TryParse(world, out var order))
                    {
                        _protoPropertyBuilder.order = order;

                        if (i != matches.Count - 1)
                        {
                            ReportLineError(lineContent, lineNumber);
                        }
                    }
                    else
                    {
                        ReportLineError(lineContent, lineNumber);
                    }
                }
            }
        }

        if (collectionMode != ESupportedCollection.None)
        {
            _protoPropertyBuilder.CollectionMode = collectionMode;
            _protoPropertyBuilder.property_type = ConvertCollectionType(collectionMode, _protoPropertyBuilder.property_type);
        }
        
        _classBuilder.properties.Add(_protoPropertyBuilder);
    }

    #endregion

    private static string ConvertType(string type, ref ESupportedCollection collectionType)
    {
        if (type == "bytes")
        {
            collectionType =  ESupportedCollection.Array;
        }
        
        return type switch
        {
            "int16" => "short",
            "int32" => "int",
            "bytes" => "byte[]",
            "uint32" => "uint",
            "long" => "long",
            "int64" => "long",
            "uint64" => "ulong",
            "uint16" => "ushort",
            _ => type
        };
    }

    private static string ConvertCollectionType(ESupportedCollection collection, string type)
    {
        return collection switch
        {
            ESupportedCollection.List => $"List<{type}>",
            _ => type
        };
    }
    
    private void ReportLineError(string lineContent, int lineNumber)
    {
        throw new Exception($"Invalid Proto ClassMeta: {lineContent}:{lineNumber}");
    }
    
    private enum EMode
    {
        None,
        Syntax,
        Namespace,
        Comment,
        ClassMeta,
        Class,
        Property,
        Response,
    }
    
    private enum EClassMetaMode
    {
        ClassKeyWorld,
        ClassName,
        Parents,
        Response,
    }
    

    
    private enum EPropertyMode
    {
        None,
        Option,
        Collection,
        Type,
        Name,
        Order,
    }
}

public enum ESupportedCollection
{
    None,
    Array,
    List,
    Dictionary,
}