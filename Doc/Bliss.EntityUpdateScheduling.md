# Entity Update 调度方案

## 目标

为 Entity 的 UpdateSystem 提供可配置的生命周期阶段和阶段内执行优先级。调度器在注册时完成分组，运行时按组遍历 Entity 队列，避免每帧对所有 Entity 的系统调用做全局排序。

## 核心约定

- `GetUpdateGroup()` 决定 UpdateSystem 所属的更新阶段组。
- `GetPriority()` 决定同一更新阶段组内 SystemGroup 的执行顺序；数值越小越先执行。
- 同一 Entity 类型可以有多个 UpdateSystem，并分别属于不同阶段组或优先级。
- 没有任何 `IUpdateGroupSystem` 的 Entity 不加入更新队列。
- 同一优先级的不同 SystemGroup 之间不保证执行顺序；若系统之间有先后依赖，必须设置不同优先级。
- 同一 SystemGroup 的 EntityRef 队列按注册顺序遍历；已销毁或无效的 EntityRef 跳过。
- 业务 Entity 尽量只保留数据，业务逻辑放到对应 System 的扩展方法中；尽量避免闭包和 lambda，调度回调优先使用缓存的静态方法并显式传入实体。`Entity`、`GamePlayEntity`、`LogicScene`、`LevelLogicScene`、`GameplayWorld` 等基础设施类型不受纯数据约束，可以封装共享生命周期与调度逻辑。

## 阶段模型

`ESystemUpdateGroup` 使用显式数值定义阶段顺序，并在内建阶段数值范围内为自定义阶段预留子区段。内建阶段使用以下值：

| 内建阶段 | 数值 |
| --- | ---: |
| `EarlyUpdate` | 100 |
| `Update` | 200 |
| `LateUpdate` | 300 |

自定义阶段按数值归入前方最近的内建阶段入口：

| 调度入口 | 阶段组范围 | 示例 |
| --- | --- | --- |
| `EarlyUpdate()` | `1 <= group <= EarlyUpdate` | 自定义组 A = 90 |
| `Update()` | `EarlyUpdate < group <= Update` | 自定义组 B = 120 |
| `LateUpdate()` | `Update < group <= LateUpdate` | 自定义组 C = 210 |

每个入口只处理自己的数值区间。区间内的阶段组按枚举数值升序遍历，因此一个系统每帧只在所属阶段执行一次。更新系统的分类接口、Entity 约束和阶段区间必须一致：Early 系统使用 `IEarlyUpdateSystem`、Entity 实现 `IEarlyUpdate`；Update 系统使用 `IUpdateSystem`、Entity 实现 `IUpdate`；Late 系统使用 `ILateUpdateSystem`、Entity 实现 `ILateUpdate`。直接实现 `IUpdateGroupSystem` 的系统也必须声明上述分类之一并遵守对应约束。

`None`、`Max`、未定义枚举值以及不属于其分类接口对应区间的值都不是有效 UpdateGroup。系统注册时必须立即报错；非法阶段值使用 `ArgumentOutOfRangeException`，错误信息包含系统类型和非法组值。

## 调度结构

调度器采用以下层级：

```text
UpdateGroup
  -> SystemGroup（按 GetPriority() 升序组织）
       -> EntityRef 队列
```

- 每个 SystemGroup 对应一个具体的 UpdateSystem 类型及其固定优先级。
- 每个 UpdateGroup 保存属于该阶段的 SystemGroup。
- Entity 注册时，根据其 Entity 类型找到对应的所有 `IUpdateGroupSystem`，并将 EntityRef 加入各自 SystemGroup 的队列。
- 一个 Entity 类型有多个不同 UpdateSystem 时，可进入多个 SystemGroup 队列；每个系统在其所属阶段和优先级轮到时执行。
- 调度一个阶段时，先按数值顺序遍历该入口范围内的 UpdateGroup，再按优先级从小到大遍历各 SystemGroup，最后按队列顺序处理该 SystemGroup 中的 EntityRef。
- 不同 SystemGroup 若优先级相同，其相互顺序不保证；排序结构可以决定实际遍历顺序。

## 系统 API 与兼容性

- 保留 `IEarlyUpdateSystem`、`IUpdateSystem`、`ILateUpdateSystem` 分类接口，以及 `EarlyUpdateSystem<TEntity>`、`UpdateSystem<TEntity>`、`LateUpdateSystem<TEntity>` 基类。
- 三个基类继续提供各自原有的默认 UpdateGroup，但 `GetUpdateGroup()` 改为可覆写，以允许系统声明自定义阶段组。
- 管理器收集和查询时统一支持 `IUpdateGroupSystem`，不要求移除现有分类接口或基类。
- `GetPriority()` 数值越小越先执行。相同优先级表示系统之间没有顺序依赖；如果需要确定先后关系，应使用不同优先级。

## 帧边界与动态注册

- 对由 `EntitySystem` 或 `GameplayWorldUpdater` 驱动的更新，`EarlyUpdate()` 开始时作为各自路径的帧边界。
- 在该帧边界之前注册的 Entity 可以参与本帧调度；帧边界之后注册的 Entity 从下一帧开始参与，即使它还有尚未执行的后续阶段。
- 调度器在帧开始时确定本帧的可调度集合；帧中新增的队列项延迟到下一帧启用，避免遍历期间新增项在尚未执行的阶段意外参与本帧。
- `GameplayWorldUpdater.Add()` 使用共享调度器注册 Entity；调用方必须对每个 Entity 只调用一次。其 Early、Update、Late 三个入口分别调度对应区间。
- 普通 `Entity` 由 Root 的 `EntitySystem` 驱动；`GamePlayEntity` 只注册到其所属的 `GameplayWorld`。World 自身的驱动组件将 Root 的三个常规阶段转发给该世界，不依赖 LevelScene 是否启用。

## GameplayWorld 固定步长

- World 创建时启动一个 50ms 的 World Timer；到期时只标记待处理 tick。同一 Root 的独立模拟驱动先处理该 Root 下所有 World Timer，再派发 LogicUpdate；常规 World Update 不驱动 Timer 或 LogicUpdate。阶段顺序为 `EarlyUpdate → GameplayWorld Timer → LogicUpdate → Update → LateUpdate`。模拟及常规 World 驱动占用 Update 区间首组的最低三个优先级，其他系统不得使用这些保留优先级。
- `GameplayWorldTimer.Cancel(ref timerId)` 在尝试取消前将调用方保存的 ID 清零；无论计时器仍然活跃、已经触发、已经取消还是 World 已销毁，ID 都会被清零。返回值仅表示是否取消了活跃计时器；其他位置保存的 ID 副本不会被清除。内部等待、失效 owner 和 World 销毁仍使用原有取消流程。
- 延迟时只产生一次 tick，并从当前 Timer 处理时刻安排下次，固定 delta 为 0.05 秒；不追补。派发开始时消费 tick，回调失败包装并传播，后续入口不运行，已消费的 tick 不重试。销毁 World 会取消 Timer、完成等待并清除待处理 tick。
- 通过 `world.LogicUpdate.Register(entity, entryName, priority, callback)` 注册；身份是活跃实体实例和入口名，重复注册报错。参与者必须属于该 World；注销使用 `Unregister(entity, entryName)`。优先级按数值升序，同优先级按注册顺序。共享顺序使用 `GameplayWorldLogicPriority`：发射点检测、角色移动、共享移动模拟、弹道检测、物理模拟；最终物理优先级保留给服务端物理模拟。
- 派发开始时确定快照；期间新增或注销后重注册的入口从下次 tick 开始，已注销、销毁、复用或脱离 World 的参与者跳过。World 销毁会中止快照内剩余入口。调用方仍应在自身销毁时主动注销。
- `GamePlayEntity` 可以直接属于 GameplayWorld，也可以属于关联到该 World 的 LevelScene；后者注册前校验 LevelScene 与 World 的层级和 Root 归属，实体的逻辑场景仍为 LevelScene。
- System 在 Awake 中通过 `self.RegisterLogicUpdate(priority, callback)` 注册一个主入口；`GamePlayEntity` 以实体完整类型名作为入口名，保存注册时的 World，并在 `Entity.Dispose` 清空 InstanceId 前统一注销。业务实体无需覆写 Dispose。重复注册主入口会报错；通过 World 接口直接删除该入口后可重新注册，替代的不同回调不会被误删。其他命名入口仍需调用方主动注销。
- World 同时支持原有 `Action<float>` 和新的 `Action<Entity, float>` 回调，后者显式接收已验证的实体，不使用闭包包装旧接口。两种入口共享顺序、快照和池化规则，回收时同时清空两种回调引用。Mover 与 Kinematic System 各缓存一个静态回调，并转发到各自的扩展方法。
- `MoverComponent`、`Mover` 和 `MoverStageRuntime` 归属于 GameplayWorld。每个 MoverComponent 以 `MOVEMENT` 优先级注册一个入口，使用固定 50ms 推进其所有 Mover；所属 LevelScene 尚未启用时不推进。组件销毁前主动注销，派发中销毁后的替代组件从下个 tick 开始运行。
- `KinematicMoverManager` 注册到 `MOVEMENT`，使用固定 50ms 并与 Mover 按实际注册顺序执行；普通 LateUpdate 插值仍由 World 驱动。服务端 Map 初始化时创建并启用 World 管理器。管理器及各参与者所属的 LevelScene 必须已启用才能模拟或插值；客户端模拟 `AutonomousProxy` 与 `Authority`，跳过 `SimulatedProxy`。管理器在每次派发前复制可复用的参与者快照；回调中销毁或注销的参与者跳过，新注册的参与者从下次派发开始。Bullet 的独立 Timer 和碰撞顺序由 #15 处理，不能将当前路径视为完整 Bullet 固定步长方案。
- `CharacterController` 负责角色专用组件的初始化及销毁；`CharacterMovementComponent` 自行注册固定 tick，输入组件独立在每帧 EarlyUpdate 采样最新移动与视角数据。跳跃边缘至少保留到所属 Level 启用后的下一次 World tick，其后继续遵守原有缓冲时间与消费状态。角色和 Kinematic 专用组件均属于 GameplayWorld；派发中销毁的实体由 World 注销机制处理。
- `InputIntentProcessPipelineSystems.collectInput` 只采集原始移动、视角、相机旋转及动作状态，并整理输入记录；`EarlyUpdate` 按顺序调用采集和 `ProcessInputIntent`。相机平面移动转换、当前批次跳跃边缘识别、消费状态重置及宽限期判断统一由 `ProcessInputIntent` 处理，不把历史 Down 当作新的按下。
- 服务端和客户端 `PhysicalScene` 均属于 `GamePlayEntity`，并在 LevelScene 准备时创建。服务端在启用流程注册 `PHYSICS` 最终优先级，传入固定 0.05 秒；尚未启用的 LevelScene 不模拟，启用失败时注销，销毁前通过 `GamePlayEntity` 注销。缺失 native 场景、PhysX fetch 失败或物理模拟抛出异常时，记录故障原因并向上传播。此优先级只保证排在 **World LogicUpdate** 内的其他业务逻辑之后；尚未迁移的 Bullet Root Timer 不受此顺序约束，留待 #15。
- 物理失败后，`GameplayWorld.IsFaulted` 为 true，`Failure` 保存首次异常；World 自身的 Timer、LogicUpdate 和常规 Early/Update/Late 驱动停止，不自动恢复，已消费的 tick 不重试。现有待处理 Timer 和等待保留，新的未来 Timer 和 LogicUpdate 注册会显式失败；等待可能一直挂起，直到 World 被显式销毁或取消。Root 和子 Root 在异常传播时仍保留调度资格，其他 World 在后续帧继续运行。本次只对物理模拟失败标记 World 故障；其他持续抛错的 Root 更新仍可能在后续帧重复报错并中断当前帧，需单独制定故障策略。此隔离不阻止旧 Root Timer、外部消息或调用方直接操作实体，因此故障状态不是完整的不可变快照。Bullet 的旧 Timer 留待 #15。
- 客户端查询仍通过既有 Unity 物理适配器，不手动驱动 Unity 物理模拟；现有适配器使用 Unity 全局物理查询，并未实现各 LevelScene 的独立 Unity 物理场景隔离。现有查询适配器未限制向调用方数组复制的命中数，小于命中数的结果数组可能越界，此次未改造该既有路径。
- 真实服务端地图资源加载仍将物理材质存入 Root 级 `PhysicalWorldComponent.MaterialsRef`，Level 销毁不会移除对应项；重复加载相同材质 ID 或失败后重试可能发生重复键错误。此次测试替换了地图资源加载处理器，未覆盖该既有资源生命周期问题。

### 输入管线测试待办

- [ ] 在 Unity 工程的 Test 环境中构建模拟输入，测试 `InputIntentProcessPipeline`：通过生产 `PlayerInputComponent` 采集与真实 World 帧调度，覆盖移动及视角意图、同批次相同/不同时间戳的 Down 与 Cancel、跳跃消费与再次按下、跨固定 tick 缓冲和 Level 启停。区分管线意图断言与角色实际起跳断言；当前直接提供 `IInputSource` 的测试不能替代这项覆盖。

## 游戏实体与专用组件归属

- `Unit`、`SceneActor`、`ASceneActorCmp` 及其派生类型、`AOIEntityCmp`、`AbilitySystemComponent`、`GameplayLayer` 和 `SmoothTransformComponent` 归属于创建它们的 GameplayWorld 或关联 LevelScene。普通管理器、网络角色、通用 Transform、RemoteGhost、ReplicationGraph 和 LevelScene 管理组件仍是普通 `Entity`。Bullet 自身及其独立调度留待 #15；其 AOI 和客户端平滑组件已走 World。
- Gate Root 为登录中转创建一个默认 GameplayWorld；临时 Unit 在该 World 创建，ActorAddress、BSON 和传送协议保持原有路径。`GameplayLayer` 不参与 BSON 序列化，目标 Map World 挂载反序列化 Unit 后重新建立 Unit 层；其他序列化组件仍通过原有反序列化和挂载流程恢复。此处未启动网络服务验证跨进程传送。
- Map 在解除位置锁之前已经为转移的 Unit 安装有序邮箱，后续完成 Unit 初始化时不重复添加。客户端 World 工厂初始化 AOI 管理器，并以 `LogicScene` 事件类型通知 World 创建处理器。
- 服务端 Map 初次转移将 Unit 挂载到已启用的默认 LevelScene，角色从该 Level 的 PhysicalScene 创建真实 PhysX 运动学刚体和胶囊；材质为静摩擦 0.6、动摩擦 0.6、弹性 0。服务端即时移动消息、固定 tick 和 motor 直接设置姿态时同步物理刚体；查询通过形状映射找到游戏碰撞体。失败时清理角色已拥有的组件和 native 资源。集成测试覆盖 BSON 初次挂载、角色初始化、射线查询、尺寸修改及销毁，但没有启动网络服务运行完整转移处理器或跨进程登录。
- 原有时间戳条件错误地拒绝正常有限时间戳，本次修正后有效移动消息会立即处理；既有处理器仍直接采用客户端提供的位置，尚无距离或速度校验，客户端可能提交任意位置。按当前约定保留该信任模型，服务端校验方案另行讨论。
- 已挂载 Unit 不能通过初次挂载接口冒充跨 Level 迁移；保留玩家在卸载前的原子迁移及卸载保护留待 #14，当前卸载仍会销毁未迁出的 Unit。
- 客户端 EditMode 测试调用生产 World、Unit 和 Bullet 工厂，并检查角色初始化、AOI 及平滑组件；测试替换了外部资源准备和视图刚体、胶囊体创建/销毁，不验证真实视图资源或这些 Unity 物理对象的生命周期。
- `SmoothTransformComponent` 保持逐帧视觉更新，而不是固定 50ms LogicUpdate；它现在由 World 驱动，故障 World 不再更新。普通 Root 更新与 World 更新的相对阶段可能不同，不能假设其旧注册顺序。
- `ShooterGear` 在场景初始化时创建 10 秒 World Timer，销毁时取消；LevelScene 未启用或 World 故障时不发射。未启用时到期仍按原间隔重新计时，启用不会立即补发；延迟帧不追补。注册回调是绑定实体的 System 扩展方法，没有 lambda 或闭包；Timer 和实体存活期由 World 与销毁流程管理。

### 游戏归属及 AOI 生命周期基准

使用 `BlissBenchMark/Benchmarks/GameplayOwnershipBenchmark.cs` 在真实 Root/World 下测量。创建负载每批创建指定数量的最小 Unit（带 GameplayLayer、空 AOI 与 SmoothTransformComponent）和 SceneActor（带空 AOI）；空闲更新负载每帧驱动已完成平滑的组件，活跃更新负载在计时外准备尚未完成的平移和旋转插值，测量实际更新并检查插值时间推进。AOI 负载逐个创建 Unit、注册 Visibility 与 NetworkVisibility 球形 AOI、销毁 Unit 并检查条目已清空；每次仅一个 Unit 存活，不代表密集交互。AOI 基线在迁移后将 Unit、GameplayLayer、AOIEntityCmp 的继承临时还原为原有 `Entity` 后测量；活跃更新基线还还原了 SceneActor 和 SmoothTransformComponent 的继承。其他迁移代码保留但不参与上述基准。

macOS 26.6.2、Apple M1 Pro、.NET 8.0.19、BenchmarkDotNet 0.15.2 ShortRun；每项 3 次测量，误差为 99.9% 置信区间半宽：

| 操作 | 数量 | 迁移前耗时 | 迁移后耗时 | 迁移前分配 | 迁移后分配 |
| --- | ---: | ---: | ---: | ---: | ---: |
| 批量创建 | 100 | 623.861 ± 62.197 µs | 609.652 ± 355.869 µs | 323,280 B | 333,816 B |
| 批量创建 | 1,000 | 6,664.472 ± 1,411.453 µs | 6,763.319 ± 1,737.314 µs | 3,312,760 B | 3,416,896 B |
| 平滑更新 | 100 | 2.288 ± 0.328 µs | 2.273 ± 0.125 µs | 168 B | 168 B |
| 平滑更新 | 1,000 | 22.282 ± 1.769 µs | 22.330 ± 3.043 µs | 168 B | 168 B |
| 活跃平滑更新 | 100 | 98.01 ± 109.36 µs | 87.68 ± 28.05 µs | 2.51 KB | 2.51 KB |
| 活跃平滑更新 | 1,000 | 825.86 ± 381.65 µs | 826.50 ± 277.07 µs | 23.60 KB | 23.60 KB |
| AOI 生命周期 | 100 | 2.150 ± 1.561 ms | 2.179 ± 0.566 ms | 823.84 KB | 828.52 KB |
| AOI 生命周期 | 1,000 | 21.259 ± 5.885 ms | 21.280 ± 3.573 ms | 8,208.27 KB | 8,255.15 KB |

批量创建的测得分配增加约 3%，平滑更新分配不变；AOI 生命周期分配增加约 0.6%。创建负载的迁移前运行没有初始化 EventSystem，迁移后初始化了 EventSystem；该负载初始位置不发生变化，不触发 Transform 事件，AOI 前后则均初始化 EventSystem。样本量少、部分迭代短于建议的 100ms，耗时置信区间重叠，不能据此判定耗时变化。以上不覆盖真实地图资源、密集 AOI 可见关系、网络、碰撞或完整帧。

### World Timer 取消基准

使用 `BlissBenchMark/Benchmarks/GameplayWorldTimerCancellationBenchmark.cs` 比较实际 `GameplayWorldTimer` 的批量取消：取消前调用方自行清零，取消后使用 `Cancel(ref timerId)` 清零。每批分别准备活跃 ID 或已取消但未清零的旧 ID，覆盖单个和 100 个到期时间桶；准备工作不计时，并检查取消结果和 ID 已清零。使用无 Root 的 World 构造接缝和无 owner 的计时器，包含 World 固定步长标记 Timer，不覆盖 owner 检查、回调、等待、并发或完整帧。

同上环境和 BenchmarkDotNet ShortRun，每项 3 次测量；误差为 99.9% 置信区间半宽：

| 数量 | 时间桶 | ID 状态 | 修改前耗时 | 修改后耗时 | 修改前/后分配 |
| ---: | ---: | --- | ---: | ---: | ---: |
| 100 | 1 | 活跃 | 8.027 ± 10.869 µs | 7.625 ± 1.514 µs | 24 B / 24 B |
| 100 | 1 | 已取消 | 1.069 ± 0.442 µs | 1.403 ± 2.191 µs | 0 B / 0 B |
| 100 | 100 | 活跃 | 35.348 ± 48.561 µs | 36.000 ± 2.290 µs | 2,400 B / 2,400 B |
| 100 | 100 | 已取消 | 1.097 ± 0.432 µs | 1.389 ± 2.453 µs | 0 B / 0 B |
| 5,000 | 1 | 活跃 | 138.361 ± 30.254 µs | 154.805 ± 237.855 µs | 0 B / 0 B |
| 5,000 | 1 | 已取消 | 35.319 ± 111.360 µs | 31.195 ± 1.910 µs | 0 B / 0 B |
| 5,000 | 100 | 活跃 | 175.388 ± 221.509 µs | 171.680 ± 101.294 µs | 0 B / 0 B |
| 5,000 | 100 | 已取消 | 33.542 ± 71.776 µs | 36.333 ± 95.007 µs | 0 B / 0 B |

这次是接口语义变更而非性能优化；取消仍使用原有字典和时间桶，复杂度不变。测得分配相同；样本少、全部迭代短于建议的 100ms，耗时置信区间重叠，无法判断耗时变化。

### 服务端物理场景迁移基准

使用 `BlissBenchMark/Benchmarks/PhysicalSimulationBenchmark.cs` 比较实际 Root 驱动的迁移前 Root Timer 与迁移后 World LogicUpdate，均运行 native PhysX 50ms 模拟。场景位于 World 下，包含 0、10 或 100 个无碰撞形状的独立动态刚体；准备和销毁不计时，并验证每个刚体受重力移动。该基准不包含碰撞形状、接触、场景资源、LevelScene 启用、游戏实体或完整帧，不能代表实际地图的物理开销。

macOS 26.6.2、Apple M1 Pro、.NET 8.0.19、BenchmarkDotNet ShortRun；每项 3 次测量，误差为 99.9% 置信区间半宽：

| 刚体数量 | 迁移前耗时 | 迁移后耗时 | 迁移前分配 | 迁移后分配 |
| ---: | ---: | ---: | ---: | ---: |
| 0 | 28.35 ± 12.87 µs | 32.51 ± 175.09 µs | 856 B | 568 B |
| 10 | 45.07 ± 231.44 µs | 41.90 ± 228.79 µs | 856 B | 568 B |
| 100 | 41.10 ± 75.75 µs | 56.03 ± 256.81 µs | 856 B | 568 B |

测得每 tick 的托管分配减少 288 B；每次迭代短于建议的 100ms，样本少且耗时置信区间重叠，无法判断耗时改善或退化。每个 World 在 Physics 最终优先级按注册顺序模拟各场景，而不是原先由 Root Timer 独立驱动。

### Root 异常保护基准

使用 `BlissBenchMark/Benchmarks/RootSchedulingBenchmark.cs` 比较 Root 队列异常保护及 World 故障检查前后实际 `RootManager.Update` 路径。每个顶层 Root 含一个子 Root，二者各有一个带普通 Update 参与者的 GameplayWorld；在准备阶段验证每个参与者均运行。计时只覆盖正常 Update，不包含故障、到期物理 tick、资源加载或完整帧。

同上环境和 ShortRun 配置，每项 3 次测量；误差为 99.9% 置信区间半宽：

| 顶层 Root 数量 | 修改前耗时 | 修改后耗时 | 修改前分配 | 修改后分配 |
| ---: | ---: | ---: | ---: | ---: |
| 1 | 184.4 ± 9.74 ns | 187.9 ± 26.07 ns | 400 B | 400 B |
| 10 | 1,912.0 ± 46.11 ns | 1,850.0 ± 45.28 ns | 3,424 B | 3,424 B |
| 100 | 26,052.1 ± 1,093.99 ns | 25,983.2 ± 11,865.13 ns | 33,664 B | 33,664 B |

测得分配不变；三次样本的耗时置信区间重叠，不据此判断耗时变化。异常隔离及两个 Root 的继续运行由真实调度路径的集成测试覆盖。

### Mover 迁移基准

使用 `BlissBenchMark/Benchmarks/MoverSimulationBenchmark.cs` 测量真实 Root 驱动路径。每个 cargo 创建一个 MoverComponent 和使用服务端配置 `TestMover1`（速度 10）的直线 Mover；0、100、1,000 个 cargo 直接放在 GameplayWorld 下，每轮测量一个到期 tick，实体构建和发射不计时。基准验证每个 cargo 确实移动；为使旧版基线有近似 50ms 的真实时间差，计时外将旧入口的上次更新时间设为当前时间减 50ms。旧版实际 delta 使用墙钟，因此只能近似匹配新方案的精确 50ms，不是严格等价的时间语义。

在 macOS 26.6.2、Apple M1 Pro、.NET 8.0.19、BenchmarkDotNet ShortRun 下，迁移前后使用相同负载；每项 3 次测量，误差为 99.9% 置信区间半宽：

| cargo 数量 | 迁移前耗时 | 迁移后耗时 | 迁移前分配 | 迁移后分配 |
| ---: | ---: | ---: | ---: | ---: |
| 0 | 3.791 ± 32.956 µs | 3.111 ± 8.549 µs | 496 B | 496 B |
| 100 | 156.861 ± 24.596 µs | 127.265 ± 28.074 µs | 20,744 B | 16,744 B |
| 1,000 | 1,612.208 ± 264.756 µs | 1,307.743 ± 904.540 µs | 193,552 B | 160,744 B |

旧路径包括每个组件的 Root Timer 派发和重排，新路径包括 World Timer 与 LogicUpdate 派发；均包含移动和 Transform 事件，不包含场景创建、销毁、碰撞、Kinematic 或完整游戏帧。100 和 1,000 cargo 的测得分配下降约 19% 和 17%，但剩余分配仍随数量增长。所有迭代均短于 BenchmarkDotNet 建议的 100ms，样本仅 3 次且耗时置信区间重叠，不能据此保证耗时改善。LevelScene 启用检查、销毁和派发中替换由真实生命周期集成测试覆盖，而非此基准。

### Kinematic 管理器迁移基准

#### IndexList 注销修复基准

Kinematic 参与者使用 `IndexList` 按实例 ID 注销。原实现只移除键映射，未正确缩减底层列表；删除非末尾元素时还留下重复的末尾值。修复后将末尾值及其键映射移到空位，再移除末尾元素；删除顺序仍不稳定，复杂度保持均摊 O(1)。单元测试覆盖首、中、末尾按键和索引删除、按值删除、缺失键及清空后的复用。

`BlissBenchMark/Benchmarks/IndexListRemovalBenchmark.cs` 对每次操作准备 64 个独立的真实 `IndexList<int, long>`，在计时外各装入 100 或 1,000 项，在计时内删除首、中或末尾键；计时外验证键映射确实被移除。由于旧实现未移除列表元素，前后工作语义不等价，这只能作为修复前调用成本参考，不能作为正确实现间的性能比较。该基准不覆盖重复键、按值/索引删除、参与者销毁或完整帧。

macOS 26.6.2、Apple M1 Pro、.NET 8.0.19、BenchmarkDotNet 0.15.2 ShortRun；每项 3 次测量，误差为 99.9% 置信区间半宽，单次删除无可测托管分配：

| 元素数 | 删除位置 | 修复前 | 修复后 |
|---:|---|---:|---:|
| 100 | 首 | 180.6 ± 175.15 ns | 151.7 ± 145.88 ns |
| 100 | 中 | 177.1 ± 120.66 ns | 154.3 ± 83.22 ns |
| 100 | 末 | 105.9 ± 75.46 ns | 101.0 ± 24.72 ns |
| 1,000 | 首 | 243.5 ± 590.18 ns | 278.2 ± 867.11 ns |
| 1,000 | 中 | 313.1 ± 2,079.31 ns | 278.4 ± 1,762.67 ns |
| 1,000 | 末 | 237.8 ± 769.21 ns | 196.7 ± 848.79 ns |

迭代耗时远低于 BenchmarkDotNet 建议的 100ms，样本少、置信区间宽且前后语义不同，不能据此判断性能改善或回归。

使用 `BlissBenchMark/Benchmarks/KinematicSimulationBenchmark.cs` 测量真实 Root 和 KinematicMoverManager 调度；一个启用的管理器驱动 0、100 或 1,000 个 KinematicRigidbodyMover，每个使用固定目标的测试控制器和真实 Transform、GameplayRigidbody。实体准备不计时；旧版在计时外将管理器上次帧时间设为当前帧时间减 50ms，确保两版均传入 0.05 秒，基准校验每个平台的实际移动。刚体没有 native 物理对象，因此这个基准不测量物理引擎、角色、插值或真实场景负载。

同上环境和 ShortRun 配置，每项 3 次测量，误差为 99.9% 置信区间半宽：

| 平台数量 | 迁移前耗时 | 迁移后耗时 | 迁移前分配 | 迁移后分配 |
| ---: | ---: | ---: | ---: | ---: |
| 0 | 5.889 ± 28.185 µs | 3.166 ± 5.947 µs | 856 B | 568 B |
| 100 | 118.500 ± 26.142 µs | 119.792 ± 38.093 µs | 856 B | 568 B |
| 1,000 | 1,193.694 ± 38.627 µs | 1,163.090 ± 120.407 µs | 856 B | 568 B |

测得每 tick 分配均减少 288 B；迭代时间低于 BenchmarkDotNet 建议的 100ms，样本仅 3 次，计时置信区间重叠，不能据此判断耗时改善或退化。同优先级的实际 Mover 与 Kinematic 顺序、固定 delta 和销毁边界由集成测试覆盖。

### #13 角色调度及 native 生命周期基准

`CharacterSchedulingBenchmark` 使用真实 Root、World 和 PhysX 角色，计时外准备 100 或 1,000 个角色并校验调度与 native 位姿同步。`Frame` 测量 EarlyUpdate 与固定角色入口，不启用 motor 模拟；`FrameWithMotors` 启用 Kinematic 管理器、输入和跳跃，包含服务端 native 位姿同步，但关闭碰撞及落地求解。`CreateAndDispose` 在已有同等数量角色时逐个创建、销毁完整角色，并校验物理映射清理；准备阶段不计时。负载未模拟物理场景、实际碰撞、网络、资源加载或完整游戏帧。

macOS 26.6.2、Apple M1 Pro、.NET 8.0.19、BenchmarkDotNet 0.15.2 ShortRun；每项仅 3 次，误差为 99.9% 置信区间半宽，分配只包含托管内存：

| 操作 | 数量 | 耗时 | 托管分配 |
|---|---:|---:|---:|
| Frame | 100 | 121.0 ± 37.93 µs | 25.09 KB |
| Frame | 1,000 | 1,199.8 ± 1,747.68 µs | 229 KB |
| FrameWithMotors | 100 | 445.9 ± 386.43 µs | 53.45 KB |
| FrameWithMotors | 1,000 | 4,499.0 ± 3,235.63 µs | 510.48 KB |
| CreateAndDispose | 100 | 2,712.7 ± 209.13 µs | 1,101.56 KB |
| CreateAndDispose | 1,000 | 32,713.9 ± 29,897.79 µs | 10,983.16 KB |

早期调度基线使用无 native 刚体的替身、没有 Kinematic 管理器：100/1,000 角色分别为 134.6 ± 53.26 µs、1,212.4 ± 480.19 µs，分配 19.27/173.96 KB；与最终版本准备的对象和调度入口不同，不能作为等价前后比较。原真实服务端角色初始化会在 native 路径崩溃，因此没有有效的真实创建或 motor 基线。所有迭代低于建议的 100ms，样本少、置信区间宽，不据此判断性能改善或回归。

输入采集与意图分析职责分离另用同一 `CharacterSchedulingBenchmark.Frame` 负载、上述环境与 ShortRun 配置实测前后基线：100 角色为 136.3 ± 215.1 → 139.1 ± 155.6 µs，1,000 角色为 1,190.5 ± 1,352.5 → 1,243.8 ± 1,497.0 µs；分配分别保持 25.09 KB、229 KB。此次仅调整职责，将当前批次动作的跳跃判断移至意图分析，未增加输入容器或改变缓冲策略；不是热点优化。每项仍仅 3 次、迭代短、置信区间重叠，不能判断耗时改善或回归；该负载不代表 Unity Input System 或复杂动作批次。

参与者场景筛选变更前后，同一 Kinematic 平台负载 100 项为 125.99 ± 118.26 → 125.847 ± 54.187 µs，1,000 项为 1,213.99 ± 638.62 → 1,218.764 ± 221.017 µs，均为 568 B；该负载使用 World 直属平台和无 native 对象的刚体，不覆盖角色及未启用 Level 的运行时成本。

随后为安全处理派发中注册与注销加入快照，平台负载复测 100 项为 125.597 ± 36.183 µs、2,192 B，1,000 项为 1,251.070 ± 432.743 µs、16,592 B；每次迭代创建新管理器，因此包括快照首次扩容，后续派发复用容量而非每 tick 重新分配。复测控制器额外检查是否需要在回调内注销。第一位控制器注销末尾活跃参与者的负载分别为 126.347 ± 18.386 µs、1,247.291 ± 344.364 µs，托管分配相同；它扫描快照以使旧注册失效，不代表批量注销。测量置信区间重叠，不能判断耗时变化。

### 静态回调与共享生命周期复测

上述迁移数据对应首次迁移版本。随后将业务实体中的注册与 Dispose 逻辑收敛到 `GamePlayEntity`，并将捕获实体的回调替换为 System 缓存的静态方法。使用相同的两个真实基准，在同上环境和 ShortRun 配置下分别测量本次重构前后；每项 3 次，误差仍为 99.9% 置信区间半宽：

| 负载 | 数量 | 重构前耗时 | 重构后耗时 | 重构前分配 | 重构后分配 |
| --- | ---: | ---: | ---: | ---: | ---: |
| Mover | 0 | 3.861 ± 18.029 µs | 12.24 ± 129.76 µs | 496 B | 496 B |
| Mover | 100 | 144.209 ± 323.871 µs | 130.89 ± 99.34 µs | 16,744 B | 16,744 B |
| Mover | 1,000 | 1,354.007 ± 1,121.636 µs | 1,285.62 ± 280.16 µs | 160,744 B | 160,744 B |
| Kinematic | 0 | 5.431 ± 50.285 µs | 14.78 ± 170.74 µs | 568 B | 568 B |
| Kinematic | 100 | 119.333 ± 11.874 µs | 127.62 ± 137.28 µs | 568 B | 568 B |
| Kinematic | 1,000 | 1,200.778 ± 751.181 µs | 1,226.13 ± 857.86 µs | 568 B | 568 B |

测得每 tick 分配不变；迭代低于建议的 100ms，置信区间重叠，不能判断耗时改善或退化。本次目的是集中生命周期管理并避免每个实体注册时捕获闭包，不是减少既有移动计算的 tick 分配。实体创建和注册不在计时范围内，因此没有量化注册分配收益。源码层面的空间取舍是：每个 GamePlayEntity 在当前版本保存注册 World 和主回调引用，每个池化入口保存一个显式实体回调引用，而两个 System 各复用一个静态委托；不能把移除闭包直接等同于总内存下降。

### LogicUpdate 基准

使用 `BlissBenchMark/Benchmarks/GameplayWorldLogicBenchmark.cs` 的真实 Root、World Timer 和调度路径，在 macOS 26.6.2、Apple M1 Pro、.NET 8.0.19、BenchmarkDotNet ShortRun 下测量；每项 3 次测量，误差为 99.9% 置信区间半宽。调度每次操作为一个 tick，预热后每轮连续运行 1,000 tick；注册和注销每次操作分别处理表中的全部参与者，准备和实体创建不计时。每个参与者注册一个入口，使用四个交错优先级，回调仅计数。

这是新组件，没有此前的生产 LogicUpdate 基线；下表比较开发期间的线性列表初版与按优先级分桶、按实体实例和入口名索引并复用快照的版本。tick 初版每次复制新数组，注册和注销初版已使用可复用快照；两者的注册与注销结构相同。

| 操作 | 参与者 | 初版耗时 | 当前耗时 | 初版分配 | 当前分配 |
| --- | ---: | ---: | ---: | ---: | ---: |
| tick | 0 | 0.847 ± 0.300 µs | 0.940 ± 0.207 µs | 416 B | 472 B |
| tick | 100 | 6.273 ± 0.241 µs | 7.201 ± 0.666 µs | 1,240 B | 504 B |
| tick | 1,000 | 40.592 ± 176.251 µs | 72.477 ± 3.988 µs | 8,440 B | 504 B |
| 注册 | 100 | 170.216 ± 97.134 µs | 41.626 ± 3.955 µs | 14,160 B | 37,888 B |
| 注册 | 1,000 | 15,076.639 ± 4,573.342 µs | 436.249 ± 191.349 µs | 136,568 B | 376,784 B |
| 注销 | 100 | 70.569 ± 9.290 µs | 42.833 ± 46.258 µs | 0 B | 96 B |
| 注销 | 1,000 | 4,366.902 ± 1,384.593 µs | 390.694 ± 109.952 µs | 0 B | 96 B |

索引消除了批量注册与注销时的线性查找，但增加注册内存；快照缓冲复用减少了稳定 tick 的参与者数量相关分配。0 和 1,000 参与者的 tick 耗时区间重叠；100 参与者的两次区间没有重叠，但差距较小。BenchmarkDotNet 提示迭代耗时低于建议的 100ms，样本少且波动较大，这些结果不足以可靠判定 tick 耗时变化。结果不覆盖回调内修改、失效参与者、异常或完整场景与物理模拟，不能外推为游戏帧耗时。基准结果文件不提交。

### LogicUpdateEntry 池化

每个 World 缓存注销后的入口及其链表节点，并清除实体、入口名和回调引用。派发期间注销的入口在快照处理结束后才可复用，以避免注销后重注册使旧快照提前执行新回调；异常中断也会执行延迟回收。World 销毁时释放缓存，不跨 World 复用；缓存大小随该 World 曾经的并发入口峰值增长。

在上述环境和 ShortRun 配置下，基准预热后反复将同一批实体注销并重新注册，四个优先级均在每轮中清空；每次操作为一批实体的注销加注册，连续 100 批。以下比较池化前后实际生产路径，每项 3 次测量，误差为 99.9% 置信区间半宽：

| 操作 | 参与者 | 池化前耗时 | 池化后耗时 | 池化前分配 | 池化后分配 |
| --- | ---: | ---: | ---: | ---: | ---: |
| 重复注销及注册 | 0 | 0.018 ± 0.012 µs | 0.019 ± 0.034 µs | 0 B | 0 B |
| 重复注销及注册 | 100 | 74.625 ± 16.629 µs | 80.633 ± 40.263 µs | 22,272 B | 9,472 B |
| 重复注销及注册 | 1,000 | 298.371 ± 31.208 µs | 154.978 ± 21.488 µs | 216,624 B | 64,648 B |
| tick | 0 | 0.973 ± 0.393 µs | 0.865 ± 0.504 µs | 472 B | 472 B |
| tick | 100 | 7.159 ± 1.609 µs | 2.987 ± 1.677 µs | 504 B | 504 B |
| tick | 1,000 | 68.727 ± 1.350 µs | 23.216 ± 4.089 µs | 504 B | 504 B |

重复注册的测得分配分别减少约 57% 和 70%；剩余分配包括基准中的回调委托以及被清空后重新创建的优先级桶等，不表示所有注册分配都被消除。派发耗时变化并非池化的直接证明；两轮测量的波动、JIT 和运行环境可能影响结果。所有测量均短于 BenchmarkDotNet 建议的最短迭代时间，样本仅 3 次；不据此保证耗时改善。该基准不覆盖派发中注册、异常或 World 销毁，相关语义由生命周期测试覆盖。

## 验证范围

- 验证自定义阶段在各内建阶段的专属数值子区段内运行，并且入口只执行自己的区间。
- 验证阶段组按枚举值升序、SystemGroup 按优先级升序执行。
- 验证同一 Entity 类型的多个系统可以分布在不同 UpdateGroup 和优先级中，并各自只在对应组执行。
- 验证同一优先级的 SystemGroup 顺序不作断言；需要先后关系的系统通过不同优先级确定顺序。
- 验证同一 SystemGroup 内 Entity 按注册顺序处理，失效 EntityRef 被跳过。
- 验证 EarlyUpdate 帧边界之后注册的 Entity 到下一帧才参与所有阶段。
- 验证 `GameplayWorldUpdater` 使用相同的阶段、优先级、注册边界和失效引用规则。
- 验证没有更新系统的 Entity 不入队，以及非法 UpdateGroup 在系统注册时立即报错。
