using Bliss.Core;
using Unity.Mathematics;

namespace Bliss
{
    [EntitySystemOf(typeof(TransformComponent))]
    public  static partial class TransformComponentSystems
    {
        public static void SetPosition(this TransformComponent self, float3 position, bool raiseEvent = true)
        {
            if (self.Transform.Location.Equals(position))
            {
                return;
            }
            self.Transform.SetTranslation(position);

            if (raiseEvent)
            {
                EventSystem.Instance.Push(self.Scene<LogicScene>(), new Event_TransformChange()
                {
                    Entity = self.Parent,
                    Flags = ETransformChangeFlag.Position,
                    Position = position,
                });
            }
            
            if (self.ViewSyncBarrier.Count == 0)
            {
                EventSystem.Instance.Push(self.Scene<LogicScene>(), new Event_SyncViewTransform()
                {
                    Entity = self.Parent,
                    Flags = ETransformChangeFlag.Position,
                    Position = self.Transform.TransformPosition(self.ViewRelativeTransform.Location),
                });
            }
        }

        // public static void SetRotation(this TransformComponent self, quaternion rotation)
        // {
        //     if (self.Transform.Rotation.Equals(rotation))
        //     {
        //         return;
        //     }
        //     
        //     self.Transform.SetRotation(rotation);
        //
        //     //if (raiseEvent)
        //     {
        //         EventSystem.Instance.Push(self.Scene<LogicScene>(), new Event_TransformChange()
        //         {
        //             Entity = self.Parent,
        //             Flags = ETransformChangeFlag.Rotation,
        //             Rotation = rotation,
        //         });
        //     }
        //     
        //     if (self.ViewSyncBarrier.Count == 0)
        //     {
        //         EventSystem.Instance.Push(self.Scene<LogicScene>(), new Event_SyncViewTransform()
        //         {
        //             Entity = self.Parent,
        //             Flags = ETransformChangeFlag.Rotation,
        //             Rotation = self.Transform.TransformRotation(self.ViewRelativeTransform.Rotation),
        //         });
        //     }
        // }
        //
        public static void SetRotation(this TransformComponent self, quaternion rotation, bool additionalSyncViewPosition = false, bool raiseEvent = true)
        {
            if (self.Transform.Rotation.Equals(rotation))
            {
                return;
            }
            
            self.Transform.SetRotation(rotation);


            if (raiseEvent)
            {
                EventSystem.Instance.Push(self.Scene<LogicScene>(), new Event_TransformChange()
                {
                    Entity = self.Parent,
                    Flags = ETransformChangeFlag.Rotation,
                    Rotation = rotation,
                });
            }
            
            if (self.ViewSyncBarrier.Count == 0)
            {
                var flag = additionalSyncViewPosition ? (ETransformChangeFlag.Position | ETransformChangeFlag.Rotation ) : ETransformChangeFlag.Rotation;
                EventSystem.Instance.Push(self.Scene<LogicScene>(), new Event_SyncViewTransform()
                {
                    Entity = self.Parent,
                    Flags = flag,
                    Position = self.Transform.TransformPosition(self.ViewRelativeTransform.Location),
                    Rotation = self.Transform.TransformRotation(self.ViewRelativeTransform.Rotation),
                });
            }
        }

        public static void SetPositionAndRotation(this TransformComponent self, float3 position, quaternion rotation, bool raiseEvent = true)
        {
            bool isSamePosition = self.Transform.Location.Equals(position);
            bool isSameRotation = self.Transform.Rotation.Equals(rotation);
            if (isSamePosition && isSameRotation)
            {
                return;
            }
            
            self.Transform.SetTranslationAndRotation(position, rotation);

            var transformChangeEvent = new Event_TransformChange();
            var viewSyncEvent = new Event_SyncViewTransform();

            transformChangeEvent.Entity = self.Parent;
            viewSyncEvent.Entity = self.Parent;

            if (!isSamePosition)
            {
                transformChangeEvent.Position = position;
                viewSyncEvent.Position = self.Transform.TransformPosition(self.ViewRelativeTransform.Location);
                transformChangeEvent.Flags |= ETransformChangeFlag.Position;
                viewSyncEvent.Flags |= ETransformChangeFlag.Position;
            }
            
            if (!isSameRotation)
            {
                transformChangeEvent.Rotation = rotation;
                viewSyncEvent.Rotation = self.Transform.TransformRotation(self.ViewRelativeTransform.Rotation);
                transformChangeEvent.Flags |= ETransformChangeFlag.Rotation;
                viewSyncEvent.Flags |= ETransformChangeFlag.Rotation;
            }

            if (raiseEvent)
            {
                EventSystem.Instance.Push(self.Scene<LogicScene>(), transformChangeEvent);
            }
            
            if (self.ViewSyncBarrier.Count == 0)
            {
                EventSystem.Instance.Push(self.Scene<LogicScene>(), viewSyncEvent);
            }
        }

        public static void SynViewTranslation(this TransformComponent self,
            float3 position)
        {
            self.ViewRelativeTransform.SetTranslation(self.Transform.InverseTransformPosition(position));
            
            var viewSyncEvent = new Event_SyncViewTransform();
            viewSyncEvent.Entity = self.Parent;
            viewSyncEvent.Position = position;
            viewSyncEvent.Flags |= ETransformChangeFlag.Position;
            EventSystem.Instance.Push(self.Scene<LogicScene>(), viewSyncEvent);
        }
        
        public static void SynViewRotation(this TransformComponent self,
            quaternion rotation)
        {
            self.ViewRelativeTransform.SetRotation(self.Transform.InverseTransformRotation(rotation));
            
            var viewSyncEvent = new Event_SyncViewTransform();
            viewSyncEvent.Entity = self.Parent;
            viewSyncEvent.Rotation = rotation;
            viewSyncEvent.Flags |= ETransformChangeFlag.Rotation;
            EventSystem.Instance.Push(self.Scene<LogicScene>(), viewSyncEvent);
        }
        
        public static void SynViewTranslationAndRotation(this TransformComponent self,
            float3 position,
            quaternion rotation)
        {
            self.ViewRelativeTransform.SetTranslation(self.Transform.InverseTransformPosition(position));
            self.ViewRelativeTransform.SetRotation(self.Transform.InverseTransformRotation(rotation));
            
            var viewSyncEvent = new Event_SyncViewTransform();
            viewSyncEvent.Entity = self.Parent;
            viewSyncEvent.Position = position;
            viewSyncEvent.Rotation = rotation;
            viewSyncEvent.Flags |= ETransformChangeFlag.Position;
            viewSyncEvent.Flags |= ETransformChangeFlag.Rotation;
            EventSystem.Instance.Push(self.Scene<LogicScene>(), viewSyncEvent);
        }
        
        public static void SynViewRelativeTranslation(this TransformComponent self,
            float3 relativePosition)
        {
            self.ViewRelativeTransform.SetTranslation(relativePosition);
            
            var viewSyncEvent = new Event_SyncViewTransform();
            viewSyncEvent.Entity = self.Parent;
            viewSyncEvent.Position = self.Transform.TransformPosition(self.ViewRelativeTransform.Location);
            viewSyncEvent.Flags |= ETransformChangeFlag.Position;
            EventSystem.Instance.Push(self.Scene<LogicScene>(), viewSyncEvent);
        }
        
        public static void SynViewRelativeRotation(this TransformComponent self,
            quaternion relativeRotation)
        {
            self.ViewRelativeTransform.SetRotation(relativeRotation);
            
            var viewSyncEvent = new Event_SyncViewTransform();
            viewSyncEvent.Entity = self.Parent;
            viewSyncEvent.Rotation = self.Transform.TransformRotation(self.ViewRelativeTransform.Rotation);
            viewSyncEvent.Flags |= ETransformChangeFlag.Rotation;
            EventSystem.Instance.Push(self.Scene<LogicScene>(), viewSyncEvent);
        }
        
        public static void SynViewRelativeTranslationAndRotation(this TransformComponent self,
            float3 relativePosition,
            quaternion relativeRotation)
        {
            self.ViewRelativeTransform.SetTranslation(relativePosition);
            self.ViewRelativeTransform.SetRotation(relativeRotation);
            
            var viewSyncEvent = new Event_SyncViewTransform();
            viewSyncEvent.Entity = self.Parent;
            viewSyncEvent.Position = self.Transform.TransformPosition(self.ViewRelativeTransform.Location);
            viewSyncEvent.Rotation = self.Transform.TransformRotation(self.ViewRelativeTransform.Rotation);
            viewSyncEvent.Flags |= ETransformChangeFlag.Position;
            viewSyncEvent.Flags |= ETransformChangeFlag.Rotation;
            EventSystem.Instance.Push(self.Scene<LogicScene>(), viewSyncEvent);
        }

        public static void AddViewSyncBarrier(this TransformComponent self, object entity)
        {
            self.ViewSyncBarrier.TryAdd(entity, 0);

            self.ViewSyncBarrier[entity]++;
        }

        public static void RemoveViewSyncBarrier(this TransformComponent self, object entity)
        {
            if (self.ViewSyncBarrier.TryGetValue(entity, out var barrierCount))
            {
                barrierCount--;
                self.ViewSyncBarrier[entity] = barrierCount;

                if (barrierCount == 0)
                {
                    self.ViewSyncBarrier.Remove(entity);
                }
            }
        }
    }
}