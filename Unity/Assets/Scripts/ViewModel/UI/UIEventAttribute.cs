using System;
using Bliss.Core;

namespace Bliss
{
    public class UIEventAttribute: BaseAttribute
    {
        public string UIType { get; }

        public UIEventAttribute(string uiType)
        {
            this.UIType = uiType;
        }
    }
}