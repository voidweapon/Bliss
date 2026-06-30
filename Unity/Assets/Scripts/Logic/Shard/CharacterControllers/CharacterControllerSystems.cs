using Bliss.CharacterMovement;
using Bliss.Core;
using Bliss.Pool;
using ET;
using Unity.Mathematics;

namespace Bliss.CharacterControllers
{
    [EntitySystemOf(typeof(CharacterController))]
    public static partial class CharacterControllerSystems
    {
        private static void Awake(this CharacterController self)
        {
            self.RuntimeData = ObjectPool.Instance.Fetch<CharacterRuntimeData>();

            //Input
            self.InputIntentProcessPipeline = self.Parent.AddComponent<InputIntentProcessPipeline>();
            self.InputIntentProcessPipeline.SetRuntimeData(self.RuntimeData);

            //Time
            self.PreviousTimeStamp = TimeInfo.Instance.ClientFrameTime();

            //Movement
            self.Parent.AddComponent<GameplayRigidbody, GameplayRigidbodyInitParams>(new GameplayRigidbodyInitParams()
            {
                IsStatic = false,
                IsKinematic = true,
            });
            
            var gameplayCapsuleCollider = self.Parent.AddChild<GameplayCapsuleCollider, CapsuleColliderInitParams>(new CapsuleColliderInitParams()
            {
                Center = new float3(0f, 1f, 0f),
                Radius = 0.5f,
                Height = 2f,
                Direction = 1,
                IsTrigger = false,
            });

            self.KinematicCharacterComponent = self.Parent.AddComponent<KinematicCharacterComponent>();
            self.KinematicCharacterComponent.SetCharacterCapsule(gameplayCapsuleCollider);
            self.KinematicCharacterComponent.StepHandling = StepHandlingMethod.Extra;
            self.KinematicCharacterComponent.RigidbodyInteractionType = RigidbodyInteractionType.None;

            self.MovementComponent = self.Parent.AddComponent<CharacterMovementComponent>();
            self.MovementComponent.KinematicCharacterCmp = self.KinematicCharacterComponent;
            self.MovementComponent.SetRuntimeData(self.RuntimeData);
            self.KinematicCharacterComponent.CharacterMovementController = self.MovementComponent.MovementController;
            self.RuntimeData.CharacterUp = self.KinematicCharacterComponent.CharacterUp;
            
            // self.InitComponentCancellationToken = new ETCancellationToken();
            // self.InitComponentWaitResourceLoaded(self.InitComponentCancellationToken).Coroutine();
        }
        private static void Destroy(this CharacterController self)
        {
            if (self.RuntimeData != null)
            {
                self.RuntimeData.Dispose();
                ObjectPool.Instance.Recycle(self.RuntimeData);
                self.RuntimeData = null;
            }

            self.InitComponentCancellationToken.Cancel();
        }
        
        private static void EarlyUpdate(this CharacterController self)
        {
            var frameTime = TimeInfo.Instance.ClientFrameTime();
            self.CurrentDeltaTimeMS = frameTime - self.PreviousTimeStamp;
            self.CurrentDeltaTime = self.CurrentDeltaTimeMS / 1000f;
            self.PreviousTimeStamp = frameTime;
            self.ViewEarlyUpdate(self.CurrentDeltaTime);
        }

        private static void Update(this CharacterController self)
        {
            self.ViewUpdate(self.CurrentDeltaTimeMS, self.CurrentDeltaTime);
        }

        /*
        private static async ETTask InitComponentWaitResourceLoaded(this CharacterController self, ETCancellationToken cancellationToken)
        {

#if !DOTNET
            var waitResult = await self.Parent.GetComponent<ObjectWait>().Wait<Wait_AfterCreateGameObjectView>(cancellationToken);
            if (waitResult.Error != WaitTypeError.Success)
            {
                return;
            }
#endif
            var gameplayCapsuleCollider = self.Parent.AddChild<GameplayCapsuleCollider, CapsuleColliderInitParams>(new CapsuleColliderInitParams()
            {
                Center = new float3(0f, 1f, 0f),
                Radius = 0.5f,
                Height = 2f,
                Direction = 1,
                IsTrigger = false,
            });

            self.KinematicCharacterComponent = self.Parent.AddComponent<KinematicCharacterComponent>();
            self.KinematicCharacterComponent.SetCharacterCapsule(gameplayCapsuleCollider);

            self.MovementComponent = self.Parent.AddComponent<CharacterMovementComponent>();
            self.MovementComponent.KinematicCharacterCmp = self.KinematicCharacterComponent;
            self.MovementComponent.SetRuntimeData(self.RuntimeData);
            self.KinematicCharacterComponent.CharacterMovementController = self.MovementComponent.MovementController;
            self.RuntimeData.CharacterUp = self.KinematicCharacterComponent.CharacterUp;

            await ETTask.CompletedTask;
        }
        */
        
        public static void LogicUpdate(this CharacterController self, float deltaTime)
        {

        }
        
        private static void ViewEarlyUpdate(this CharacterController self, float deltaTime)
        {
            if (self.InputIntentProcessPipeline != null)
            {
                self.InputIntentProcessPipeline.ViewEarlyUpdate(deltaTime);
            }
        }

        private static void ViewUpdate(this CharacterController self, long deltaTimeMS, float deltaTime)
        {
            if (self.MovementComponent != null)
            {
                self.MovementComponent.LogicUpdate(deltaTimeMS, deltaTime);
            }
        }

    }
}