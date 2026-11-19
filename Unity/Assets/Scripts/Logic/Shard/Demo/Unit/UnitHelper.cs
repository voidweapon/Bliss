using System;
using System.Runtime.ExceptionServices;
using Bliss.CharacterControllers;
using Unity.Mathematics;

namespace Bliss
{
    public static class UnitHelper
    {
        /// <summary>
        /// Creates and mounts the minimum components required for a Unit.
        /// </summary>
        /// <param name="root">The scene that owns the Unit.</param>
        /// <param name="unitConfigId">The Unit configuration identifier.</param>
        /// <param name="unitId">The Unit identifier within the scene.</param>
        /// <param name="localRole">The local network role.</param>
        /// <param name="remoteRole">The remote network role.</param>
        /// <param name="position">The initial world position.</param>
        /// <param name="rotation">The initial world rotation.</param>
        /// <returns>The mounted Unit.</returns>
        public static Unit CreateMinimalUnit(LogicScene root,
            int unitConfigId, long unitId,
            ENetRole localRole, ENetRole remoteRole,
            float3 position, quaternion rotation)
        {
            Unit unit = root.AddChildWithId<Unit>(unitId);
            try
            {
                unit.UnitConfigId = unitConfigId;
                unit.AddComponent<GameplayLayer, int>(GameplayLayerDefine.Unit);

                var transformComponent = unit.AddComponent<TransformComponent>();
                transformComponent.SetPositionAndRotation(position, rotation);

                unit.AddComponent<NetRoleComponent, NetRoleComponentInitParam>(
                    new NetRoleComponentInitParam() { LocalNetRole = localRole, RemoteNetRole = remoteRole });

                return unit;
            }
            catch (Exception exception)
            {
                disposeFailedUnit(unit, exception);
                throw;
            }
        }

        /// <summary>Mounts an unmounted transferred Unit and restores its non-serialized gameplay layer.</summary>
        /// <param name="destination">The destination GameplayWorld or an enabled LevelScene within it.</param>
        /// <param name="unit">Deserialized Unit to mount.</param>
        /// <exception cref="InvalidOperationException">The destination is invalid or the Unit is already mounted.</exception>
        public static void MountTransferredUnit(LogicScene destination, Unit unit)
        {
            if (destination == null || destination.IsDisposed || unit == null ||
                unit.Parent != null || unit.LogicScene != null)
            {
                throw new InvalidOperationException("A live destination and an unmounted transferred Unit are required.");
            }

            if (destination.SceneType == ELogicSceneType.Level)
            {
                LevelGameWorldComponent association = destination.GetComponent<LevelGameWorldComponent>();
                if (association == null || !association.IsEnabled || association.GameplayWorld == null ||
                    association.GameplayWorld.IsDisposed)
                {
                    throw new InvalidOperationException("The destination LevelScene must be enabled and owned by a live GameplayWorld.");
                }
                association.ValidateOwnership(association.GameplayWorld);
            }
            else if (destination is not GameplayWorld)
            {
                throw new InvalidOperationException("The destination must be a GameplayWorld or an enabled LevelScene.");
            }

            try
            {
                destination.AddChild(unit);
                if (unit.GetComponent<GameplayLayer>() == null)
                {
                    unit.AddComponent<GameplayLayer, int>(GameplayLayerDefine.Unit);
                }
            }
            catch (Exception exception)
            {
                disposeFailedUnit(unit, exception);
                throw;
            }
        }
        
        /// <summary>
        /// Completes Unit setup and removes the partially created Unit if setup fails.
        /// </summary>
        /// <param name="root">The scene that owns the Unit.</param>
        /// <param name="minimalUnit">The Unit to complete.</param>
        /// <param name="isReplicate">Whether to add its ordered mailbox.</param>
        /// <param name="isLoadViewModel">Whether to load its view model.</param>
        /// <returns>The completed Unit.</returns>
        public static Unit CreateUnit(LogicScene root, Unit minimalUnit,
            bool isReplicate,
            bool isLoadViewModel = true)
        {
            try
            {
                minimalUnit.AddComponent<ObjectWait>();
#if Client
                minimalUnit.AddComponent<SmoothTransformComponent, SmoothTransformComponentInitParam>(new SmoothTransformComponentInitParam(ENetworkSmoothingMode.Linear));
#endif
                if (isReplicate)
                {
                    minimalUnit.AddComponent<MailBoxComponent, EMailBoxType>(EMailBoxType.OrderedMessage);
                }

                Log.Info($"Start Load Unit View Node");
                var gameplayScene = root.GetLevelGameplayWorld();
                var unitConfig = minimalUnit.GetUnitConfig();
                EventSystem.Instance.Push(root.Scene(), new Event_AfterCreateSceneActor()
                {
                    Actor = minimalUnit,
                    ViewAssetPath = unitConfig.ModelPath,
                    LoadViewModel = isLoadViewModel,
                });
                Log.Info($"End Load Unit View Node");

                Log.Info($"Start Init UnitCharacterController");
                minimalUnit.AddComponent<CharacterController>();
                var transformComponent = minimalUnit.GetComponent<TransformComponent>();
                var aoiManager = gameplayScene.GetComponent<AoiManagerComponent>();
                var aoiIdentifier = aoiManager.Register(AoiRegisterInfo.CreateSphere(minimalUnit, AOIType.Visibility, transformComponent.Transform.Location, 10f));
                var aoi = minimalUnit.AddComponent<AOIEntityCmp>();
                aoi.Add(AOIType.Visibility, aoiIdentifier);
#if DOTNET
                var netAoiIdentifier = aoiManager.Register(AoiRegisterInfo.CreateSphere(minimalUnit, AOIType.NetworkVisibility, transformComponent.Transform.Location, 10f));
                aoi.Add(AOIType.NetworkVisibility, netAoiIdentifier);
#endif

                return minimalUnit;
            }
            catch (Exception exception)
            {
                disposeFailedUnit(minimalUnit, exception);
                throw;
            }
        }
        
        public static Unit CreateUnit(LogicScene root, 
            int unitConfigId, long unitId, 
            ENetRole localRole, ENetRole remoteRole, 
            float3 position, quaternion rotation, 
            bool isReplicate,
            bool isLoadViewModel = true)
        {
            Unit unit = CreateMinimalUnit(root, unitConfigId, unitId, localRole, remoteRole, position, rotation);
            return CreateUnit(root, unit, isReplicate, isLoadViewModel);
        }

        public static Unit CreateUnit(this DTOUnitInfo unitInfo, LogicScene root)
        {
            Unit unit = CreateUnit(root,
                unitInfo.ConfigId, unitInfo.UnitId, 
                unitInfo.LocalRole, unitInfo.RemoteRole, 
                unitInfo.Position, unitInfo.Rotation,
                true, true);
     
            return unit;
        }

        public static DTOUnitInfo CreateDTOUnitInfo(this Unit unit)
        {
            DTOUnitInfo unitInfo = DTOUnitInfo.Create();
            unitInfo.UnitId = unit.Id;
            unitInfo.ConfigId = unit.UnitConfigId;
        
            var transformCmp = unit.GetComponent<TransformComponent>();
            unitInfo.Position = transformCmp.Transform.Location;
            unitInfo.Rotation = transformCmp.Transform.Rotation;
            
            var netRoleComponent = unit.GetComponent<NetRoleComponent>();
            unitInfo.LocalRole =  netRoleComponent.LocalNetRole;
            unitInfo.RemoteRole = netRoleComponent.RemoteNetRole;
        
            return unitInfo;
        }

        private static void disposeFailedUnit(Unit unit, Exception creationException)
        {
            try
            {
                if (!unit.IsDisposed)
                {
                    unit.Dispose();
                }
            }
            catch (Exception cleanupException)
            {
                throw new AggregateException("Unit creation and cleanup failed.", creationException, cleanupException);
            }

            ExceptionDispatchInfo.Capture(creationException).Throw();
        }
    }
}