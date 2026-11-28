using System;
using System.IO;
using Bliss.Core;
using Bliss.Core.Network;
using Bliss.Pool;

namespace Bliss
{
    public static class MessageSerializer
    {
        public static byte[] Serialize(MessageObject message)
        {
            return Serializer.Serialize(message);
        }
        
        /// <summary>Serializes a message into a buffer with space reserved for its opcode.</summary>
        /// <param name="message">The message to serialize.</param>
        /// <param name="bufferRef">The destination buffer.</param>
        /// <exception cref="InvalidOperationException">The buffer contains data beyond its reserved prefix or serialization produces no content.</exception>
        public static void Serialize(MessageObject message, SocketBufferRef bufferRef)
        {
            serialize(message, bufferRef, sizeof(ushort));
        }

        private static void serialize(MessageObject message, SocketBufferRef bufferRef, int prefixLength)
        {
            SocketBuffer buffer = bufferRef.Target;
            long prevLength = buffer.Length;
            if (prevLength > prefixLength)
            {
                throw new InvalidOperationException("The buffer contains data beyond its reserved prefix.");
            }
            
            Serializer.Serialize(message, buffer);
            
            long afterLength = buffer.Length;
            int messageLength = (int)(afterLength - prevLength);
            if (messageLength == 0)
            {
                throw new InvalidOperationException("Serialization produced no content.");
            }
            
            buffer.Reserve(messageLength);
            buffer.Push(messageLength);
        }

        public static MessageObject Deserialize(Type type, byte[] bytes, int offset, int count)
        {
            object mergeObject = ObjectPool.Instance.Fetch(type);
            Serializer.Deserialize(type, bytes, offset, count, ref mergeObject);
            
            return mergeObject as MessageObject;
        }
        
        public static MessageObject Deserialize(Type type, SocketBufferRef bufferRef)
        {
            object mergeObject = ObjectPool.Instance.Fetch(type);
            SocketBuffer buffer = bufferRef.Target;
            Serializer.Deserialize(type, buffer.Pull(buffer.TrueLength), ref mergeObject);
            
            return mergeObject as MessageObject;
        }
        
        public static MessageObject Deserialize(Type type, ReadOnlySpan<byte> buffer)
        {
            object mergeObject = ObjectPool.Instance.Fetch(type);
            Serializer.Deserialize(type, buffer, ref mergeObject);
            
            return mergeObject as MessageObject;
        }

        /// <summary>Serializes a payload body and returns its registered message opcode.</summary>
        /// <param name="message">The business message to serialize.</param>
        /// <param name="opcode">The registered opcode for the message type.</param>
        /// <returns>The serialized message body without a network header.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="message"/> is null.</exception>
        /// <exception cref="InvalidOperationException">The message type has no registered opcode.</exception>
        public static byte[] SerializePayload(MessageObject message, out ushort opcode)
        {
            if (message is null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            if (!MessageOpcodeManager.Instance.TryGetOpcodeTypeBy(message.GetType(), out opcode))
            {
                throw new InvalidOperationException($"{message.GetType()} not found Opcode.");
            }

            return Serialize(message);
        }

        /// <summary>Deserializes a payload body using its registered message opcode.</summary>
        /// <param name="opcode">The message opcode.</param>
        /// <param name="payload">The serialized message body.</param>
        /// <returns>The deserialized message, owned by the caller.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="payload"/> is null.</exception>
        /// <exception cref="InvalidOperationException">The opcode has no registered message type.</exception>
        public static MessageObject DeserializePayload(ushort opcode, byte[] payload)
        {
            if (payload is null)
            {
                throw new ArgumentNullException(nameof(payload));
            }

            if (!MessageOpcodeManager.Instance.GetMessageTypeBy(opcode, out Type messageType))
            {
                throw new InvalidOperationException($"Opcode {opcode} not found message type.");
            }

            return Deserialize(messageType, payload, 0, payload.Length);
        }

        /// <summary>Serializes a message into a network stream.</summary>
        /// <param name="netDriver">The driver that owns the output buffer.</param>
        /// <param name="message">The message to serialize.</param>
        /// <returns>A network buffer containing the opcode and message body.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="netDriver"/> or <paramref name="message"/> is null.</exception>
        /// <exception cref="InvalidOperationException">The message has no registered opcode or cannot be serialized into the buffer.</exception>
        public static SocketBufferRef MessageToNetStream(ANetDriver netDriver, MessageObject message)
        {
            return messageToNetStream(netDriver, message, default, false);
        }

        /// <summary>Serializes an internal message with its routing address.</summary>
        /// <param name="netDriver">The driver that owns the output buffer.</param>
        /// <param name="address">The destination Actor address to prepend.</param>
        /// <param name="message">The message to serialize.</param>
        /// <returns>A network buffer containing the address, opcode, and message body.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="netDriver"/> or <paramref name="message"/> is null.</exception>
        /// <exception cref="InvalidOperationException">The message has no registered opcode or cannot be serialized into the buffer.</exception>
        public static SocketBufferRef MessageToNetStream(ANetDriver netDriver, ActorAddress address, MessageObject message)
        {
            return messageToNetStream(netDriver, message, address, true);
        }

        private static SocketBufferRef messageToNetStream(ANetDriver netDriver, MessageObject message,
            ActorAddress address, bool includeAddress)
        {
            if (netDriver is null)
            {
                throw new ArgumentNullException(nameof(netDriver));
            }

            if (message is null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            if (!MessageOpcodeManager.Instance.TryGetOpcodeTypeBy(message.GetType(), out var opcodeType))
            {
                throw new InvalidOperationException($"{message.GetType()} not found Opcode.");
            }
            
            int opcodeSize = sizeof(ushort);
            int addressSize = includeAddress ? sizeof(int) + sizeof(int) + sizeof(long) : 0;
            int prefixSize = opcodeSize + addressSize;
            SocketBufferRef bufferRef = netDriver.RentBuffer(message.SizeHint() + prefixSize);
            SocketBuffer buffer = bufferRef.Target;

            
            buffer.Reserve(prefixSize);
            buffer.Advance(prefixSize);
            
            serialize(message, bufferRef, prefixSize);
            
            buffer.Push(opcodeSize)
                .WriteUShort(opcodeType);

            if (includeAddress)
            {
                buffer.Push(addressSize).WriteIntTo(address.Process).WriteIntTo(address.Root).WriteLong(address.InstanceId);
            }
            
            return bufferRef;
        }

        /// <summary>Reads the routing address preceding an internal network message.</summary>
        /// <param name="bufferRef">The buffer positioned at an internal message's address header.</param>
        /// <returns>The decoded destination Actor address.</returns>
        /// <exception cref="ArgumentException"><paramref name="bufferRef"/> does not contain a valid buffer.</exception>
        public static ActorAddress ReadInternalAddress(SocketBufferRef bufferRef)
        {
            SocketBuffer buffer = bufferRef.Target;
            if (buffer is null)
            {
                throw new ArgumentException("The buffer reference is not initialized.", nameof(bufferRef));
            }

            ReadOnlySpan<byte> address = buffer.Pull(sizeof(int) + sizeof(int) + sizeof(long));
            address.ReadInt(out int process).ReadInt(out int root).ReadLong(out long instanceId);
            return new ActorAddress(process, root, instanceId);
        }

        public static (ushort, MessageObject) NetStreamToMessage(SocketBufferRef bufferRef)
        {
            int msgLength = bufferRef.Target.TrueLength;
            int dataPosition = bufferRef.Target.DataPosition;
            int tailPosition = bufferRef.Target.TailPosition;

            SocketBuffer buffer = bufferRef.Target;
            var msgSpan = buffer.Pull(buffer.TrueLength)
                .ReadUShort(out ushort opcodeType);
            
            if (!MessageOpcodeManager.Instance.GetMessageTypeBy(opcodeType, out Type messageType))
            {
                throw new Exception($"opcode {opcodeType} not found messageType");
            }

            try
            {
                MessageObject  messageObject = Deserialize(messageType, msgSpan);
                return (opcodeType, messageObject);
            }
            catch (Exception e)
            {
                throw new Exception($"message type {messageType} opcode[{opcodeType}] 解析失败 [{dataPosition},{tailPosition}] ", e);
            }
        }
    }
}