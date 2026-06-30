using Bliss.Core.EventSystem;
using UnityEngine;

namespace Bliss.ViewLogic.Config
{
    [Invoke(ConfigManager.InvokeType_LoadFile)]
    public class InvokeHandler_ConfigManagerLoadFile : AInvokeHandler<ConfigManager.LoadFile, string>
    {
        public override string Handle(ConfigManager.LoadFile args)
        {
            var fileText = SimpleResourcesComponent.Instance.LoadAsset<TextAsset>(args.FileName).text;
            return fileText;
        }
    }
}