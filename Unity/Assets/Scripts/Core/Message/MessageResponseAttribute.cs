using System;

namespace Bliss
{
    public class MessageResponseAttribute : Attribute
    {
        /// <summary>
        /// 消息类型可能定义在不同程序集，相互之间不直接引。
        /// 所以用Type的FullName从<see cref="Bliss.Core.TypeManager"/>获取类型
        /// </summary>
        public string ResponseType {get; private set;}

        public MessageResponseAttribute(string responseTypeFullName)
        {
            ResponseType = responseTypeFullName;
        }
    }
}