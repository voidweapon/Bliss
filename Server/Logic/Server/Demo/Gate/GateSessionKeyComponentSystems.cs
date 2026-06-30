using Bliss.Timer;
using ET;

namespace Bliss;

public static partial class GateSessionKeyComponentSystems
{
    /// <summary>
    /// 添加一个SessionToken,并且在20秒后自动删除这个Token
    /// </summary>
    /// <param name="self"></param>
    /// <param name="account"></param>
    /// <returns></returns>
    public static long Add(this GateSessionKeyComponent self, string account)
    {
        long token = RandomGenerator.RandInt64();
        self.SessionTokens.Add(token, account);
        
        self.TimeoutRemoveKey(token).Coroutine();
        
        return token;
    }
    
    /// <summary>
    /// 根据token获取账号
    /// </summary>
    /// <param name="self"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public static string Get(this GateSessionKeyComponent self, long token)
    {
        self.SessionTokens.TryGetValue(token, out var account);
        return account;
    }

    public static void Remove(this GateSessionKeyComponent self, long token)
    {
        self.SessionTokens.Remove(token);
    }
    
    private static async ETTask TimeoutRemoveKey(this GateSessionKeyComponent self, long token)
    {
        await self.RootScene().GetComponent<TimerComponent>().WaitDurationAsync(20000);
        self.SessionTokens.Remove(token);
    }
}