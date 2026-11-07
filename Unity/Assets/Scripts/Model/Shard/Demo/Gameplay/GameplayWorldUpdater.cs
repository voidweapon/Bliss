using System;
using System.Collections.Generic;

namespace Bliss
{
    /// <summary>
    /// Entity 可以自己决定自己在Update里的执行顺序,通过预设的UpdateGroup将Update分为EarlyUpdate, MiddleUpdate, LateUpdate三类,每一类的UpdateGroup可以通过Priority来决定执行顺序,Priority越小越先执行
    /// </summary>
    public class GameplayWorldUpdater : BObject
    { 
        private Dictionary<long, EntityRef<Entity>> _entities = new Dictionary<long, EntityRef<Entity>>();
        private static readonly ESystemUpdateGroup[] _builtInUpdateGroups =
        {
            ESystemUpdateGroup.EarlyUpdate,
            ESystemUpdateGroup.Update,
            ESystemUpdateGroup.LateUpdate,
        };
        private List<UpdateGroup> _updateSystemGroups = new List<UpdateGroup>(_builtInUpdateGroups.Length);
        private static Dictionary<int, (Type, Type)> _updateSystemGroupTypes = new Dictionary<int, (Type, Type)>()
        {
            
            { (int)ESystemUpdateGroup.EarlyUpdate, (typeof(IEarlyUpdate), typeof(IEarlyUpdateSystem)) },
            { (int)ESystemUpdateGroup.Update, (typeof(IUpdate), typeof(IUpdateSystem)) },
            { (int)ESystemUpdateGroup.LateUpdate, (typeof(ILateUpdate), typeof(ILateUpdateSystem)) },
        };

        public GameplayWorldUpdater()
        {
            for (int i = 0; i < _builtInUpdateGroups.Length; i++)
            {
                _updateSystemGroups.Add(new UpdateGroup());
            }
        }

       public void Update()
       {
           for (int i = 0; i < _builtInUpdateGroups.Length; i++)
           {
               _updateSystemGroups[i].Update(_builtInUpdateGroups[i]);
           }
       }

       public void Add(Entity entity)
       {
           _entities.Add(entity.InstanceId, entity);
           var entityType = entity.GetType();
           for (int i = 0; i < _builtInUpdateGroups.Length; i++)
           {
               if (EntitySystemManager.Instance.IsEntityUpdateInGroup(entityType, _builtInUpdateGroups[i]))
               {
                   int priority = EntitySystemManager.Instance.GetEntityUpdateGroupPriority(entityType, _builtInUpdateGroups[i]);
                   _updateSystemGroups[i].Add(entity, priority);
               }
           }
       }
       
       private class UpdateGroup
       {
           private List<EntityGroup> _updateGroups = new List<EntityGroup>();
           private List<EntityGroup> _pendingAddGroups = new List<EntityGroup>();
           private Dictionary<Type, EntityGroup> _dicType2Groups = new Dictionary<Type, EntityGroup>();

           public void Add(EntityRef<Entity> entityRef, int priority)
           {
               Entity entityObj = (Entity)entityRef;
               Type entityType = entityObj.GetType();
               if (!_dicType2Groups.TryGetValue(entityType, out EntityGroup updateGroup))
               {
                   updateGroup = new EntityGroup(priority);
                   _dicType2Groups.Add(entityType, updateGroup);
                   _pendingAddGroups.Add(updateGroup);
               }
               updateGroup.Add(entityRef);
           }

           public void Update(ESystemUpdateGroup updateGroup)
           {
               int thisFrameCount = _updateGroups.Count;
               for (int i = 0; i < thisFrameCount; i++)
               {
                   _updateGroups[i].Update(updateGroup);
               }
               
               int thisFramePendingCount = _pendingAddGroups.Count;
               for (int i = 0; i < thisFramePendingCount; i++)
               {
                   var addGroup = _pendingAddGroups[i];
                   
                   int insertIndex = _updateGroups.Count;
                   for (int j = 0; j < _updateGroups.Count; j++)
                   {
                       if (_updateGroups[j].Priority > addGroup.Priority)
                       {
                           insertIndex = j;
                           break;
                       }
                   }
                   _updateGroups.Insert(insertIndex, addGroup);
               }
           }
       }
       
       private class EntityGroup
       {
           public EntityGroup(int priority)
           {
               Priority = priority;
           }
           
           public int Priority { get; }
           private Queue<EntityRef<Entity>> _updateQueue = new Queue<EntityRef<Entity>>();

           public void Add(EntityRef<Entity> entityRef)
           {
               _updateQueue.Enqueue(entityRef);
           }

           public void Update(ESystemUpdateGroup updateGroup) 
           {
               var typePair = _updateSystemGroupTypes[(int)updateGroup];
               Type entityType = typePair.Item1;
               Type systemType = typePair.Item2;
               
               int thisFrameCount = _updateQueue.Count;
               while (thisFrameCount > 0)
               {
                   var entity = (Entity)_updateQueue.Dequeue();
                   thisFrameCount--;
                
                   if (entity == null || entity.IsDisposed)
                   {
                       continue;
                   }

                   if (entityType.IsAssignableFrom(entity.GetType()) == false)
                   {
                       continue;
                   }

                   try
                   {
                       var updateSystems = EntitySystemManager.Instance.GetEntitySystems(entity.GetType(), systemType);
                       if (updateSystems == null)
                       {
                           continue;
                       }
                       _updateQueue.Enqueue(entity);

                       foreach (var system in updateSystems)
                       {
                           var updateSystem = (IUpdateGroupSystem)system;
                           if (updateSystem == null)
                           {
                               continue;
                           }
                           updateSystem.Run(entity);
                       }
                   }
                   catch (Exception e)
                   {
                       throw new Exception($"entity system ${updateGroup} fail: {entity.GetType().FullName}", e);
                   }
               }
           }
       }
    }
}