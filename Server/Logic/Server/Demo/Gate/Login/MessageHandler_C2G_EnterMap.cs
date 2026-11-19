using System;
using ET;
using Unity.Mathematics;

namespace Bliss;

/// <summary>
/// 处理玩家登录请求
/// </summary>
[SessionMessageHandler(ELogicSceneType.Gate)]
public class MessageHandler_C2G_EnterMap : SessionMessageHandler<C2G_EnterMap, G2C_EnterMap>
{
    private const int LOGIN_UNIT_CONFIG_ID = 1;

    protected override async ETTask Run(Session session, C2G_EnterMap requst, G2C_EnterMap response)
    {
        LogicScene scene = session.RootScene();

        long playerId = session.GetComponent<SessionPlayerComponent>().Player.Id;
        
        if (!ConfigManager.Instance.Tables.TbServerScene.DicId2Maps.TryGetValue(requst.MapId, out var mapConfig))
        {
            response.Error = ErrorCode.ERR_NOT_FOUND_MAP;
            return;
        }

        GameplayWorld world = scene.GetChild<GameplayWorld>(GameplayWorldFactory.DEFAULT_WORLD_ID)
            ?? throw new InvalidOperationException("The Gate login transit GameplayWorld is missing.");
        Unit unit = UnitHelper.CreateMinimalUnit(world,
            LOGIN_UNIT_CONFIG_ID, playerId,
            ENetRole.Authority, ENetRole.AutonomousProxy,
            float3.zero, quaternion.identity);

        //从Gate走传送流程到指定Map
        Log.Info($"玩家 {playerId} 开始进入地图{requst.MapId}, 地图地址{mapConfig.ActorAddress}");
        await MapTransferHelper.StartTransferUnit(unit, mapConfig.ActorAddress, true);
        Log.Info($"玩家 {playerId} 进入地图{requst.MapId}!");
    }
}