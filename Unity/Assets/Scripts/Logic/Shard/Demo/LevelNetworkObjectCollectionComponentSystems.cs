using Bliss.Core;

namespace Bliss
{
    [EntitySystemOf(typeof(LevelNetworkObjectCollectionComponent))]
    public static partial class LevelNetworkObjectCollectionComponentSystems
    {
        private static void Awake(this LevelNetworkObjectCollectionComponent self)
        {
            self.NetworkObjects.Clear();
        }
        
        private static void Destroy(this LevelNetworkObjectCollectionComponent self)
        {
            self.NetworkObjects.Clear();
        }


        public static void AddNetworkObject(this LevelNetworkObjectCollectionComponent self, Entity networkObject)
        {
            self.NetworkObjects[networkObject.Id] = networkObject;
        }
        
        public static void RemoveNetworkObject(this LevelNetworkObjectCollectionComponent self, Entity networkObject)
        {
            self.NetworkObjects.Remove(networkObject.Id);
        }
        
        public static void DispatchMessage(this LevelNetworkObjectCollectionComponent self, IActorMessage actorMessage)
        {
            if (actorMessage is not MessageObject messageObject)
            {
                return;
            }
            
            if (self.NetworkObjects.TryGetValue(actorMessage.ActorId, out var networkObject))
            {
                var mailBox = networkObject.GetComponent<MailBoxComponent>();
                mailBox.Delivery(self.Root().RootAddress ,messageObject);
            }
        }
    }
}