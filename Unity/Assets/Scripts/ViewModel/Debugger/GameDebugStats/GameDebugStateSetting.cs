using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Bliss.ViewModel.Debugger.GameDebugStats
{
    [CreateAssetMenu(fileName = "GameDebugStateSettings", menuName = "Bliss/ViewModel/GameDebugStateSettings")]
    public class GameDebugStateSetting : ScriptableObject
    {
        public List<GameDebugStateGroupSetting> GroupSettings = new List<GameDebugStateGroupSetting>();

        [System.NonSerialized]
        public Dictionary<string, GameDebugStateGroupSetting> Settings = new Dictionary<string, GameDebugStateGroupSetting>();

        public void Init()
        {
            Settings.Clear();
            foreach (var groupSetting in GroupSettings)
            {
                Settings.Add(groupSetting.GroupName, groupSetting);
            }
        }

        public bool TryGetSetting(string groupName, out GameDebugStateGroupSetting setting)
        {
            return Settings.TryGetValue(groupName, out setting);
        }

        public void AddSetting(string groupName, GameDebugStateGroupSetting setting)
        {
            if (Settings.ContainsKey(groupName))
            {
                return;
            }
            Settings[groupName] = setting;
            GroupSettings.Add(setting);
        }
    }

    [System.Serializable]
    public class GameDebugStateGroupSetting
    {
        public string GroupName;
        public Rect GroupRect;
        public bool IsFold;
    }
}