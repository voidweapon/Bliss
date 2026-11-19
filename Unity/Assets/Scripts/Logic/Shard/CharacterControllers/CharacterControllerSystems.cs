using Bliss.CharacterMovement;
using Bliss.Core;
using Bliss.Pool;
using ET;
using Unity.Mathematics;
#if DOTNET
using Bliss.Physics;
#endif

namespace Bliss.CharacterControllers
{
    [EntitySystemOf(typeof(CharacterController))]
    public static partial class CharacterControllerSystems
    {
        private static void Awake(this CharacterController self)
        {
            Entity parent = self.Parent;
            bool hadInput = parent.GetComponent<InputIntentProcessPipeline>() != null;
            bool hadMovement = parent.GetComponent<CharacterMovementComponent>() != null;
            bool hadMotor = parent.GetComponent<KinematicCharacterComponent>() != null;
            bool hadRigidbody = parent.GetComponent<GameplayRigidbody>() != null;
            try
            {
                self.initialize();
            }
            catch
            {
                // AddComponent attaches before Awake, so recover partially attached components too.
                self.InputIntentProcessPipeline ??= hadInput ? null : parent.GetComponent<InputIntentProcessPipeline>();
                self.MovementComponent ??= hadMovement ? null : parent.GetComponent<CharacterMovementComponent>();
                self.KinematicCharacterComponent ??= hadMotor ? null : parent.GetComponent<KinematicCharacterComponent>();
                self.OwnedRigidbody ??= hadRigidbody ? null : parent.GetComponent<GameplayRigidbody>();
                self.Dispose();
                throw;
            }
        }

        private static void initialize(this CharacterController self)
        {
            self.RuntimeData = ObjectPool.Instance.Fetch<CharacterRuntimeData>();

            //Input
            self.InputIntentProcessPipeline = self.Parent.AddComponent<InputIntentProcessPipeline>();
            self.InputIntentProcessPipeline.SetRuntimeData(self.RuntimeData);

            //Movement
            CapsuleColliderInitParams capsuleParams = new CapsuleColliderInitParams()
            {
                Center = new float3(0f, 1f, 0f),
                Radius = 0.5f,
                Height = 2f,
                Direction = 1,
                IsTrigger = false,
            };
#if DOTNET
            PhysicalScene physicalScene = self.Scene().GetComponent<PhysicalScene>()
                ?? throw new System.InvalidOperationException("The character's scene has no PhysicalScene.");
            var gameplayCapsuleCollider = physicalScene.CreateCharacterPhysics(self.Parent, capsuleParams);
            self.OwnedRigidbody = self.Parent.GetComponent<GameplayRigidbody>();
#else
            self.OwnedRigidbody = self.Parent.AddComponent<GameplayRigidbody, GameplayRigidbodyInitParams>(new GameplayRigidbodyInitParams()
            {
                IsStatic = false,
                IsKinematic = true,
            });
            var gameplayCapsuleCollider = self.Parent.AddChild<GameplayCapsuleCollider, CapsuleColliderInitParams>(capsuleParams);
#endif
            self.OwnedCapsule = gameplayCapsuleCollider;

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
            if (!self.Parent.IsDisposed)
            {
                self.InputIntentProcessPipeline?.Dispose();
                self.MovementComponent?.Dispose();
                self.OwnedCapsule?.Dispose();
                self.KinematicCharacterComponent?.Dispose();
                self.OwnedRigidbody?.Dispose();
            }

            if (self.RuntimeData != null)
            {
                self.RuntimeData.Dispose();
                ObjectPool.Instance.Recycle(self.RuntimeData);
                self.RuntimeData = null;
            }

            self.InitComponentCancellationToken.Cancel();
            self.InputIntentProcessPipeline = null;
            self.MovementComponent = null;
            self.KinematicCharacterComponent = null;
            self.OwnedCapsule = null;
            self.OwnedRigidbody = null;
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
        
    }
}