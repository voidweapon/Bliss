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



## 代码规范
1. 所有编程相关的命名均不能以下划线或美元符号开始，也不能以下划线或美元符号结束（例外：私有字段可使用 `_lowerCamelCase`，与现有代码保持一致）
2. 类名、公有的方法名、公有的成员变量使用 UpperCamelCase 风格（例外：框架事件/调用处理器类可沿用 `Xxx_YYY_ZZZ` 命名）
3. 非公有的方法名、非公有的成员变量、参数名、局部变量都统一使用 lowerCamelCase 风格
4. 常量命名应该全部大写，单词间用下划线隔开，力求语义表达完整清楚，不要嫌名字长
5. 枚举命名以大写E开头
6. IDE 的 text file encoding 设置为 UTF-8；IDE 中文件的换行符使用 Unix 格式，不要使用 Windows 格式
7. 注释的双斜线与注释内容之间有且仅有一个空格
   历史代码若不符合该规则，新增/修改代码需按本规则执行
8. 新增或修改的公共类、公共函数/方法、公共属性、公共字段和公共事件必须添加注释，说明用途及必要的使用约束；C# 使用 XML 文档注释，并按需说明参数、返回值和异常。非直观或特殊的实现方式也必须就近注释，说明其原因和作用
9. 新增、修改或优化性能敏感的组件、算法及影响复杂度的数据结构时，必须执行仓库根目录 `Doc/Bliss.PerformanceSensitiveWorkflow.md` 中的流程
10. 不允许任何魔法值（即未经预先定义的常量）直接出现在代码中

# 每次做出决定时请务必检查代码是否符合上述规范，确保代码的可读性和维护性,执行完毕后再次检测查代码是否符合上述规范
# 做Plane是显示将要提交或修改的代码