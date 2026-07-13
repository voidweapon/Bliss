using System.Collections.Generic;
using ET;
using Unity.Mathematics;

namespace Bliss;

/// <summary>
/// 处理单位在地图间传送
/// </summary>
[MessageHandler(ELogicSceneType.Map)]
public class MessageHandler_M2M_UnitTransferRequest : MessageHandler<LogicScene, M2M_UnitTransferRequest , M2M_UnitTransferResponse>
{
    protected override async ETTask Run(LogicScene mapScene, M2M_UnitTransferRequest request, M2M_UnitTransferResponse response)
    {
        var levelConfigList = ConfigManager.Instance.Tables.TbLevelConfig.DicSceneId2Level[(int)mapScene.Id];
        var defaultLevelConfig = levelConfigList[0];
        var gameplayWorld = mapScene.GetChild<LogicScene>(1);
        var levelScene = gameplayWorld.GetChild<LogicScene>(defaultLevelConfig.Id);
        var playerSpawnPointMgr = levelScene.GetComponent<LevelPlayerSpawnPointMgr>();
        
        //出生位置
        float3 spawnPoint = float3.zero;
        quaternion spawnRotation = quaternion.identity;
        if (playerSpawnPointMgr != null && playerSpawnPointMgr.PlayerSpawnPoints != null &&
            playerSpawnPointMgr.PlayerSpawnPoints.Count > 0)
        {
            var spawnPointConfig = ((PlayerSpawnPoint)playerSpawnPointMgr.PlayerSpawnPoints[0]).Config;
            spawnPoint = spawnPointConfig.Position;
            spawnRotation =  spawnPointConfig.Rotation;
        }
        
        Entity entity = MongoHelper.Deserialize<Entity>(request.RawEntity);
        gameplayWorld.AddChild(entity);
        entity.AddComponent<MailBoxComponent, EMailBoxType>(EMailBoxType.OrderedMessage);
        entity.GetComponent<TransformComponent>()?.SetPositionAndRotation(spawnPoint, spawnRotation);
        
        M2C_ChangeMap m2c_changeMapMessage = M2C_ChangeMap.Create();
        m2c_changeMapMessage.LevelId = mapScene.Root().Id;
        m2c_changeMapMessage.LevelConfigId = levelConfigList[0].Id;
        m2c_changeMapMessage.IsLogin = request.IsLogin;
        MapMessageHelper.SendToClient(entity, m2c_changeMapMessage);

        if (request.IsLogin)
        {
            M2C_CreateMyUnit m2c_createMyUnit = M2C_CreateMyUnit.Create();
            m2c_createMyUnit.Unit = ((Unit)entity).CreateDTOUnitInfo();
            MapMessageHelper.SendToClient(entity, m2c_createMyUnit);
        }

        await MapTransferHelper.FinishTransferUnit(entity, request.FromAddress);
        
        UnitHelper.CreateUnit(gameplayWorld, (Unit)entity, true);
        //全量状态同步
        entity.AddComponent<RemoteGhost>();
        
        var unitManager = gameplayWorld.GetComponent<UnitManager>();
        if (unitManager != null && unitManager.DicEntityId2Unit.Count > 1)
        {
            M2C_CreateUnits m2c_CreateUnits = M2C_CreateUnits.Create();
            
            foreach (var kv in unitManager.DicEntityId2Unit)
            {
                var unit = (Unit)kv.Value;
                if(unit == entity) continue;

                var dotUnitInfo = unit.CreateDTOUnitInfo();
                dotUnitInfo.RemoteRole = ENetRole.SimulatedProxy;
                m2c_CreateUnits.Units.Add(dotUnitInfo);
            }
            MapMessageHelper.SendToClient(entity, m2c_CreateUnits);
        }
        
        var sceneActorManager = gameplayWorld.GetComponent<SceneActorManager>();
        if (sceneActorManager != null && sceneActorManager.DicEntityId2SceneActor.Count > 0)
        {
            M2C_CreateSceneActors m2C_CreateSceneActors = M2C_CreateSceneActors.Create();
            
            foreach (var kv in sceneActorManager.DicEntityId2SceneActor)
            {
                var sceneActor = (SceneActor)kv.Value;
                
                ASceneActorCmp actorCmp = null;
                foreach (var cmpKv in sceneActor.Components)
                {
                    if (cmpKv.Value is ASceneActorCmp aSceneActorCmp)
                    {
                        actorCmp =  aSceneActorCmp;
                        break;
                    }
                }
                
                if(actorCmp == null) continue;
                m2C_CreateSceneActors.SceneActors.Add(actorCmp.RawConfigData);
            }
            MapMessageHelper.SendToClient(entity, m2C_CreateSceneActors);
        }
        
        var bulletManager = gameplayWorld.GetComponent<BulletManager>();
        if (bulletManager != null && bulletManager.DicEntityId2Bullet.Count > 0)
        {
            M2C_CreateBullets  m2C_CreateBullets = M2C_CreateBullets.Create();
            
            foreach (var kv in bulletManager.DicEntityId2Bullet)
            {
                var bullet = (Bullet)kv.Value;
                m2C_CreateBullets.Bullets.Add(bullet.CreateDTOBulletInfo());
            }
            MapMessageHelper.SendToClient(entity, m2C_CreateBullets);
        }

        Log.Info($"单位 {entity.Id} 进入地图{mapScene.Id}!");
    }
}