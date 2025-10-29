using System.Linq;
using Bliss.Core;
using Bliss.ViewModel;
using Bliss.ViewModel.Debugger;
using QFSW.QC;
using QFSW.QC.Suggestors.Tags;

namespace Bliss.ViewLogic.Debugger
{
    public static partial class ConsoleCommands
    {
        [Command("net_stat")]
        public static void ShowNetStat()
        {
            Console.Instance.AttachedScene.GetSceneComponent<NetStatDebugComponent>().Toggle();
        }

        [Command("net")]
        public static void NetCommand(
            [Suggestions("LocalClient","RemoteClient", "Server")]string sceneName,
            [Suggestions("Latency", "LatencyVariance", "LatencyMin", "LatencyMax", "Jitter")]string settingName,
            int settingValue)
        {
            if (!TryGetTestScene(sceneName, out var resultScene)) return;

            var netDriver = resultScene.GetComponent<NetworkComponent>()?.NetDriver;
            if(netDriver == null) return;
            var oldSetting = netDriver.SimulationSettings;
            oldSetting.ParseCommandLineArgs(settingName, settingValue);
            netDriver.SetNetSimulationSettings(oldSetting);
        }

        [Command("net_pkt_loss_burst")]
        public static void NetPktLossBurstCommand(
            [Suggestions("LocalClient","RemoteClient", "Server")]string sceneName,
            int durationMS)
        {
            if (!TryGetTestScene(sceneName, out var resultScene)) return;

            var netDriver = resultScene.GetComponent<NetworkComponent>()?.NetDriver;
            if(netDriver == null) return;
            netDriver.HandlePacketLossBurstCommand(durationMS);
        }

        [Command("tranform_stat")]
        public static void ToggleTransformStat()
        {
            var mainScene = Console.Instance.AttachedScene.Parent as LogicScene;
            mainScene?.GetComponent<TransformStatDebugComponent>()?.Toggle();
        }
        
        [Command("debug_stat")]
        public static void ToggleGameDebugStat()
        {
            GameDebugStatesView.Instance?.Toggle();
        }
        
        [Command("debug_transfrom_sync")]
        public static void ToggleTransformDebugStat()
        {
            Console.Instance.AttachedScene.GetSceneComponent<NetStatDebugComponent>().Toggle();
            var mainScene = Console.Instance.AttachedScene.Parent as LogicScene;
            mainScene?.GetComponent<TransformStatDebugComponent>()?.Toggle();
            GameDebugStatesView.Instance?.Toggle();
        }
        
        private static bool TryGetTestScene(string sceneName, out LogicScene resultScene)
        {
            var mainScene = Console.Instance.AttachedScene.Parent.Scene<LogicScene>();
            if (mainScene == null)
            {
                resultScene = null;
                return false;
            }
            
            if (mainScene.ChildCount == 0)
            {
                resultScene = null;
                return false;
            }
            
            resultScene = null;
            foreach (var kv in mainScene.Children)
            {
                var child = kv.Value;
                if (child is LogicScene logicScene)
                {
                    if (logicScene.Name == sceneName)
                    {
                        resultScene =  logicScene;
                        break;
                    }
                }
            }

            if (resultScene == null)
            {
                return false;
            }

            return true;
        }

    }
    
    
}