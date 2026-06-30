namespace Bliss
{
    public static class GameplayTargetInfoHelper
    {
        public static GameplayTargetInfo Create(this DTOGameplayTargetInfo dto, LogicScene scene)
        {
            var networkEntityManager = scene.RootScene().GetComponent<NetworkEntityManagerComponent>();
            EntityRef<Entity> targetEntity = dto.EntityId == 0 ? default  : networkEntityManager.GetEntity(dto.EntityId);
            
            return new GameplayTargetInfo
            {
                TargetPos =  dto.Position,
                TargetDir = dto.Direction,
                TargetEntity = targetEntity,
            };
        }

        public static DTOGameplayTargetInfo Create(this GameplayTargetInfo targetInfo, bool isFromPool = false)
        {
            var dtoInfo = DTOGameplayTargetInfo.Create(isFromPool);
            dtoInfo.Position = targetInfo.TargetPos;
            dtoInfo.Direction = targetInfo.TargetDir;
            dtoInfo.EntityId = ((Entity)targetInfo.TargetEntity)?.Id ?? 0;
            
            return dtoInfo;
        }
    }
}