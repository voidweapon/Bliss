using MemoryPack;

namespace Bliss;

internal static class InternalMessageEnvelopeOpcodes
{
    internal const ushort SEND = 65002;
    internal const ushort CALL = 65000;
    internal const ushort CALL_RESPONSE = 65001;
}

internal static class InternalMessageEnvelopeSizeHints
{
    internal const int SEND_ENVELOPE_BASE_SIZE = 64;
    internal const int CALL_ENVELOPE_BASE_SIZE = 80;
    internal const int CALL_RESPONSE_ENVELOPE_BASE_SIZE = 64;
}

/// <summary>Routes a one-way business message through the Main process gateway.</summary>
/// <remarks>
/// Main processes exchanging these envelopes must use compatible builds; mixed protocol versions are unsupported.
/// </remarks>
[MemoryPackable]
[MessageOpcode(InternalMessageEnvelopeOpcodes.SEND)]
public partial class InternalMessageSendEnvelope : MessageObject, IMessage
{
    /// <summary>The Root that originated the business message.</summary>
    public RootAddress OriginRoot { get; set; }
    /// <summary>The final business Actor address.</summary>
    public ActorAddress TargetAddress { get; set; }
    /// <summary>The opcode of the serialized business payload.</summary>
    public ushort PayloadOpcode { get; set; }
    /// <summary>The serialized business message body.</summary>
    public byte[] Payload { get; set; }

    /// <inheritdoc />
    public override int SizeHint() => InternalMessageEnvelopeSizeHints.SEND_ENVELOPE_BASE_SIZE
        + (this.Payload?.Length ?? 0);
}

/// <summary>Routes a business RPC through the Main process gateway.</summary>
[MemoryPackable]
[MessageOpcode(InternalMessageEnvelopeOpcodes.CALL)]
[MessageResponse("Bliss.InternalMessageCallResponse")]
public partial class InternalMessageCallEnvelope : MessageObject, IRequest
{
    /// <summary>The Root that originated the business request.</summary>
    public RootAddress OriginRoot { get; set; }
    /// <summary>The original sender's correlation identifier, restored by its Main.</summary>
    public int OriginRpcId { get; set; }
    /// <summary>The final business Actor address.</summary>
    public ActorAddress TargetAddress { get; set; }
    /// <summary>The opcode of the serialized business request.</summary>
    public ushort PayloadOpcode { get; set; }
    /// <summary>The serialized business request body.</summary>
    public byte[] Payload { get; set; }
    /// <summary>The gateway RPC identifier for this hop.</summary>
    public int RpcId { get; set; }

    /// <inheritdoc />
    public override int SizeHint() => InternalMessageEnvelopeSizeHints.CALL_ENVELOPE_BASE_SIZE
        + (this.Payload?.Length ?? 0);
}

/// <summary>Returns a business RPC response through the Main process gateway.</summary>
[MemoryPackable]
[MessageOpcode(InternalMessageEnvelopeOpcodes.CALL_RESPONSE)]
public partial class InternalMessageCallResponse : MessageObject, IResponse
{
    /// <summary>The serialized business response opcode.</summary>
    public ushort PayloadOpcode { get; set; }
    /// <summary>The serialized business response body.</summary>
    public byte[] Payload { get; set; }
    /// <summary>The business response error code.</summary>
    public int Error { get; set; }
    /// <summary>The business response error message.</summary>
    public string Message { get; set; }
    /// <summary>The gateway RPC identifier for this hop.</summary>
    public int RpcId { get; set; }

    /// <inheritdoc />
    public override int SizeHint() => InternalMessageEnvelopeSizeHints.CALL_RESPONSE_ENVELOPE_BASE_SIZE
        + (this.Payload?.Length ?? 0);
}
