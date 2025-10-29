using Bliss.Core;
using Bliss.ViewModel;
using Bliss.ViewModel.Debugger;
using TMPro;

namespace Bliss.ViewLogic.Debugger
{
    [EntitySystemOf(typeof(NetStatDebugComponent))]
    public static partial class NetStatDebugComponentSystems
    {
        public static void Awake(this NetStatDebugComponent self)
        {
            var mainScene = self.Scene<LogicScene>().Parent as LogicScene;
            var viewGlobal =  mainScene.GetComponent<ViewGlobalComponent>();
            var netStatUI =  viewGlobal.UGUI_Canvas_Debug.Find("Net Stat/NetStatRect");
            self.UI = netStatUI.gameObject;
            self.TextPing = netStatUI.Find("Stat_Table/Text_V_Ping").GetComponent<TextMeshProUGUI>();
            self.TextMiss = netStatUI.Find("Stat_Table/Text_V_Miss").GetComponent<TextMeshProUGUI>();
            self.TextLoss = netStatUI.Find("Stat_Table/Text_V_Loss").GetComponent<TextMeshProUGUI>();
            self.TextJitter = netStatUI.Find("Stat_Table/Text_V_Jitter").GetComponent<TextMeshProUGUI>();
            self.Graphic_Jitter = netStatUI.Find("JitterDiagram").GetComponent<FixMax_DataUIGraphic>();
        }
        public static void Destroy(this NetStatDebugComponent self)
        {
            self.TextPing = null;
            self.TextMiss = null;
            self.TextLoss = null;
            self.TextJitter = null;
            self.Graphic_Jitter = null;
        }
        
        public static void Update(this NetStatDebugComponent self)
        {
            var session = self.GetSceneComponent<SessionComponent>()?.Session;
            if (session == null)
            {
                return;
            }

            var pingCmp = session.GetComponent<PingComponent>();
            if (pingCmp == null)
            {
                return;
            }

            if ((int)pingCmp.RTT_EMA.Value != self.Simpled_Ping)
            {
                self.Simpled_Ping = (int)pingCmp.RTT_EMA.Value;
                self.TextPing.text = $"{self.Simpled_Ping}ms";
            }
            
            if ((int)pingCmp.Jitter_EMA.Value != self.Simpled_Jitter)
            {
                self.Simpled_Jitter = (int)pingCmp.Jitter_EMA.Value;
                self.TextJitter.text = $"{self.Simpled_Jitter}ms";
                self.Graphic_Jitter.Value =  self.Simpled_Jitter;
            }
        }

        public static void Toggle(this NetStatDebugComponent self)
        {
            self.UI.SetActive(!self.UI.activeInHierarchy);
        }
    }
}