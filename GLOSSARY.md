# Bliss 领域词汇

## Language

**GameplayWorld**：
拥有游戏实体和游戏更新过程的世界；一个世界可以拥有多个 LevelScene。

**LevelScene**：
属于 GameplayWorld 的关卡场景，承载该关卡的实体和初始化资源。

**LevelScene 树**：
由根 LevelScene 及其嵌套 LevelScene 组成的层级集合。

**LevelScene 初始化**：
准备 LevelScene 及其必要资源的过程；成功不代表场景已经启用。

**LevelScene 启用**：
在初始化成功后，使 LevelScene 及其实体、组件和相关运行能力投入使用的过程。

**临时 LevelScene**：
已开始创建、但尚未完成启用的 LevelScene。

**客户端协议模拟测试**：
在不连接实际 Server 的情况下，验证 Unity 客户端与提供可控协议响应的测试端点之间交互的测试。

**客户端与 Server 真实联调测试**：
使用实际 Unity 客户端连接实际 Server，验证跨客户端与服务端网络业务流程的测试。
