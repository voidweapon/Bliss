using Bliss.ProjectRule.Analyzer;
using Bliss.ProjectRule.SourceGenerator.Templates;
using Scriban;

namespace Bliss.ProjectRule.SourceGenerator.Generators;

internal class MemoryPackPolymorphismTemplate : ACodeTemplate
{
    private readonly string Templatename = Definition.TemplateMap.MemoryPackPolymorphism;
    private readonly string TemplateString = @"
#nullable enable
#pragma warning disable CS0108 // hides inherited member
#pragma warning disable CS0162 // Unreachable code
#pragma warning disable CS0164 // This label has not been referenced
#pragma warning disable CS0219 // Variable assigned but never used
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8601 // Possible null reference assignment
#pragma warning disable CS8602
#pragma warning disable CS8604 // Possible null reference argument for parameter
#pragma warning disable CS8619
#pragma warning disable CS8620
#pragma warning disable CS8631 // The type cannot be used as type parameter in the generic type or method
#pragma warning disable CS8765 // Nullability of type of parameter
#pragma warning disable CS9074 // The 'scoped' modifier of parameter doesn't match overridden or implemented member
#pragma warning disable CA1050 // Declare types in namespaces.

using System;
using MemoryPack;

{{~namespace_with_grace_begin __namespace__ ~}}
    [global::MemoryPack.Internal.Preserve]
    public class {{gen_class_name __class_name__}}_MemoryPackSerializeFormatter : MemoryPackFormatter<global::{{__class_name__}}>
    {
        static readonly System.Collections.Generic.Dictionary<Type, long> __typeToTag = new()
        {
            {{~for subclass_name in __class_name__list__~}}
            { typeof(global::{{subclass_name}}), {{gen_str_hashcode subclass_name}} },
            {{~end~}}
        };

        [global::MemoryPack.Internal.Preserve]
        {{~ if __is_dotnet__ ~}}
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref global::{{__class_name__}}? value)
        {{~else~}}
        public override void Serialize(ref MemoryPackWrite writer, ref global::{{__class_name__}}? value)
        {{~end~}}
        {
            if (value == null)
            {
                writer.WriteNullUnionHeader();
                return;
            }

            if (__typeToTag.TryGetValue(value.GetType(), out long tag))
            {
                writer.WriteValue<byte>(global::MemoryPack.MemoryPackCode.WideTag);
                writer.WriteValue<long>(tag);
                switch (tag)
                {
                    {{~for subclass_name in __class_name__list__~}}
                    case {{gen_str_hashcode subclass_name}}:
                    {
                        writer.WritePackable(System.Runtime.CompilerServices.Unsafe.As<global::{{__class_name__}}?, global::{{subclass_name}}>(ref value)); 
                        break;
                    }
                    {{~end~}}
                    default:
                        //MemoryPackSerializationException.ThrowInvalidTag(tag, typeof(global::IForExternalUnion));
                        break;
                }
            }
            else
            {
                MemoryPackSerializationException.ThrowNotFoundInUnionType(value.GetType(), typeof(global::{{__class_name__}}));
            }
        }

        [global::MemoryPack.Internal.Preserve]
        {{~ if __is_dotnet__ ~}}
        public override void Deserialize(ref MemoryPackReader reader, scoped ref global::{{__class_name__}}? value)
        {{~else~}}
        public override void Deserialize(ref MemoryPackReader reader, ref global::{{__class_name__}}? value)
        {{~end~}}
        {
            bool isNull = reader.ReadValue<byte>() == global::MemoryPack.MemoryPackCode.NullObject;
            if (isNull)
            {
                value = default;
                return;
            }

            var tag = reader.ReadValue<long>();
            switch (tag)
            {
                {{~for subclass_name in __class_name__list__~}}
                case {{gen_str_hashcode subclass_name}}:
                {
                    if(value is global::{{subclass_name}})
                    {
                        reader.ReadPackable(ref System.Runtime.CompilerServices.Unsafe.As<global::{{__class_name__}}?, global::{{subclass_name}}>(ref value));
                    }
                    else
                    {
                        value = (global::{{subclass_name}})reader.ReadPackable<global::{{subclass_name}}>();
                    }
                    break;
                }
                {{~end~}}
                default:
                    //MemoryPackSerializationException.ThrowInvalidTag(tag, typeof(global::IForExternalUnion));
                    break;
            }
        }
    }
{{~ namespace_with_grace_end __namespace__ ~}}
";
    
    public override (string, Template) Get()
    {
        var template = Template.Parse(this.TemplateString);
        return (Templatename, template);
    }
}

internal class MemoryPackPolymorphismRegisterTemplate : ACodeTemplate
{
    private readonly string Templatename = Definition.TemplateMap.MemoryPackPolymorphismRegister;

    private readonly string TemplateString = @"
{{namespace_with_grace_begin __namespace__}}
    public static partial class {{__class_name__}}
    {
        static partial void Register()
        {
            {{~for base_class_name in __class_name__list__~}}
            if (!global::MemoryPack.MemoryPackFormatterProvider.IsRegistered<global::{{base_class_name}}>())
            {
                global::MemoryPack.MemoryPackFormatterProvider.Register(new {{gen_class_name base_class_name}}_MemoryPackSerializeFormatter());
            }
            {{~end~}}
        }
    }
{{namespace_with_grace_end __namespace__}}
";
    public override (string, Template) Get()
    {
        var template = Template.Parse(this.TemplateString);
        return (Templatename, template);
    }
}