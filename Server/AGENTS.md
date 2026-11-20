# Instructions for AI Agents

## 仓库结构
### 1. Base 程序集
 - 路径: 'Base':
 - 实际源码来源: `Base/Base.csproj` 通过 `../../Unity/Assets/Scripts/Base/**/*.cs` 链入
 - 用途: 框架的基础支持库
 - 内容: 数学库, 网络库，基础数据结构等
 - 特点: 不可热更新，不可热重载，稳定性高，基础支持代码

### 2. Core 程序集 
 - 路径: 'Core':
 - 实际源码来源: `Core/Core.csproj` 通过 `../../Unity/Assets/Scripts/Core/**/*.cs` 链入
 - 用途: 框架核心组件
 - 内容: 基础实体定义，事件系统等无关具体业务逻辑的核心组件
 - 特点: 不可热更新，不可热重载，稳定性高

### 3. Model 程序集 
 - 路径: 'Model': 
 - 实际源码来源: `Model/Model.csproj` 通过 `../../Unity/Assets/Scripts/Model/Shard/**/*.cs` 链入
 - 用途: 服务器和客户端公用的模型层
 - 内容: 实体定义, 配置数据, 共享逻辑
 - 特点: 可热更新，不可热重载，稳定性高

### 4. Logic 程序集 
 - 路径: 'Logic':
 - 实际源码来源: `Logic/Logic.csproj` 通过 `../../Unity/Assets/Scripts/Logic/Shard/**/*.cs` 链入
 - 用途: 服务器和客户端公用的逻辑层
 - 内容: 业务逻辑实现
 - 特点: 可热更新，可热重载，高频变化，核心业务逻辑
 - 运行方式: 由 `App/CodeLoader.cs` 在运行时加载 `./Logic.dll` 与 `./Logic.pdb`（`App/App.csproj` 不直接引用 `Logic`）

### 5. ThirdParty 程序集
 - 路径: 'ThirdParty':
 - 用途: 服务器第三方插件
 - 内容: 第三方源码（ETTask/KCP/Unity.Mathematics）、NuGet 依赖与跨平台 native runtime
 - 特点: 不可热重载，稳定性高

### 6. App 程序集
 - 路径: 'App':
 - 用途: 服务器入口
 - 内容: 服务器
 - 集成: 使用 `Configs/NLog/NLog.config` 初始化 NLog（见 `App/NLogger.cs`）
 - 特点: 不可热更新，不可热重载，稳定性高

 - 构建约束: `Core`/`Model`/`Logic` 引用 `Bliss.ProjectRule` Analyzer，提交前需确保分析器规则通过



## 共享编码规范

Server 与 Unity 的新增、修改及审查代码统一遵循仓库根目录 `CODING_STANDARDS.md`，包括命名例外、注释、容器字段以及 Entity、System 与回调约定。该文档是两端共用规则的唯一来源；本文件保留 Server 程序集结构、构建约束和工作流说明。

# 每次做出决定时请务必检查代码是否符合上述规范，确保代码的可读性和维护性,执行完毕后再次检测查代码是否符合上述规范
# 做Plane是显示将要提交或修改的代码