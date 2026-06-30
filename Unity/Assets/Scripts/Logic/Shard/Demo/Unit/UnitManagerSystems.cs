namespace Bliss
{
    public static class UnitManagerSystems
    {
        public static void Add(this UnitManager self, Unit unit)
        {
            self.DicEntityId2Unit.Add(unit.Id, unit);
        }

        public static void Remove(this UnitManager self, long id)
        {
            self.DicEntityId2Unit.Remove(id);
        }
        
        public static bool GetUnit(this UnitManager self, long id, out EntityRef<Unit> unit)
        {
            return self.DicEntityId2Unit.TryGetValue(id, out unit);
        }
    }
}