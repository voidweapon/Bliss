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
- 延迟时只产生一次 tick，并从当前 Timer 处理时刻安排下次，固定 delta 为 0.05 秒；不追补。派发开始时消费 tick，回调失败包装并传播，后续入口不运行，已消费的 tick 不重试。销毁 World 会取消 Timer、完成等待并清除待处理 tick。
- 通过 `world.LogicUpdate.Register(entity, entryName, priority, callback)` 注册；身份是活跃实体实例和入口名，重复注册报错。参与者必须属于该 World；注销使用 `Unregister(entity, entryName)`。优先级按数值升序，同优先级按注册顺序。共享顺序使用 `GameplayWorldLogicPriority`：发射点检测、角色移动、共享移动模拟、弹道检测、物理模拟；最终物理优先级保留给服务端物理模拟。
- 派发开始时确定快照；期间新增或注销后重注册的入口从下次 tick 开始，已注销、销毁、复用或脱离 World 的参与者跳过。World 销毁会中止快照内剩余入口。调用方仍应在自身销毁时主动注销。

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
