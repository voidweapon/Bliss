# Server 业务测试环境构建计划

## 状态与目标

本计划记录已确认的方案，尚未实施。目标是使用进程内业务集成测试和真实 Server 进程端到端测试覆盖业务流程，而不局限于单元测试。

- 进程内测试沿用 `Bliss.UnitTest/Bliss.GameplayIntegrationTest/`，验证多个 Root 与业务组件协作时的内部状态。
- 新增独立的 Server 端到端测试工程，启动真实 Server 并通过网络驱动业务流程。
- Unity 客户端 PlayMode 与 Server 的真实联调仍由[客户端测试计划](Bliss.UnityTestEnvironmentPlan.md)负责；本计划的端到端驱动端是 .NET 测试客户端，不替代 Unity 客户端测试。

## 现有基础与约束

- `Bliss.GameplayIntegrationTest` 已有进程内 Root 测试，例如 `MoverIntegrationTests`。保留现有场景，并在需要时扩展，不另建重复的进程内测试工程。
- Server 的 `App` 持续运行；`CodeLoader` 从当前工作目录加载 `Logic.dll` 和 `Logic.pdb`。测试启动器必须使用正确的 Server 输出布局、工作目录和真实 `ServerRes` 资源，不能以跳过业务初始化或移除处理器来替代生产启动。
- 当前配置由 `Process 1` 承载所有 Root，网络使用固定 loopback UDP 端口。测试运行前检查所需端口；发现冲突时明确失败，不终止占用端口的其他进程，也不改写开发配置。
- 当前 Server 业务链路没有实际 MongoDB 连接。MongoDB 连接属于后续工作，不能把现有序列化或迁移能力当作数据库集成。

## 测试分层

### 进程内业务集成测试

- 使用现有 `Bliss.GameplayIntegrationTest` 工程，在 Server 进程内构造和协调 Root，以内部状态断言跨组件或跨 Root 的业务结果。
- 测试夹具围绕 Root 管理测试生命周期和状态读取；外层测试协调器编排一个或多个 Root，并汇总断言所需的数据。
- `RootManager` 主线程/独立线程调度器落地后，扩展此层以覆盖两种调度方式。测试不能假定 Root 永远由测试线程直接更新。

### Server 端到端测试

- 新增 `Bliss.UnitTest/Bliss.ServerE2ETest/`，使用 .NET 8、NUnit 和 `dotnet test`。
- 由 .NET 测试客户端通过生产协议定义、序列化和网络帧格式连接真实 Server。测试客户端不使用 Unity，也不以 fake Server 代替被测 Server。
- 真实 Server 使用当前工作区构建产物、完整业务初始化及真实 `ServerRes` 资源。测试工程的 NUnit 生命周期夹具负责准备/构建 Server、启动子进程并等待就绪，避免使用过期的 Server 二进制。
- 首批以当前 `Process 1` 配置为边界。测试夹具按 Server 实例封装，便于未来扩展多实例拓扑；首批每个测试组启动一个 Server 实例，组内串行运行。
- 以完整初始化日志 `Server Init Success` 作为就绪信号。启动失败、未就绪、测试超时或缺少有效报告均返回非零状态；运行结束只清理本次启动的子进程。优雅关闭协议后续实现。

## 首批业务流程与断言

1. **登录并进入有效地图**：使用合成账号，通过真实网络流程登录并请求进入配置中的有效地图。断言收到成功的 `G2C_EnterMap` 响应，以及对应的地图切换和自身单位推送。成功响应是 Map 迁移 RPC 完成后的屏障；推送本身不单独作为实体已经激活的证明。
2. **拒绝无效 MapId**：登录后请求一个不存在的 MapId，断言响应为 `ERR_NOT_FOUND_MAP`，且测试不会把该请求判定为成功进图。

当前登录流程不校验账号密码，测试使用每次运行生成的合成账号，不使用真实用户凭据。登录认证、账号注册或持久化流程改变后，需重新设计账号准备、清理及成功/拒绝场景。

## 运行、结果与诊断

- 端到端测试保持独立、显式运行，不隐式加入快速单元或进程内测试组；未来在 GitHub Actions 中作为单独 job。
- macOS、Windows 和 Ubuntu GitHub Actions 是目标环境，须在实施时逐个平台验证 Server 启动、Native 依赖、完整资源加载和网络行为；未实际运行的平台不得标记为已支持。
- NUnit XML、测试运行器日志和 Server 日志统一写入 `Bliss.ServerE2ETest/TestResults/`。目录加入 Git 忽略规则；使用固定文件名，每次运行覆盖旧结果。
- 测试失败、Server 启动/就绪失败、超时或报告缺失时返回非零，并保留本次运行的诊断日志。端口冲突须报告被占用的端口，不关闭其他进程。

## 验收标准

- `Bliss.GameplayIntegrationTest` 可继续运行现有进程内场景；Root 调度器落地后，该层分别验证主线程和独立线程调度。
- 对端到端工程运行 `dotnet test` 能构建并启动当前工作区的真实 Server，以生产协议完成登录和进入有效地图；成功、无效 MapId 拒绝两条路径均有可观察断言。
- 测试加载真实 `ServerRes` 并执行完整业务初始化；有效地图路径同时验证进入地图响应和相关推送，不能仅以进程启动或端口可连接作为通过。
- macOS、Windows 和 Ubuntu 的实际验证结果逐平台记录；Native 依赖、配置、资源或端口等环境问题须明确失败，不能伪装成业务测试成功。
- XML 与两类日志生成在专属忽略目录；测试失败、启动未就绪、超时和报告缺失均导致非零退出，且只清理本次启动的 Server 子进程。

## 后续阶段

- **Root 调度器**：待 `RootManager` 支持主线程和独立线程调度后，扩展进程内测试覆盖两种模式；端到端测试使用生产调度模式。
- **MongoDB**：待 Server 建立实际 MongoDB 连接后，按业务需要增加连接真实隔离数据库的端到端场景。
- **多进程拓扑**：首批只启动当前单个 Server 实例；待业务部署到多个 Server 实例后，再扩展测试编排和实例间业务流程。
- **优雅关闭**：Server 增加受控关闭协议后，评估让测试夹具通过协议关闭实例，保留子进程清理作为异常兜底。
- **CI 配置**：端到端工程可独立运行后，再新增单独 GitHub Actions job，并记录平台与 Native 依赖验证结果。
