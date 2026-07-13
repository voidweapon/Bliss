using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Bliss.CharacterMovement;
using MemoryPack;
using MemoryPack.Compression;
using MemoryPack.Internal;
using Unity.Mathematics;

namespace Bliss
{
    [MemoryPackable]
    public partial class CharacterNetworkMoveData
    {
        // Indicates whether this was the latest new move, a pending/dual move, or old important move.
        public ENetworkMoveType NetworkMoveType;

        //------------------------------------------------------------------------
        // Basic movement data.

        public float TimeStamp;
        public Vector_NetQuantize10 Acceleration = new Vector_NetQuantize10();
        public Vector_NetQuantize100 Location = new Vector_NetQuantize100();		// Either world location or relative to MovementBase if that is set.
        //public float3 ControlRotation;
        public float3 Rotation;
        public byte CompressedMoveFlags;

        byte MovementMode;
        public long MovementBaseId;
        public string MovementBaseBoneName;
        
        public enum ENetworkMoveType
        {
            NewMove,
            PendingMove,
            OldMove
        };

        public void ClientFillNetworkMoveData(SavedMove_Character moveData, ENetworkMoveType  networkMoveType)
        {
            NetworkMoveType = networkMoveType;
            TimeStamp = moveData.TimeStamp;
            //ControlRotation = moveData.EndRotation;
            Rotation =  math.Euler(moveData.EndRotation);
            CompressedMoveFlags = moveData.EndPackedMovementMode;
            MovementMode = moveData.GetCompressedFlags();
            
            Acceleration.Value =  moveData.Acceleration;
            Location.Value =  moveData.EndLocation;

            MovementBaseId = 0;
            MovementBaseBoneName = null;
        }

        public void Clear()
        {
            NetworkMoveType =  ENetworkMoveType.NewMove;
            TimeStamp = 0;
            Acceleration.Value = float3.zero;
            Location.Value = float3.zero;
            Rotation =  float3.zero;
            CompressedMoveFlags = 0;
            MovementMode = 0;
            MovementBaseId = 0;
            MovementBaseBoneName = null;
        }
    }
    
    [MemoryPackable]
    public partial class CharacterNetworkMoveDataContainer
    {
        [MemoryPackIgnore] public CharacterNetworkMoveData NewMoveData => MoveData[0];
        [MemoryPackIgnore] public CharacterNetworkMoveData PendingMoveData => MoveData[1];
        [MemoryPackIgnore] public CharacterNetworkMoveData OldMoveData => MoveData[2];

        [MemoryPackInclude] public CharacterNetworkMoveData[] MoveData;
        [MemoryPackInclude] public bool bHasPendingMove = false;
        [MemoryPackInclude] public bool bHasOldMove = false;
        [MemoryPackInclude] public bool bDisableCombinedScopedMove = false;
        [MemoryPackInclude] public bool bIsDualHybridRootMotionMove = false;

        public CharacterNetworkMoveDataContainer()
        {
            MoveData =  new CharacterNetworkMoveData[3];
            MoveData[0] = new CharacterNetworkMoveData();
            MoveData[1] = new CharacterNetworkMoveData();
            MoveData[2] = new CharacterNetworkMoveData();
        }

        public void ClientFillNetworkMoveData(SavedMove_Character newMoveData, SavedMove_Character pendingMoveData, SavedMove_Character  oldMoveData)
        {
            if (newMoveData == null)
            {
                throw new ArgumentNullException(nameof(newMoveData));
            }
            NewMoveData.ClientFillNetworkMoveData(newMoveData, CharacterNetworkMoveData.ENetworkMoveType.NewMove);
            bDisableCombinedScopedMove |= newMoveData.bForceNoCombine;

            bHasPendingMove = pendingMoveData != null;
            if (bHasPendingMove)
            {
                PendingMoveData.ClientFillNetworkMoveData(pendingMoveData, CharacterNetworkMoveData.ENetworkMoveType.PendingMove); 
                bDisableCombinedScopedMove |= pendingMoveData.bForceNoCombine;
            }
            else
            {
                bIsDualHybridRootMotionMove = false;
            }
            
            bHasOldMove = oldMoveData != null;
            if (bHasOldMove)
            {
                OldMoveData.ClientFillNetworkMoveData(oldMoveData, CharacterNetworkMoveData.ENetworkMoveType.OldMove);
            }
        }

        public void Clear()
        {
            bHasPendingMove =  false;
            bHasOldMove =  false;
            bDisableCombinedScopedMove = false;
            bIsDualHybridRootMotionMove = false;
            for (int i = 0; i < MoveData.Length; i++)
            {
                MoveData[i].Clear();
            }
        }
    }

    [MemoryPackable]
    public partial class CharacterRepMovement
    {
        public float3 LinearVelocity;
        public float3 AngularVelocity;
        public float3 Location;
        public float3 Rotation;
    }

    [MemoryPackable]
    public partial class Vector_NetQuantize10
    {
        [MemoryPackInclude]
        public float3 Value;
    }
    [MemoryPackable]
    public partial class Vector_NetQuantize100
    {
        [MemoryPackInclude]
        public float3 Value;
    }
    
#if Client
    [Preserve]
    public class Vector_NetQuantize10Formtter : MemoryPackFormatter<Vector_NetQuantize10>
    {
        public override void Serialize(ref MemoryPackWriter writer, ref Vector_NetQuantize10 value)
        {
            CharacterMovementReplicationExtensions.WriteQuantizedVector(10, ref value.Value, ref writer);
        }

        public override void Deserialize(ref MemoryPackReader reader, ref Vector_NetQuantize10 value)
        {
            value = new Vector_NetQuantize10();
            CharacterMovementReplicationExtensions.ReadQuantizedVector(10, ref value.Value, ref reader);
        }
    }
    
    [Preserve]
    public class Vector_NetQuantize100Formtter : MemoryPackFormatter<Vector_NetQuantize100>
    {
        public override void Serialize(ref MemoryPackWriter writer, ref Vector_NetQuantize100 value)
        {
            CharacterMovementReplicationExtensions.WriteQuantizedVector(100, ref value.Value, ref writer);

        }

        public override void Deserialize(ref MemoryPackReader reader, ref Vector_NetQuantize100 value)
        {
            CharacterMovementReplicationExtensions.ReadQuantizedVector(100, ref value.Value, ref reader);
        }
    }
#endif
    
#if DOTNET
    [Preserve]
    public class Vector_NetQuantize10Formtter : MemoryPackFormatter<Vector_NetQuantize10>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref Vector_NetQuantize10 value)
        {
            throw new NotImplementedException();
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref Vector_NetQuantize10 value)
        {
            throw new NotImplementedException();
        }
    }
    
    [Preserve]
    public class Vector_NetQuantize100Formtter : MemoryPackFormatter<Vector_NetQuantize10>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref Vector_NetQuantize10 value)
        {
            throw new NotImplementedException();
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref Vector_NetQuantize10 value)
        {
            throw new NotImplementedException();
        }
    }
#endif
    
    public static class CharacterMovementReplicationExtensions
    {
        const int ScalarTypeSize = sizeof(float);
        private static float MaxValueToScale;
        private static float MaxScaledValue;

        static CharacterMovementReplicationExtensions()
        {
            // Beyond 2^MaxExponentForScaling scaling cannot improve the precision as the next floating point value is at least 1.0 more. 
            int MaxExponentForScaling = ScalarTypeSize == 4 ? 23 : 52;
            MaxValueToScale = (float)(1 << MaxExponentForScaling);
            // Rounding of large values can introduce additional precision errors and the extra cost to serialize with full precision is small.
            int MaxExponentAfterScaling = ScalarTypeSize == 4 ? 30 : 62;
            MaxScaledValue = (float)(1 << MaxExponentAfterScaling);
        }
        
        public static bool ContainsNaN(this float3 value)
        {
            return  float.IsNaN(value.x) || float.IsNaN(value.y) || float.IsNaN(value.z);
        }
        
        public static float GetAbsMax(this float3 value)
        {
            return math.max(math.max(math.abs(value.x), math.abs(value.y)), math.abs(value.z));
        }

        public static float GetAbsMin(this float3 value)
        {
            return math.min(math.min(math.abs(value.x), math.abs(value.y)), math.abs(value.z));
        }
        
        public static int RoundFloatToInt(float value)
        {
            return (int)(value + math.sign(value) * 0.5f);
        }
        public static int RoundFloatToInt(double value)
        {
            return (int)(value + math.sign(value) * 0.5f);
        }


        
        #if DOTNET
        public static int CountLeadingZeros(this ulong x)
        {
            return System.Numerics.BitOperations.LeadingZeroCount(x);
        }
        #else
        public static uint CountLeadingZeros(this ulong x)
        {
            // Do the smearing which turns (for example)
            // this: 0000 0101 0011
            // into: 0000 0111 1111
            x |= x >> 1;
            x |= x >> 2;
            x |= x >> 4;
            x |= x >> 8;
            x |= x >> 16;
            x |= x >> 32;

            // Count the ones
            x -= x >> 1 & 0x5555555555555555;
            x = (x >> 2 & 0x3333333333333333) + (x & 0x3333333333333333);
            x = (x >> 4) + x & 0x0f0f0f0f0f0f0f0f;
            x += x >> 8;
            x += x >> 16;
            x += x >> 32;

            const int numLongBits = sizeof(long) * 8; // compile time constant
            return numLongBits - (uint)(x & 0x0000007f); // subtract # of 1s from 64
        }
        #endif
        

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetBitsNeeded(int value)
        {
            return GetBitsNeeded((long)value);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetBitsNeeded(long value)
        {
            ulong massagedValue = (ulong)(value ^ (value >> 63)); // convert to unsigned and flip the sign bit to the least significant bit
            return (int)(65U - CountLeadingZeros(massagedValue));
        }
        
#if Client
        public static void WriteQuantizedVector(int scale, ref float3 value, ref MemoryPackWriter writer)
#elif DOTNET
        public static void WriteQuantizedVector<TBufferWriter>(int scale, ref float3 value, ref MemoryPackWriter<TBufferWriter> writer) where TBufferWriter : System.Buffers.IBufferWriter<byte>
#endif

        {
            float3 scaledValue = value * scale;
            if (scaledValue.GetAbsMax() < MaxScaledValue)
            {
                bool bUseScaledValue = value.GetAbsMin() < MaxValueToScale;
                int x;
                int y;
                int z;

                if (bUseScaledValue)
                {
                    x = RoundFloatToInt(scaledValue.x);
                    y = RoundFloatToInt(scaledValue.y);
                    z = RoundFloatToInt(scaledValue.z);
                }
                else
                {
                    x = RoundFloatToInt(value.x);
                    y = RoundFloatToInt(value.y);
                    z = RoundFloatToInt(value.z);
                }
                
                //TODO:手动压缩x,y,z分量
                int componentBitCount = math.max(math.max(GetBitsNeeded(x), GetBitsNeeded(y)), GetBitsNeeded(z));
                int componentBitCountAndScaleInfo = (bUseScaledValue ? (1<< 6) : 0) | componentBitCount;
                writer.WriteVarInt(componentBitCountAndScaleInfo);
                writer.WriteVarInt(x);
                writer.WriteVarInt(y);
                writer.WriteVarInt(z);
            }
            else
            {
                int componentBitCount = 0;
                int componentBitCountAndScaleInfo = (ScalarTypeSize == 8 ? (1<< 6) : 0) | componentBitCount;
                writer.WriteVarInt(componentBitCountAndScaleInfo);
                writer.WriteValue(value.x);
                writer.WriteValue(value.y);
                writer.WriteValue(value.z);
            }
        }

        public static void ReadQuantizedVector(int scale, ref float3 value, ref MemoryPackReader reader)
        {
            int componentBitCountAndScaleInfo = reader.ReadVarIntInt32();
            int componentBitCount = componentBitCountAndScaleInfo & 63;
            int extraInfo = componentBitCountAndScaleInfo >> 6;

            if (componentBitCount > 0)
            {
                //TODO:手动压缩x,y,z分量
                int x = reader.ReadVarIntInt32();
                int y = reader.ReadVarIntInt32();
                int z = reader.ReadVarIntInt32();

                float3 tempValue = new float3(x, y, z);
                if (extraInfo > 0)
                {
                    value = tempValue / scale;
                }
                else
                {
                    value = tempValue;
                }
            }
            else
            {
                value = new float3(reader.ReadValue<float>(), reader.ReadValue<float>(), reader.ReadValue<float>());
            }
        }


        public static C2M_CharacterMoveData ToMessage(this CharacterNetworkMoveDataContainer self, long actorId, bool isFromPool = false)
        {
            var msg = C2M_CharacterMoveData.Create(isFromPool);
            //msg.ActorId = actorId;
            msg.bDisableCombinedScopedMove = self.bDisableCombinedScopedMove;
            msg.bIsDualHybridRootMotionMove =  self.bIsDualHybridRootMotionMove;
            msg.MoveData.Add(self.NewMoveData);
            if (self.bHasPendingMove)
            {
                msg.MoveData.Add(self.PendingMoveData);
            }

            if (self.bHasOldMove)
            {
                msg.MoveData.Add(self.OldMoveData);
            }
            return msg;
        }

        public static void Serialize(this CharacterNetworkMoveDataContainer self, C2M_CharacterMoveData msg)
        {
            self.Clear();
            for (int i = 0; i < msg.MoveData.Count; i++)
            {
                if( msg.MoveData[i].NetworkMoveType == CharacterNetworkMoveData.ENetworkMoveType.NewMove)
                {
                    self.MoveData[0] = msg.MoveData[i];
                }
                else if( msg.MoveData[i].NetworkMoveType == CharacterNetworkMoveData.ENetworkMoveType.PendingMove)
                {
                    self.MoveData[1] = msg.MoveData[i];
                    self.bHasPendingMove = true;
                }
                else if( msg.MoveData[i].NetworkMoveType == CharacterNetworkMoveData.ENetworkMoveType.OldMove)
                {
                    self.MoveData[2] = msg.MoveData[i];
                    self.bHasOldMove = true;
                }
            }
            self.bDisableCombinedScopedMove = msg.bDisableCombinedScopedMove;
            self.bIsDualHybridRootMotionMove = msg.bIsDualHybridRootMotionMove;
        }
    }
}