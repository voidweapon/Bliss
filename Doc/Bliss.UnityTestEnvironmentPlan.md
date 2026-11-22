# Unity 客户端自动化测试环境构建计划

## 状态与目标

本计划记录已确认的第一阶段方案，尚未实施。目标是在 `Unity/` 客户端建立可离线、无需启动服务器的自动化测试环境，先覆盖客户端独有的纯逻辑。

- 客户端与服务器共享的代码由 `Bliss.UnitTest/` 下的独立测试工程覆盖。
- 客户端独有功能使用 Unity Test Framework 的 EditMode 和 PlayMode 测试；第一阶段只建设 EditMode，PlayMode 留待后续阶段。
- 第一阶段不配置 CI，也不处理客户端与服务器联调。

## 现有基础

- Unity 工程版本以 `Unity/ProjectSettings/ProjectVersion.txt` 为准；`Unity/Packages/manifest.json` 已声明 Unity Test Framework。
- `Unity/Assets/Scripts/Tests/Bliss.UnityTests.asmdef` 是仅在 Editor 平台使用的现有测试程序集，并已引用客户端的 `Bliss.Unity` 程序集。
- `Unity/Assets/Scripts/Tests/MemoryPack_Tests.cs` 包含已有测试。第一阶段保留它们，不迁移或删除；默认运行全部 EditMode 测试时也应执行这些测试。
- `ReferenceCollectorDataComparer` 位于 `Unity/Assets/Scripts/Unity/ReferenceCollector.cs`，按 `StringComparison.Ordinal` 比较数据的 `key`，适合作为首个客户端独有纯逻辑测试对象。

现有配置和测试的实际运行状态尚未验证，实施时应先确认测试能被 Unity 发现和执行，不能将已有程序集或依赖声明视为运行成功的证明。

## 第一阶段实施步骤

1. 沿用现有 `Bliss.UnityTests` 测试程序集，在其测试目录内新增 `ReferenceCollectorDataComparer` 的 EditMode 测试。验证小于、大于、相等及大小写敏感的序数比较，并覆盖空字符串等边界；断言比较结果的符号而非假设具体非零值。测试不依赖场景、服务器或网络。
2. 提供 macOS 和 Windows 命令行脚本，调用者显式传入与项目版本匹配的 Unity Editor 可执行文件路径。脚本默认对 `Unity/` 工程运行全部 EditMode 测试，不自动安装 Editor 或修改许可证；同时保留通过 Unity Editor 的 Test Runner 运行测试的方式。
3. 将 NUnit XML 结果与 Editor 日志写入 `Unity/` 下的 Git 忽略专用目录，使用固定文件名，每次运行覆盖上一次结果。实施时确定目录和脚本文件名，并配置 `.gitignore`；不提交测试结果或日志。
4. 脚本正确处理包含空格的路径，检查必要输入和输出目录，返回非零状态以报告测试失败、Editor 启动失败或没有有效结果等异常。避免把上一次运行残留的 XML 当成本次成功的结果；项目已被其他 Editor 打开、许可证不可用或编译失败时，保留诊断信息并明确报告，不关闭其他人的 Editor。
5. 补充使用说明，说明 Editor Test Runner 操作、两平台脚本的调用方式、Editor 路径要求、结果位置和常见失败处理。脚本及配置的具体路径在实施时确定，避免把尚不存在的命令当成现有功能。

## 验收标准

- 在 Unity Editor 的 Test Runner 中能发现并运行新增的比较器测试；测试覆盖上述比较行为，且无需服务器或网络。
- macOS 和 Windows 的脚本均能使用显式提供的 Editor 路径运行全部 EditMode 测试，包括新增测试与保留的历史测试；两平台分别验证，未验证的平台须明确标记。
- 成功运行产生可解析的 NUnit XML 和 Editor 日志，XML 中实际包含新增测试及历史测试的执行结果；测试失败时脚本返回非零状态，启动失败或结果缺失也不能报告成功。
- 连续运行覆盖固定位置的结果与日志；结果目录中的 XML 和日志被 Git 忽略，且不会被提交。
- 使用项目指定的 Unity Editor 版本完成验证；若现有测试、编译、许可证或平台环境阻碍验收，记录具体阻碍，不将编译通过等同于测试通过。

## 后续阶段：客户端 PlayMode 与 Server 联调

本阶段方案已通过讨论确认，尚未实施。它扩展第一阶段的 EditMode 环境，不改变共享代码由 `Bliss.UnitTest/` 覆盖的边界。

Server 进程内业务集成测试和 .NET 驱动的 Server 端到端测试另见 [Server 测试环境计划](Bliss.ServerTestEnvironmentPlan.md)；两类测试与 Unity 客户端联调互为补充，不相互替代。

### 测试组织与范围

- 保留 Editor-only 的 `Bliss.UnityTests` 供 EditMode 测试使用；新增一个独立 PlayMode 测试程序集，通过分类或过滤提供两组各自的运行入口及按顺序运行两组的完整入口。
- PlayMode 测试分为两组：
  - **客户端协议模拟测试**：使用轻量测试专用 bootstrap 初始化真实客户端登录所需组件和 Root，由测试驱动器推进 World；夹具管理本机 fake Realm/Gate，通过客户端现有网络路径测试完整登录握手。覆盖成功和至少一条可控的拒绝/失败路径，并验证失败时不建立登录成功状态。当前不增加可注入的 transport/session 抽象。
  - **客户端与 Server 真实联调测试**：加载生产 `Init` 场景，经过生产资源加载和客户端框架初始化后，由生产 `Init` 驱动 World；连接本机实际 Server，调用 `LoginHelper.Login`，检查登录后的 `ClientSessionComponent`、`Player` 和 `Event_LoginSuccess`。不自动操作登录 UI，首批不包含地图进入和场景切换。
- 两组各自独占客户端 World 生命周期，确保只有对应驱动器更新 World；结束时释放各自的 Session、网络线程和测试创建的对象。遇到已有不兼容的活动 World 时明确失败，不接管或销毁它。
- 当前客户端 Realm 地址与 Server 配置使用固定 loopback 端口。测试运行前检查所需端口；冲突时明确失败且不终止占用端口的其他进程。fake 与真实 Server 使用相同 Realm 端口，因此测试组必须串行执行，暂不支持同机并行运行。
- 当前 Server 登录流程不校验账号密码，Gate 按账号在进程内存中创建玩家。真实 Server 测试使用每次运行生成的合成账号，不使用真实用户凭据。正式认证、账号注册或持久化登录流程实现后，重新设计测试账号准备、清理及成功/拒绝场景。

### 本地运行与进程生命周期

- 提供 macOS 和 Windows 本地命令入口，使用显式指定的 Unity Editor 路径和项目指定版本；支持单独运行任一组，也支持按顺序运行完整测试。真实联调入口自动构建当前工作区的 .NET 8 Server。
- 生产客户端启动所需资源包由开发者预先准备；运行前检查所需资源存在并记录版本或标识，能验证兼容性时进行校验，不能验证时明确提示由开发者确认，不声称已验证资源与当前代码一致。
- 完整入口先在没有实际 Server 监听时运行 fake 协议端点测试；再启动配置中的 Server process 1，等待 `Server Init Success` 初始化信号，并运行真实联调测试。Server 就绪语义见 [Server 测试计划](Bliss.ServerTestEnvironmentPlan.md)。
- 真实联调等待客户端框架、登录所需 Root 和网络组件的可靠就绪条件后才调用登录；若现有启动链缺少可靠信号，实施时增加或暴露该信号。初始化失败或有界等待超时须明确失败并保留 Editor 日志，不以固定延时代替就绪检查。
- 启动器持续读取本次 Server 子进程的 stdout/stderr，将其写入 Unity 测试结果目录；仅从该子进程实时输出识别就绪信号并确认进程存活。运行前清除旧结果，提前退出或信号超时即失败；不修改 NLog 开发配置或清理共享日志。
- Server 尚无专用优雅关闭协议。运行器只跟踪并停止本次启动的 Server 子进程；退出或超时后有界等待，必要时强制停止该子进程，不按进程名清理其他实例。
- Unity Editor Test Runner 保留用于手动调试：fake 端点测试自行管理端点；手动运行真实联调测试前，开发者需自行启动兼容的本机 Server。完整自动构建、启动、清理流程以本地命令入口为准。
- 两组测试分别输出 NUnit XML 及对应日志到 `Unity/` 下 Git 忽略的专用结果目录，使用固定文件名并覆盖上次对应结果。命令入口运行完两组测试后汇总结果；任何一组失败、启动未就绪、超时或缺少有效报告都返回非零状态，并保留诊断日志。测试结果和日志不提交版本库。

### 后续阶段验收

- 单独运行协议模拟组可验证成功和拒绝路径；单独运行真实联调组须实际经过生产 `Init` 与资源加载、等待客户端和 Server 就绪并验证登录状态。完整入口按顺序运行两组。
- 资源缺失、可检测到的不兼容、客户端初始化失败或就绪超时须明确失败；无法验证资源版本时保留标识及提示。已有不兼容 World 时不接管或销毁它。
- 每组结束后不残留测试拥有的客户端 Session、网络线程或 fake 端点；本次 Server 日志包含可识别的就绪信号和重要错误，旧结果不能冒充本次输出。

### 后续演进

- **GitHub Actions**：Unity Editor 可在 runner 中以 batchmode 运行；CI 接入暂不属于本阶段。后续需确定 runner 平台、Unity 许可证激活与席位、秘密配置和结果归档，再接入现有工作流。
- **可并行运行**：为客户端 Realm 地址和 Server 端口增加可配置能力后，再设计动态端口、并发隔离和并行测试。
- **优雅关闭**：为 Server 增加受控关闭方式后，评估替换运行器对子进程的强制清理兜底。
- **认证及更多业务流程**：正式登录认证实现后更新测试账号和失败场景；再按需要扩展至进入地图、场景切换及 UI 自动化。
