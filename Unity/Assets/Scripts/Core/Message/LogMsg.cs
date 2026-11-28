using System;
using System.Collections.Generic;

namespace Bliss
{
    public partial class LogMsg : Singleton<LogMsg>, ISingletonInit
    {
        private bool _useIgnoreRule = true;
        private HashSet<ushort> _ignore = new()
        {

        };

        private bool _useIncludeRule = true;
        private HashSet<ushort> _include = new()
        {

        };
        
        public void Init()
        {
            
        }

        public void EnableIgnoreRule(bool enable)
        {
            _useIgnoreRule = enable;
        }
        public void AddIgnoreRule(HashSet<ushort> ignore)
        {
            foreach (var i in ignore)
            {
                _ignore.Add(i);
            }
        }
        public void RemoveIgnoreRule(HashSet<ushort> ignore)
        {
            foreach (var i in ignore)
            {
                _ignore.Remove(i);
            }
        }

        public void EnableIncludeRule(bool enable)
        {
            _useIncludeRule = enable;
        }
        public void AddIncludeRule(HashSet<ushort> include)
        {
            foreach (var i in include)
            {
                _include.Add(i);
            }
        }
        public void RemoveIncludeRule(HashSet<ushort> include)
        {
            foreach (var i in include)
            {
                _include.Remove(i);
            }
        }

    
        public void Debug(Entity sender, object msg)
        {
            if (Check(msg))
            {
                var scene = sender.Scene();
                Log.Debug($"[Network][{scene.SceneType}][{scene.Name}]:{msg.GetType()}  {msg.ToString()}");
            }
        }
        
        public void DebugInnerMsg(Entity sender, ActorAddress targetAddress, object msg, bool send)
        {
            if (Check(msg))
            {
                var scene = sender.Scene();
                if (send)
                {
                    Log.Debug($"[ProcessInner][{scene.SceneType}][{scene.Name}]:{msg.GetType()} To {targetAddress}");
                }
                else
                {
                    Log.Debug($"[ProcessInner][{scene.SceneType}][{scene.Name}]:{msg.GetType()} From {targetAddress}");
                }
            }
        }

        public void DebugInnerMsg(Entity sender, object msg, string message)
        {
            if (Check(msg))
            {
                var scene = sender.Scene();
                Log.Debug($"[ProcessInner][{scene.SceneType}][{scene.Name}]:{msg.GetType()} -- {message}");
            }
        }

        /// <summary>Logs gateway routing without including the business message body.</summary>
        /// <param name="origin">The original sending Root.</param>
        /// <param name="target">The final business Actor.</param>
        /// <param name="opcode">The business message opcode.</param>
        /// <param name="stage">The gateway routing stage.</param>
        /// <param name="rpcId">The relevant RPC correlation identifier, or zero for one-way messages.</param>
        public void DebugInternalGateway(RootAddress origin, ActorAddress target, ushort opcode, string stage, int rpcId)
        {
            if (_useIncludeRule && !_include.Contains(opcode)
                || _useIgnoreRule && _ignore.Contains(opcode))
            {
                return;
            }

            if (!MessageOpcodeManager.Instance.GetMessageTypeBy(opcode, out Type messageType))
            {
                return;
            }

            Log.Debug($"[InternalGateway][{stage}] {origin} -> {target} {messageType.FullName} RpcId={rpcId}");
        }

        private bool Check(object msg)
        {
            if(MessageOpcodeManager.Instance.TryGetOpcodeTypeBy(msg.GetType(), out ushort opcode))
            {
                if (_useIgnoreRule && _ignore.Contains(opcode))
                {
                    return false;
                }

                if (_useIncludeRule && !_include.Contains(opcode))
                {
                    return false;
                }
                return true;
            }
            
            return false;
        }
    }
}