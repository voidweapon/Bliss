using System;
using System.Collections.Generic;
using Bliss.Base.Collections;
using Bliss.Core;

namespace Bliss
{
    /// <summary>
    /// 管理消息编号，request到Response到映射
    /// </summary>
    public class MessageOpcodeManager : Singleton<MessageOpcodeManager>, ISingletonInit
    {
        /// <summary>
        /// key: opcode -- value: message type
        /// </summary>
        private readonly DoublyDictionary<ushort, Type> _opcode2Type = new DoublyDictionary<ushort, Type>(); 
        
        /// <summary>
        /// key: request message type -> value: response message type
        /// </summary>
        private readonly Dictionary<Type, Type> _request2Response = new Dictionary<Type, Type>();
        
        public void Init()
        {
            foreach (var type in TypeManager.Instance.GetTypes(typeof(MessageOpcodeAttribute)))
            {
                object[] att = type.GetCustomAttributes(typeof (MessageOpcodeAttribute), false);
                if (att.Length == 0)
                {
                    continue;
                }

                var messageAttribute = att[0] as MessageOpcodeAttribute;
                if (messageAttribute == null)
                {
                    continue;
                }
                
                ushort opcode = messageAttribute.Opcode;
                if (opcode == 0)
                {
                    continue;
                }
                
                AddOrUpdate(opcode, type);
                
                var attrs = type.GetCustomAttributes(typeof (MessageResponseAttribute), false);
                if (attrs.Length == 0)
                {
                    continue;
                }
                var responseTypeAttribute = attrs[0] as MessageResponseAttribute;
                if (responseTypeAttribute == null)
                {
                    continue;
                }
                var responseType = TypeManager.Instance.GetType(responseTypeAttribute.ResponseType);
                AddResponse(type, responseType);
            }
        }
        
        public void AddOrUpdate(ushort opcode, Type messageType)
        {
            if (_opcode2Type.ContainsValue(messageType))
            {
                _opcode2Type.RemoveByValue(messageType);
            }
            _opcode2Type.Add(opcode, messageType);
        }
        
        public bool GetMessageTypeBy(ushort opcode, out Type messageType)
        {
            return _opcode2Type.TryGetValueByKey(opcode, out messageType);
        }
        
        public bool TryGetOpcodeTypeBy(Type messageType, out ushort opcode)
        {
           return _opcode2Type.TryGetKeyByValue(messageType, out opcode);
        }

        public void AddResponse(Type request, Type responseType)
        {
            _request2Response.Add(request, responseType);
        }
    }
}