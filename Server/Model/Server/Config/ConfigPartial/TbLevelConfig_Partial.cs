using System.Collections.Generic;

namespace Bliss;

public partial class TbLevelConfig
{
    public Dictionary<int, List<LevelConfig>> DicSceneId2Level;

    public void EndInit()
    {
        DicSceneId2Level = new Dictionary<int, List<LevelConfig>>();
        foreach (var item in DataList)
        {
            if (!DicSceneId2Level.TryGetValue(item.SceneId, out List<LevelConfig> list))
            {
                list = new List<LevelConfig>();
                DicSceneId2Level.Add(item.SceneId, list);
            }
            list.Add(item);
        }
    }
}