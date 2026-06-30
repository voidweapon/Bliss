using Bliss;
using Bliss.ViewModel;
using ET;
using UnityEngine.SceneManagement;

namespace Bliss.ViewLogic
{
    [Event(ELogicSceneType.Client)]
    public class Event_InitProcessAppInitFinish_InitClientView : AEvent<LogicScene, Event_InitProcessAppInitFinish>
    {
        protected override async ETTask Run(LogicScene mainScene, Event_InitProcessAppInitFinish eventInfo)
        {
            // var mainScene = scene.Parent as LogicScene;
            // var netCmp = mainScene.GetComponent<NetworkComponent>();
            // Log.Debug($"Connect to Server: 127.0.0.1:9001");
            //
            // var session = netCmp.CreateConnection(NetworkHelper.ToIPEndPoint("127.0.0.1", 9001));
            // session.AddComponent<PingComponent>();
            // scene.AddComponent<SessionComponent>().Session = session;

            var scenePath = "Assets/Res/Scenes/DemoScene.unity";
            await mainScene.GetComponent<ResourcesLoaderComponent>().LoadSceneAsync(scenePath, LoadSceneMode.Single);
            
            await ETTask.CompletedTask;
        }
    }
}