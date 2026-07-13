using Bliss.CharacterMovement;
using ET;

namespace Bliss;

static class MessageHandler_C2M_CharacterMoveData
{
    public static async ETTask HandleMessage(Unit entity, C2M_CharacterMoveData message)
    {
        var characterMover =  entity.GetComponent<CharacterMovementComponent>();
        if (characterMover == null)
        {
            return;
        }
        
        characterMover.ServerMove_ServerReceive(message);
        
        await ETTask.CompletedTask;
    }
    
    [MessageHandler(ELogicSceneType.GameplayWorld)]
    public class MessageHandler_C2M_CharacterMoveData_Gameworld : LocationMessageHandler<Unit, C2M_CharacterMoveData>
    {
        protected override async ETTask Run(Unit entity, C2M_CharacterMoveData message)
        {
            await HandleMessage(entity, message);
        }
    }

    [MessageHandler(ELogicSceneType.Level)]
    public class MessageHandler_C2M_CharacterMoveData_Level : LocationMessageHandler<Unit, C2M_CharacterMoveData>
    {
        protected override async ETTask Run(Unit entity, C2M_CharacterMoveData message)
        {
            await HandleMessage(entity, message);
        }
    }
}