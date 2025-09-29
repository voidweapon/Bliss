using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace Bliss.Id
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct IdStruct
    {
        public short Process;  // 14bit
        public uint Time;    // 30bit
        public uint Value;   // 20bit

        public long ToLong()
        {
            ulong result = 0;
            result |= (ushort) Process;
            result <<= 30;
            result |= Time;
            result <<= 20;
            result |= Value;
            return (long) result;
        }

        public IdStruct(uint time, short process, uint value)
        {
            Process = process;
            Time = time;
            Value = value;
        }

        public IdStruct(long id)
        {
            ulong result = (ulong) id; 
            Value = (uint) (result & IdGenerator.Mask20bit);
            result >>= 20;
            Time = (uint) result & IdGenerator.Mask30bit;
            result >>= 30;
            Process = (short) (result & IdGenerator.Mask14bit);
        }

        public override string ToString()
        {
            return $"process: {Process}, time: {Time}, value: {Value}";
        }

        public static explicit operator long(IdStruct id)
        {
            return id.ToLong();
        }
        public static explicit operator IdStruct(long id)
        {
            return new IdStruct(id);
        }
    }
    
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct InstanceIdStruct
    {
        public uint Time;  // 32bit
        public uint Value; // 32bit

        public long ToLong()
        {
            ulong result = 0;
            result |= Time;
            result <<= 32;
            result |= Value;
            return (long) result;
        }

        public InstanceIdStruct(uint time, uint value)
        {
            Time = time;
            Value = value;
        }

        public InstanceIdStruct(long id)
        {
            ulong result = (ulong) id; 
            Value = (uint)(result & uint.MaxValue);
            result >>= 32;
            Time = (uint)(result & uint.MaxValue);
        }

        public override string ToString()
        {
            return $"time: {Time}, value: {Value}";
        }
        
        public static explicit operator long(InstanceIdStruct id)
        {
            return id.ToLong();
        }
        public static explicit operator InstanceIdStruct(long id)
        {
            return new InstanceIdStruct(id);
        }
    }
    
    public class IdGenerator
    {
        private static IdGenerator _instance;

        public static IdGenerator Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new IdGenerator();
                return _instance;
            }
        }

        public short Process;
        
        public const int MaxZone = 1024;
        
        public const int Mask14bit = 0x3fff;
        public const int Mask30bit = 0x3fffffff;
        public const int Mask20bit = 0xfffff;
        
        private long epoch2022;
        
        private int value;
        private int instanceIdValue;
        
        private IdGenerator()
        {
            long epoch1970Tick = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).Ticks / 10000;
            epoch2022 = new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc).Ticks / 10000 - epoch1970Tick;
        }
        
        private uint TimeSince2022(long frameTimeSince1970MS)
        {
            uint a = (uint)((frameTimeSince1970MS - epoch2022) / 1000);
            return a;
        }
        
        public long GenerateId(long frameTimeSince1970MS)
        {
            uint time = TimeSince2022(frameTimeSince1970MS);
            int v = 0;
            
            // 这里必须加锁
            lock (this)
            {
                if (++value > Mask20bit - 1)
                {
                    value = 0;
                }
                v = value;
            }
            
            IdStruct idStruct = new(time, Process, (uint)v);
            return idStruct.ToLong();
        }
        
        public long GenerateInstanceId(long frameTimeSince1970MS)
        {
            uint time = TimeSince2022(frameTimeSince1970MS);
            uint v = (uint)Interlocked.Add(ref instanceIdValue, 1);
            InstanceIdStruct instanceIdStruct = new(time, v);
            return instanceIdStruct.ToLong();
        }
    }
}