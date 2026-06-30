using Bliss.Core;

namespace Bliss
{
    [EntitySystemOf(typeof(Unit))]
    public static partial class UnitSystems
    {
        private static void Awake(this Unit self)
        {
        }
        
        private static void MountToScene(this Unit self)
        {
            var networkEntityManagerCmp = self.RootScene().GetComponent<NetworkEntityManagerComponent>();
            networkEntityManagerCmp?.AddEntity(self.Id, self);

            var unitManager = self.GetLevelGameplayWorld()?.GetComponent<UnitManager>();
            unitManager?.Add(self);
        }

        private static void Destroy(this Unit self)
        {
            self.UnitConfigId = 0;
            var networkEntityManagerCmp = self.RootScene().GetComponent<NetworkEntityManagerComponent>();
            networkEntityManagerCmp?.RemoveEntity(self.Id);

            var unitManager = self.GetLevelGameplayWorld()?.GetComponent<UnitManager>();
            unitManager?.Remove(self.Id);
        }

        public static UnitConfig GetUnitConfig(this Unit self)
        {
            return ConfigManager.Instance.Tables.TbUnitConfig.GetOrDefault(self.UnitConfigId);
        }

    }
}