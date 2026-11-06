using Bliss.Base.Collections;
using Bliss.CharacterMovement;
using Bliss.Core;

namespace Bliss
{
    [EntitySystemOf(typeof(ReplicationGraph))]
    public static partial class ReplicationGraphSystems
    {
        private static void Awake(this ReplicationGraph self)
        {
            self.RemoteGhosts.Clear();
        }
        private static void Destroy(this ReplicationGraph self)
        {
            self.RemoteGhosts.Clear();
        }

        internal static void AddGhost(this ReplicationGraph self, EntityRef<RemoteGhost> ghost)
        {
            self.RemoteGhosts.Add(ghost);
        }

        internal static void RemoveGhost(this ReplicationGraph self, EntityRef<RemoteGhost> ghost)
        {
            self.RemoteGhosts.Remove(ghost);
        }

        internal static void UpdateGhosts(this ReplicationGraph self)
        {
            foreach (var ghost in self.RemoteGhosts)
            {
                RemoteGhost remoteGhost = (RemoteGhost)ghost;
                if (remoteGhost == null)
                {
                    continue;
                }

                if (remoteGhost.ClientVersion == 0)
                {
                    //全量同步
                    self.ReplicationFullStateToClient(remoteGhost);
                }
                else
                {
                    //增量同步
                    self.ReplicationIncrementStateToClient(remoteGhost);
                }
            }
        }

        /// <summary>
        /// 全量同步世界状态给客户端
        /// </summary>
        /// <param name="self"></param>
        /// <param name="ghost"></param>
        public static void ReplicationFullStateToClient(this ReplicationGraph self, RemoteGhost ghost)
        {
            LogicScene mapScene = self.Scene();
            var entity = ghost.Parent;
            //本单位的全量状态
            M2C_CreateMyUnit m2c_createMyUnit = M2C_CreateMyUnit.Create();
            m2c_createMyUnit.Unit = ((Unit)entity).CreateDTOUnitInfo();
            MapMessageHelper.SendToClient(entity, m2c_createMyUnit);
            
            //可视范围内实体的全量状态
            var unitAoi = entity.GetComponent<AOIEntityCmp>();
            unitAoi.GetId(AOIType.NetworkVisibility, out var networkVisibility);
            var aoiMgr = mapScene.GetComponent<AoiManagerComponent>();
            
            using var visibleStateSyncList = ListComponent<long>.Create();
            using var candidateUnitStateSyncList = ListComponent<Unit>.Create();
            using var candidateSceneActorStateSyncList = ListComponent<SceneActor>.Create();
            using var candidateBulletStateSyncList = ListComponent<Bullet>.Create();
            
            aoiMgr.GetVisibleTargets(networkVisibility, visibleStateSyncList,  AOIType.NetworkVisibility);
            if (visibleStateSyncList.Count > 0)
            {
                foreach (var visibleTarget in visibleStateSyncList)
                {
                    aoiMgr.Entries.TryGetValue(visibleTarget, out var aoiEntry);
                    if (aoiEntry == null) continue;
                    Entity targetEntity = (Entity)aoiEntry.EntityRef;
                    if (targetEntity == null) continue;
                    switch (targetEntity)
                    {
                        case Unit targetUnit:
                            candidateUnitStateSyncList.Add(targetUnit);
                            break;
                        case SceneActor targetSceneActor:
                            candidateSceneActorStateSyncList.Add(targetSceneActor);
                            break;
                        case Bullet bullet:
                            candidateBulletStateSyncList.Add(bullet);
                            break;
                    }
                }

                if (candidateUnitStateSyncList.Count > 0)
                {
                    M2C_CreateUnits m2c_CreateUnits = M2C_CreateUnits.Create();
            
                    foreach (var unit in candidateUnitStateSyncList)
                    {
                        if(unit == entity) continue;

                        var dotUnitInfo = unit.CreateDTOUnitInfo();
                        dotUnitInfo.RemoteRole = ENetRole.SimulatedProxy;
                        m2c_CreateUnits.Units.Add(dotUnitInfo);
                    }
                    MapMessageHelper.SendToClient(entity, m2c_CreateUnits);
                }

                if (candidateSceneActorStateSyncList.Count > 0)
                {
                    M2C_CreateSceneActors m2C_CreateSceneActors = M2C_CreateSceneActors.Create();
        
                    foreach (var sceneActor in candidateSceneActorStateSyncList)
                    {
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

                if (candidateBulletStateSyncList.Count > 0)
                {
                    M2C_CreateBullets  m2C_CreateBullets = M2C_CreateBullets.Create();
            
                    foreach (var bullet in candidateBulletStateSyncList)
                    {
                        m2C_CreateBullets.Bullets.Add(bullet.CreateDTOBulletInfo());
                    }
                    MapMessageHelper.SendToClient(entity, m2C_CreateBullets);
                }
            }
        }
        
        /// <summary>
        /// 增量同步世界状态给客户端
        /// </summary>
        /// <param name="self"></param>
        /// <param name="ghost"></param>
        public static void ReplicationIncrementStateToClient(this ReplicationGraph self, RemoteGhost ghost)
        {
            LogicScene gameplayScene = self.Scene();
            var entity = ghost.Parent;
            Unit unit = (Unit)entity;
            if (unit == null)
            {
                return;
            }

            var characterMovementComponent = unit.GetComponent<CharacterMovementComponent>();
            characterMovementComponent.GaterCurrentMovement();
            characterMovementComponent.SendClientAdjustment();
            
            var aoiMgr = gameplayScene.GetComponent<AoiManagerComponent>();
            var aoiCmp = unit.GetComponent<AOIEntityCmp>(); 
            RemoteGhost remoteGhost = unit.GetComponent<RemoteGhost>();
                        
            if (aoiCmp.GetId(AOIType.NetworkVisibility, out var networkVisibility))
            {
                using var tempAoiIdentifierList = ListComponent<long>.Create();
                using var fullStateSyncList = ListComponent<EntityRef<Entity>>.Create();
                using var incrementStateSyncList = ListComponent<EntityRef<Entity>>.Create();
                
                aoiMgr.GetVisibleObservers(networkVisibility, tempAoiIdentifierList, tagFilter: AOIType.NetworkVisibility);
                foreach (var aoiIdentifier in tempAoiIdentifierList)
                {
                    if(!aoiMgr.Entries.TryGetValue(aoiIdentifier, out var aoiEntry))
                    {
                        continue;
                    }
                    
                    // 处理 aoiEntry
                    Entity observerEntity = (Entity)aoiEntry.EntityRef;
                    if(observerEntity == null) continue;
                        
                    RemoteGhost observerRemoteGhost = observerEntity.GetComponent<RemoteGhost>();
                    if(observerRemoteGhost == null) continue;
                        
                    if(!observerRemoteGhost.TryGetValue(unit.Id, out var info))
                    {
                        continue;
                    }

                    if (info.Version == 0)
                    {
                        fullStateSyncList.Add(aoiEntry.EntityRef);
                    }
                    else
                    {
                        incrementStateSyncList.Add(aoiEntry.EntityRef);
                    }
                }

                if (fullStateSyncList.Count > 0)
                {
                    M2C_CreateUnits unitFullStateSyncMsg = M2C_CreateUnits.Create();
                    unitFullStateSyncMsg.Units.Add(unit.CreateDTOUnitInfo());
                    foreach (var fullStateEntityRef in fullStateSyncList)
                    {
                        Entity observerEntity = (Entity)fullStateEntityRef;
                        MapMessageHelper.SendToClient(observerEntity, unitFullStateSyncMsg);
                    }
                }

                if (incrementStateSyncList.Count > 0)
                {
                    var serverData = characterMovementComponent.GetPredictionData_Server_CharacterMovement();
                    if (serverData != null)
                    {
                        M2C_CharacterMovementRepData unitCharMovementMsg = M2C_CharacterMovementRepData.Create();
                        unitCharMovementMsg.ReplicatedMovement = characterMovementComponent.ReplicatedMovement;

                        foreach (var entityRef in incrementStateSyncList)
                        { 
                            Entity observerEntity = (Entity)entityRef;
                            RemoteGhost observerRemoteGhost = observerEntity.GetComponent<RemoteGhost>();
                            observerRemoteGhost.TryGetValue(unit.Id, out var info);
                            //Movement
                            if (info.TimeStamp < serverData.ServerTimeStamp)
                            {
                                info.TimeStamp = serverData.ServerTimeStamp; 
                                MapMessageHelper.SendToClient(observerEntity, unitCharMovementMsg);
                            }
                        }
                    }
                }
            }
        }
    }
}