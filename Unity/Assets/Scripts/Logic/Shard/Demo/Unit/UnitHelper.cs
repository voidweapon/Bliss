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
        public static Unit CreateUnit(LogicScene root, 
            int unitConfigId, long unitId, 
            ENetRole localRole, ENetRole remoteRole, 
            float3 position, quaternion rotation, 
            bool isReplicate,
            bool isLoadViewModel = true)
        {
            Unit unit = CreateMinimalUnit(root, unitConfigId, unitId, localRole, remoteRole, position, rotation);
            unit.AddComponent<ObjectWait>();
#if Client
            unit.AddComponent<SmoothTransformComponent, SmoothTransformComponentInitParam>( new SmoothTransformComponentInitParam(ENetworkSmoothingMode.Linear));
#endif
            if (isReplicate)
            {   
                unit.AddComponent<MailBoxComponent, EMailBoxType>(EMailBoxType.OrderedMessage);
            }

            Log.Info($"Start Load Unit View Node");
            var unitConfig = unit.GetUnitConfig();
            EventSystem.Instance.Push(root.Scene(), new Event_AfterCreateSceneActor()
            {
                Actor = unit,
                ViewAssetPath = unitConfig.ModelPath,
                LoadViewModel = isLoadViewModel,
            });
            Log.Info($"End Load Unit View Node");
            
            
            Log.Info($"Start Init UnitCharacterController");
            unit.AddComponent<CharacterController>();


            return unit;
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