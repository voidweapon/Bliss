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
        
        public static void Serialize(MessageObject message, SocketBufferRef bufferRef)
        {
            SocketBuffer buffer = bufferRef.Target;
            long prevLength = buffer.Length;
            if (prevLength > 2)
            {
                throw new Exception("Serialize error!!!");
            }
            
            Serializer.Serialize(message, buffer);
            
            long afterLength = buffer.Length;
            int messageLength = (int)(afterLength - prevLength);
            if (messageLength == 0)
            {
                throw new Exception("Serialize error");
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

        public static SocketBufferRef MessageToNetStream(ANetDriver netDriver, MessageObject message)
        {
            if (!MessageOpcodeManager.Instance.TryGetOpcodeTypeBy(message.GetType(), out var opcodeType))
            {
                throw new Exception($"{message.GetType()} not found Opcode");
            }
            
            int opcodeSize = sizeof(ushort);
            SocketBufferRef bufferRef = netDriver.RentBuffer(message.SizeHint() + opcodeSize);
            SocketBuffer buffer = bufferRef.Target;

            
            buffer.Reserve(opcodeSize);
            buffer.Advance(opcodeSize);
            
            Serialize(message, bufferRef);
            
            buffer.Push(opcodeSize)
                .WriteUShort(opcodeType);
            
            return bufferRef;
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