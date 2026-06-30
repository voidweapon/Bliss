using Bliss.Base.Collections;
using UnityEngine;

namespace Bliss
{
    [SingletonManger]
    public class ViewEntityMapManager : Singleton<ViewEntityMapManager>, ISingletonInit
    {
        private DoublyDictionary<Entity, GameObject> _dicEntity2Go  =  new DoublyDictionary<Entity, GameObject>();

        private DoublyDictionary<GameplayCollider, Collider> _dicGameplayCollider2Collider = new DoublyDictionary<GameplayCollider, Collider>();

        public void Init()
        {
            
        }
        
        public void Map(Entity entity, GameObject go)
        {
            _dicEntity2Go.Add(entity, go);
        }

        public void UnMap(Entity entity)
        {
            _dicEntity2Go.RemoveByKey(entity);
        }
        
        public void UnMap(GameObject go)
        {
            _dicEntity2Go.RemoveByValue(go);
        }

        public Entity GetEntity(GameObject go)
        {
            return _dicEntity2Go.GetKeyByValue(go);
        }
        
        public GameObject GetGameObject(Entity entity)
        {
            return _dicEntity2Go.GetValueByKey(entity);
        }


        public void MapCollider(GameplayCollider gameplayCollider, Collider collider)
        {
            _dicGameplayCollider2Collider.Add(gameplayCollider, collider);
        }

        public void UnMapCollider(GameplayCollider gameplayCollider)
        {
            _dicEntity2Go.RemoveByKey(gameplayCollider);
        }
        
        public Collider GetCollider(GameplayCollider gameplayCollider)
        {
            return _dicGameplayCollider2Collider.GetValueByKey(gameplayCollider);
        }
        
        public GameplayCollider GetGameplayCollider(Collider collider)
        {
            var test =  _dicGameplayCollider2Collider.GetKeyByValue(collider);
            if (test == null)
            {
                Log.Error($"collider {collider} position={collider.transform.position} layer={collider.gameObject.layer} not map to system");
            }
            return _dicGameplayCollider2Collider.GetKeyByValue(collider);
        }

    }
    
}