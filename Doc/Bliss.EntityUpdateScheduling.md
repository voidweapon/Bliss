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

调度器采用以下层级：[Bliss.GameplayUpdater.md](EngineeringJournal%20/Bliss.GameplayUpdater.md)

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

- 对由 `EntitySystem` 驱动的更新，`EarlyUpdate()` 开始时作为帧边界。
- 在该帧边界之前注册的 Entity 可以参与本帧调度；帧边界之后注册的 Entity 从下一帧开始参与，即使它还有尚未执行的后续阶段。
- 调度器在帧开始时确定本帧的可调度集合；帧中新增的队列项延迟到下一帧启用，避免遍历期间新增项在尚未执行的阶段意外参与本帧。
- `GameplayWorldUpdater` 接入共享调度器后遵循同一规则，以它的 `EarlyUpdate()` 调用开始作为该驱动路径的帧边界，并由 Early、Update、Late 三个入口分别调度对应区间。

## 验证范围

- 验证自定义阶段在各内建阶段的专属数值子区段内运行，并且入口只执行自己的区间。
- 验证阶段组按枚举值升序、SystemGroup 按优先级升序执行。
- 验证同一 Entity 类型的多个系统可以分布在不同 UpdateGroup 和优先级中，并各自只在对应组执行。
- 验证同一优先级的 SystemGroup 顺序不作断言；需要先后关系的系统通过不同优先级确定顺序。
- 验证同一 SystemGroup 内 Entity 按注册顺序处理，失效 EntityRef 被跳过。
- 验证 EarlyUpdate 帧边界之后注册的 Entity 到下一帧才参与所有阶段。
- 验证 `GameplayWorldUpdater` 使用相同的阶段、优先级、注册边界和失效引用规则。
- 验证没有更新系统的 Entity 不入队，以及非法 UpdateGroup 在系统注册时立即报错。
