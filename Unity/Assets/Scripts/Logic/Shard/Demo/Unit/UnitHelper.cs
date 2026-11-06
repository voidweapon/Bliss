using Bliss.CharacterControllers;
using Unity.Mathematics;

namespace Bliss
{
    public static class UnitHelper
    {
        public static Unit CreateMinimalUnit(LogicScene root,
            int unitConfigId, long unitId,
            ENetRole localRole, ENetRole remoteRole,
            float3 position, quaternion rotation)
        {
            Unit unit = root.AddChildWithId<Unit>(unitId);
            unit.UnitConfigId = unitConfigId;

            unit.AddComponent<GameplayLayer, int>(GameplayLayerDefine.Unit);
            
            var transformComponent = unit.AddComponent<TransformComponent>();
            transformComponent.SetPositionAndRotation(position, rotation);

            unit.AddComponent<NetRoleComponent, NetRoleComponentInitParam>(new NetRoleComponentInitParam() { LocalNetRole = localRole, RemoteNetRole = remoteRole });
            
            return unit;
        }
        
        /// <summary>
        ///补全Unit组件
        /// </summary>
        /// <param name="root"></param>
        /// <param name="minimalUnit"></param>
        /// <param name="isReplicate"></param>
        /// <param name="isLoadViewModel"></param>
        /// <returns></returns>
        public static Unit CreateUnit(LogicScene root, Unit minimalUnit,
            bool isReplicate,
            bool isLoadViewModel = true)
        {
            minimalUnit.AddComponent<ObjectWait>();
#if Client
            minimalUnit.AddComponent<SmoothTransformComponent, SmoothTransformComponentInitParam>( new SmoothTransformComponentInitParam(ENetworkSmoothingMode.Linear));
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
    }
}