using System;
using Bliss.CharacterMovement;
using Bliss.Core;
using Bliss.Physics;
using Unity.Mathematics;

namespace Bliss
{
    [EntitySystemOf(typeof(MovablePlatform))]
    public static partial class MovablePlatformSystems
    {
        private static void Destroy(this MovablePlatform self)
        {
            
        }
        
        private static void AfterSceneInit(this MovablePlatform self)
        {
            var physicalData = self.SceneActorCmpConfig.PhysicalData;
            if(physicalData != null)
            {
                var physicalScene = self.Scene().GetComponent<PhysicalScene>();
                physicalScene.CreatePhysicalComponent(self.Parent, physicalData);
            }
            
            var mover = self.Parent.AddComponent<KinematicRigidbodyMover>();
            var moverController = new MovablePlatformMoverController();
            moverController.SetMovablePlatform(self);
            mover.MoverController = moverController;

            self.MoveForward = !self.SceneActorCmpConfig.InitialDirection;
            if (!self.SceneActorCmpConfig.InitialDirection)
            {
                //forward
                self.MoveDirection = math.normalize(self.SceneActorCmpConfig.Point2 - self.SceneActorCmpConfig.Point1);
            }
            else
            {
                //backward
                self.MoveDirection = math.normalize(self.SceneActorCmpConfig.Point1 - self.SceneActorCmpConfig.Point2);
            }
        }
        
        public class MovablePlatformMoverController : IMoverController
        {
            private MovablePlatform _platform;
            private TransformComponent _transformComponent;

            public void SetMovablePlatform(MovablePlatform platform)
            {
                _platform = platform;
                _transformComponent = _platform.Parent.GetComponent<TransformComponent>();
                var mover = platform.Parent.GetComponent<KinematicRigidbodyMover>();
                
                mover.TransientPosition = _transformComponent.Transform.Location;
                mover.TransientRotation = _transformComponent.Transform.Rotation;
            }
            
            public void UpdateMovement(out float3 goalPosition, out quaternion goalRotation, float deltaTime)
            {
                var config = _platform.SceneActorCmpConfig;
                
                float remainTime = deltaTime;
                float moveDistance = remainTime * config.Speed;
                float remainDistance = 0f;
                float3 endPoint = config.Point2;
                if (!_platform.MoveForward)
                {
                    endPoint = config.Point1;
                }
                remainDistance = math.length(_transformComponent.Transform.Location - endPoint);
                
                if (remainDistance > moveDistance)
                {
                    goalPosition = _transformComponent.Transform.Location + (moveDistance * _platform.MoveDirection);
                }
                else
                {
                    goalPosition = endPoint;
                    remainTime -= remainDistance / config.Speed;
                    _platform.MoveDirection = -_platform.MoveDirection;
                    _platform.MoveForward = !_platform.MoveForward;
                    goalPosition += (remainTime * config.Speed) * _platform.MoveDirection;
                }
                
                goalRotation = _transformComponent.Transform.Rotation;
            }
            
        }
    }
}