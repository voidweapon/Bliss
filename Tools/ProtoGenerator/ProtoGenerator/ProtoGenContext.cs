namespace ProtoGenerator;

public class ProtoPropertyInfo
{
    public string comment { get; set; }
    public bool optional { get; set; }
    public string property_type { get; set; }
    public string name { get; set; }
    public int order { get; set; }
}


public class ProtoClassInfo
{
    public int opcode { get; set; }
    public string comment { get; set; }
    public string class_name { get; set; }
    public List<string> parents { get; set; } = new List<string>();
    public string response_class_name  { get; set; }

    public bool use_custom_dispose { get; set; }
    public List<ProtoPropertyInfo> properties { get; set; } = new List<ProtoPropertyInfo>();
}

public class ProtoGenContext
{
    public List<string> includes { get; set; } = new List<string>();
    public string namesapce { get; set; }
    public List<ProtoClassInfo> proto_items { get; set; } = new List<ProtoClassInfo>();
}