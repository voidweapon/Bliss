using System;
using System.Collections.Generic;
using Bliss.Core;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Bliss.Debugger.GameDebugStats
{
    public class GameDebugStates : Singleton<GameDebugStates>, ISingletonInit
    {
        private readonly Dictionary<string, DebugStatGroup> _debugStatGroups = new Dictionary<string, DebugStatGroup>();
        private readonly Dictionary<int, string> _groupNameStableHash = new Dictionary<int, string>();
        
        public void Init()
        {


        }
        
        protected override void Destroy()
        {
            foreach (var kv in _debugStatGroups)
            {
                kv.Value?.Dispose();
            }
            _debugStatGroups.Clear();
        }

        public void SetDebugStat<TValue>(string groupName, 
            string propertyName,
            TValue value,
            float4 color,
            bool highlightWhenChange = false)
        {
            if (!_debugStatGroups.TryGetValue(groupName, out var group))
            {
                group = new DebugStatGroup(groupName);
                _debugStatGroups.Add(groupName, group);
                _groupNameStableHash.Add(groupName.GetStableHashCode(), groupName);
            }
            group.SetDebugStat(propertyName, value, color, highlightWhenChange);
        }
        
        public void AddDebugStat<TObj, TValue>(string groupName, 
            TObj obj, string propertyName, 
            float4 color,
            bool highlightWhenChange = false)
        {
            if (!_debugStatGroups.TryGetValue(groupName, out var group))
            {
                group = new DebugStatGroup(groupName);
                _debugStatGroups.Add(groupName, group);
                _groupNameStableHash.Add(groupName.GetStableHashCode(), groupName);
            }
            group.AddWeakDebugStat<TObj, TValue>(obj, propertyName,  color, highlightWhenChange);
        }
        
        public void AddStaticDebugStat<TValue>(string groupName, Type staticType, string propertyName,
            float4 color,
            bool highlightWhenChange = false)
        {
            if (!_debugStatGroups.TryGetValue(groupName, out var group))
            {
                group = new DebugStatGroup(groupName);
                _debugStatGroups.Add(groupName, group);
                _groupNameStableHash.Add(groupName.GetStableHashCode(), groupName);
            }
            group.AddStaticDebugStat<TValue>(staticType, propertyName,  color, highlightWhenChange);
        }

        public void RemoveDebugStat<TObj>(string groupName,
            TObj obj, string propertyName)
        {
            if (_debugStatGroups.TryGetValue(groupName, out var group))
            {
                group.RemoveWeakDebugStat<TObj>(obj, propertyName);
            }
        }
        
        public void RemoveStaticDebugStat(string groupName, Type staticType, string propertyName)
        {
            if (_debugStatGroups.TryGetValue(groupName, out var group))
            {
                group.RemoveStaticDebugStat(staticType, propertyName);
            }
        }

        public void Draw(Action<int, DebugStatGroup> drawFunc)
        {
            foreach (var kv in _groupNameStableHash)
            {
                var windowId = kv.Key;
                var groupName = kv.Value;
                drawFunc(windowId, GetGroupByName(groupName));
            }
        }

        public DebugStatGroup GetGroupById(int groupId)
        {
            return _debugStatGroups[_groupNameStableHash[groupId]];
        }
        
        public DebugStatGroup GetGroupByName(string groupName)
        {
            return _debugStatGroups[groupName];
        }

        public string GetGroupNameById(int groupId)
        {
            return _groupNameStableHash[groupId];
        }
        
    }
}