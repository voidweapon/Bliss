using System;

namespace Bliss;

public static class PlayerManagerComponentSystems
{
    
    public static Player Add(this PlayerManagerComponent self, string account)
    {
        if (self.dictionary.ContainsKey(account))
        {
            throw new Exception($"玩家已经存在: {account}");
        }
        Player player = self.AddChild<Player, string>(account);
        self.dictionary.Add(account, player);
        return player;
    }

    public static void Remove(this PlayerManagerComponent self, Player player)
    {
        if (player.Account != null)
        {
            self.dictionary.Remove(player.Account);
        }
        else
        {
            System.Diagnostics.Debugger.Break();
        }
        player.Dispose();
    }

    /// <summary>
    /// 根据账号查找玩家
    /// </summary>
    /// <param name="self"></param>
    /// <param name="account"></param>
    /// <returns></returns>
    public static Player Find(this PlayerManagerComponent self, string account)
    {
        self.dictionary.TryGetValue(account, out EntityRef<Player> playerRef);
        return (Player)playerRef;
    }
    
}