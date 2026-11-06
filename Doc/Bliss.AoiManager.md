# Bliss.AoiManager

## 目标
实现一个通用 AOI（Area Of Interesting）管理器，用于维护实体间可见关系，支持调用方按业务需要注册不同范围、不同 Tag 的 AOI，并进行位置更新、注销与 Tag 筛选。

## 能力范围
- 注册 AOI（支持范围与 Tag）
- 更新 AOI 位置
- 注销 AOI
- 按 Tag 筛选 AOI 候选集
- 可见关系增量输出（进入可见 / 离开可见）
- 查询“本 AOI 句柄能看到的所有 AOI”
- 查询“所有看到本 AOI 句柄的 AOI”

## 范围形状（首版）
- 圆形（Circle）
- AABB（Axis-Aligned Bounding Box）

## 分层设计
### Model 层
- AOI 数据模型与索引结构
- 形状类型枚举（圆形、AABB）
- 可见关系缓存结构（Observer -> Target 集合）

### Logic 层
- AOI 管理器系统方法：
  - `Register(...) -> identifier`
  - `UpdatePosition(identifier, ...)`
  - `Unregister(identifier)`
  - `QueryByTag(...)`
  - `GetVisibleTargets(identifier, ...)`（我能看到谁）
  - `GetVisibleObservers(identifier, ...)`（谁能看到我）
  - 可见关系增量计算与事件分发

## 核心数据与索引
- 条目主表：`identifier -> aoiEntry`
- Tag 索引：`tag -> HashSet<identifier>`
- 正向可见缓存：`observerIdentifier -> HashSet<targetIdentifier>`
- 反向可见缓存：`targetIdentifier -> HashSet<observerIdentifier>`
- 空间网格索引：`(cellX, cellY, cellZ) -> HashSet<identifier>`
- 条目覆盖网格缓存：`identifier -> HashSet<cellKey>`

## 可见关系输出
采用增量事件回调模式：
- `OnEnterVisible(observerId, targetId, tag)`
- `OnLeaveVisible(observerId, targetId, tag)`

同时支持双向查询：
- `GetVisibleTargets(identifier)` 返回该 AOI 当前可见目标集合
- `GetVisibleObservers(identifier)` 返回当前可见该 AOI 的观察者集合
- 两个接口均支持可选 `tag` 过滤参数；未传 `tag` 时返回全量集合

### 视野迟滞（Hysteresis）

为避免目标在视野边界附近因轻微移动而反复触发进入/离开事件，AOI 需要支持将**进入视野阈值**与**离开视野阈值**拆开：

- 当前不可见的目标：只有进入 `EnterRange` 后才触发 `OnEnterVisible`
- 当前已可见的目标：只有超出 `LeaveRange` 后才触发 `OnLeaveVisible`
- 约束：`LeaveRange` 必须大于或等于 `EnterRange`

这会形成一个“迟滞区间”：

- 目标从外部靠近时，先跨过 enter 阈值才进入可见
- 目标进入后，只要仍位于 leave 阈值内，即使短暂回到 enter 与 leave 之间，也保持可见

该策略的主要目标是降低边界抖动、网络同步抖动或离散帧更新导致的可见事件震荡。

当前已确认该迟滞策略同时适用于 **Circle** 与 **AABB** 两种形状；两者都需要支持独立的进入阈值与离开阈值配置。

## 计算策略
- 采用固定大小 3D 网格（Grid）作为空间索引，AOI 按范围覆盖多个体素格子。
- 查询时先通过网格与 Tag 获取候选，再做 3D 精确几何判定。
- 同帧多次位置更新合并为一次重算，按差集输出进入/离开可见事件。

## 网格化位置管理与更新
1. 网格坐标
   - `cellX = floor(position.x / CELL_SIZE)`
   - `cellY = floor(position.y / CELL_SIZE)`
   - `cellZ = floor(position.z / CELL_SIZE)`
   - `CELL_SIZE` 使用集中常量定义。
2. 条目入格
   - Circle：实现层需升级为 Sphere 或等价 3D 范围语义，并生成包含 Y 轴的 3D 包围范围后映射覆盖格。
   - AABB：实现层需升级为 3D AABB 语义，并生成包含 Y 轴的 3D 包围范围后映射覆盖格。
3. 增量更新
   - 计算 `oldCoveredCells` 与 `newCoveredCells`。
   - 差集更新索引：`removeCells = old - new`，`addCells = new - old`。
   - 更新后标记实体 dirty，进入可见关系增量重算队列。
   - 当启用 enter/leave 迟滞后，条目的网格覆盖范围应以 **LeaveRange** 为准，避免“对象仍处于保留可见区，但已被候选收敛层漏掉”的情况。
4. 注销清理
   - 从所有覆盖格移除实体。
   - 清理正向/反向可见缓存与 Tag 索引。

## 计算优化策略
1. 候选收敛
   - 先网格粗筛，再 Tag 过滤，最后精确几何判定。
   - 静态对象与动态对象分桶，减少动态重算集合。
2. 重算节流
   - 脏标记 + 合帧：同实体一帧内多次移动只计算最后状态。
   - 时间片预算：每帧最多处理固定数量 dirty 实体，超出顺延。
3. 判定降本
   - Sphere 判定使用三维距离平方，避免开方。
   - 缓存半径平方、3D AABB min/max 等中间值，避免重复计算。
   - 当采用迟滞策略时，根据“目标当前是否已在可见集合内”切换使用 enter 判定或 leave 判定。
4. 数据一致性
   - 所有 enter/leave 同步更新双向可见索引，保证查询结果一致。

## 网格分块与十字链表方案对比

当前 AOI 管理器采用**固定网格分块**作为空间索引基础方案，而不是十字链表。两者对比如下。

| 方案 | 优点 | 缺点 | 适合场景 |
| --- | --- | --- | --- |
| **网格分块** | 实现简单；天然支持 3D；容易与 Tag 过滤组合；容易做脏标记合帧、分帧刷新；支持不同范围和不同形状（Circle/AABB）更自然 | 依赖 `CELL_SIZE` 选型；大范围 AOI 会覆盖较多格子；热点区域可能出现单格过密；跨格移动时有索引更新成本 | 通用 AOI 组件、多业务复用、3D 场景、范围差异较大的系统 |
| **十字链表** | 不依赖固定网格；对连续移动对象的边界变化比较敏感；在规则单一时可较高效地收敛候选集 | 实现复杂；插入、删除、移动维护成本高；扩展到 3D 后复杂度明显上升；与 Tag、不同范围、Circle/AABB 混合后逻辑复杂度高；调试和维护成本高 | 大量动态对象、规则较单一、主要按距离判定、偏 2D/平面 AOI 的场景 |

### 当前方案选择理由
1. **更通用**
   - AOI 管理器需要支持不同 Tag、不同范围、不同形状（Circle/AABB）。
   - 网格分块适合作为统一候选收敛层，再叠加 Tag 过滤和几何精判。
   - 十字链表更适合规则相对单一的 AOI，不适合作为当前通用组件的基础实现。

2. **更适合 3D**
   - 当前 AOI 已明确要求空间映射和精确判定都支持 3D。
   - 网格索引只需要从 `(cellX, cellZ)` 扩展为 `(cellX, cellY, cellZ)`。
   - 十字链表在 3D 下需要维护三轴有序关系，更新、查询和一致性维护都会明显复杂化。

3. **更容易与现有优化策略组合**
   - 当前 AOI 优化策略为：**网格粗筛 + Tag 过滤 + 几何精判 + 脏标记合帧 + 时间片预算**。
   - 网格分块天然适合作为这套优化链路中的第一层候选过滤。
   - 十字链表虽然可能在特定场景下减少部分候选扫描，但和当前组合策略的适配成本更高。

4. **更利于工程落地和维护**
   - 网格分块的数据结构更直接，便于调试、单测和后续扩展。
   - 更容易接入 `MapMessageHelper` 广播路径和 Unit 生命周期管理。
   - 对通用基础模块而言，稳定性、可维护性和可扩展性优先于局部极限性能。

### 十字链表的适用边界

如果后续业务场景满足以下条件，可以再评估是否引入十字链表或类似有序结构做专项优化：

- 对象数量极大；
- 对象移动非常频繁；
- AOI 规则高度统一（例如几乎全是同半径圆形范围）；
- 主要运行在 2D 或近似平面场景；
- 性能瓶颈已经明确落在候选收敛阶段，而不是广播、序列化或其他逻辑链路。

在当前阶段，**网格分块仍然是更合适的基础方案**；十字链表更适合作为未来面向特定热点场景的增强优化手段，而不是默认实现。

## 已确认
- 3D 升级不仅作用于空间索引，也作用于精确几何判定。

## Map 接入点
- `MapMessageHelper` 增加 AOI 广播接口（Tag + 距离/形状筛选）。
- 保留当前全量广播接口作为回退路径。
- 生命周期挂接：
  - Map RootInit 挂载 AOI 管理组件
  - Unit 上线/转图注册 AOI
  - Unit 移动更新 AOI 位置
  - Unit 下线/销毁注销 AOI

## 约束
- 遵循仓库命名规范与无魔法值约束。
- 参数非法和状态冲突采用显式错误/日志，不做静默吞错。
- AOI 层不绑定具体业务实体类型，仅使用通用实体标识、位置、Tag、范围。

## 同步维护规则
当 AOI 方案发生变更（目标、接口、形状支持、接入点、约束）时，需同步更新：
1. session 计划文件：`plan.md`
2. 本文档：`Doc/Bliss.AoiManager.md`
