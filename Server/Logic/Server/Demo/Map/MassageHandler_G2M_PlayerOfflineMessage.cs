using ET;

namespace Bliss;

/// <summary>
/// 处理玩家下线
/// 单位从Map下线逻辑
/// </summary>
[MessageHandler(ELogicSceneType.GameplayWorld)]
public class MassageHandler_G2M_PlayerOfflineMessage : LocationMessageHandler<Unit, G2M_PlayerOfflineMessage>
{
    protected override async ETTask Run(Unit entity, G2M_PlayerOfflineMessage message)
    {
        Log.Info($"单位 {entity.Id} 下线!");
        await entity.RemoveNetworkLocation((int)ENetworkLocationCategory.Unit);
        entity.Dispose();
        await ETTask.CompletedTask;
    }
}