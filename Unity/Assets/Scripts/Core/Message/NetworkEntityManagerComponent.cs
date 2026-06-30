using System;
using System.Collections.Generic;

namespace Bliss
{
    /// <summary>
    /// 场景树内网络实体管理组件，负责管理网络实体的邮箱地址
    /// 外部消息进来,通过这个组件定位到网络实体
    /// </summary>
    public class NetworkEntityManagerComponent : Entity
    {
        private readonly Dictionary<long, EntityRef<MailBoxComponent>> _entityInstanceId2MailBoxes = new Dictionary<long, EntityRef<MailBoxComponent>>();
        private readonly Dictionary<long, EntityRef<Entity>> _entityId2Entities = new Dictionary<long, EntityRef<Entity>>();

        /// <summary>
        /// 添加网络实体的邮箱地址
        /// </summary>
        /// <param name="entityInstanceId"></param>
        /// <param name="mailBox"></param>
        public void AddEntityMailBox(long entityInstanceId, EntityRef<MailBoxComponent> mailBox)
        {
            _entityInstanceId2MailBoxes.Add(entityInstanceId, mailBox);
        }

        /// <summary>
        /// 移除网络实体的邮箱地址
        /// </summary>
        /// <param name="entityInstanceId"></param>
        public void RemoveEntityMailBox(long entityInstanceId)
        {
            _entityInstanceId2MailBoxes.Remove(entityInstanceId);
        }

        public MailBoxComponent GetEntityMailBox(long entityInstanceId)
        {
            if (_entityInstanceId2MailBoxes.TryGetValue(entityInstanceId, out var mailBox))
            {
                return (MailBoxComponent)mailBox;
            }

            return null;
        }
        
        
        /// <summary>
        /// 添加网络实体的邮箱地址
        /// </summary>
        /// <param name="entityId"></param>
        /// <param name="entityRef"></param>
        public void AddEntity(long entityId, EntityRef<Entity> entityRef)
        {
            if (_entityId2Entities.ContainsKey(entityId))
            {
                Log.Error(new ArgumentException($"{entityId} has already been added.  {((Entity)_entityId2Entities[entityId]).GetType().FullName}  --  {((Entity)entityRef).GetType().FullName}"));
            }
            _entityId2Entities.Add(entityId, entityRef);
        }

        /// <summary>
        /// 移除网络实体的邮箱地址
        /// </summary>
        /// <param name="entityId"></param>
        public void RemoveEntity(long entityId)
        {
            _entityId2Entities.Remove(entityId);
        }

        public EntityRef<Entity> GetEntity(long entityId)
        {
            return _entityId2Entities.GetValueOrDefault(entityId);
        }
    }
}