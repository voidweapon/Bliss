using System.Collections.Generic;

namespace Bliss;

public partial class TbServerScene
{
    public ServerScene LocationScene { get; private set; }

    public Dictionary<int, List<ServerScene>> DicZone2Gates { get; private set; }
    public Dictionary<int, ServerScene> DicId2Maps { get; private set; }
    public void EndInit()
    {
        foreach (var sceneCfg in DataList)
        {
            sceneCfg.EndInit();
        }
        
        DicZone2Gates = new Dictionary<int, List<ServerScene>>();
        DicId2Maps = new Dictionary<int, ServerScene>();
        
        foreach (var sceneCfg in DataList)
        {
            if (sceneCfg.LogicSceneType == ELogicSceneType.Location)
            {
                LocationScene = sceneCfg;
            }
            else if (sceneCfg.LogicSceneType == ELogicSceneType.Gate)
            {
                if (!DicZone2Gates.TryGetValue(sceneCfg.Zone, out List<ServerScene> gates))
                {
                    gates = new List<ServerScene>();
                    DicZone2Gates.Add(sceneCfg.Zone, gates);
                }
                gates.Add(sceneCfg);
            }
            else if (sceneCfg.LogicSceneType == ELogicSceneType.Map)
            {
                DicId2Maps.Add(sceneCfg.Id, sceneCfg);
            }
        }
    }
}