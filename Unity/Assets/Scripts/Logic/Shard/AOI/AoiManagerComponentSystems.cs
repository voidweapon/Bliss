using System;
using System.Collections.Generic;
using Bliss.Base.Collections;
using Bliss.Core;
using Unity.Mathematics;

namespace Bliss
{
    /// <summary>
    /// AOI 管理系统：
    /// 1. 维护内部句柄、网格/Tag 索引
    /// 2. 维护双向可见关系
    /// 3. 提供注册、更新、注销与查询接口
    /// </summary>
    [EntitySystemOf(typeof(AoiManagerComponent))]
    public static partial class AoiManagerComponentSystems
    {
        private static void Awake(this AoiManagerComponent self)
        {
        }

        private static void Destroy(this AoiManagerComponent self)
        {
            self.Entries.Clear();
            self.TagToEntityIds.Clear();
            self.CellToEntityIds.Clear();
            self.EntityToCellKeys.Clear();
            self.VisibleTargetsByObserver.Clear();
            self.VisibleObserversByTarget.Clear();
            self.DirtyEntityIds.Clear();
            self.IdentifierGenerator = 0;
            self.OnEnterVisible = null;
            self.OnLeaveVisible = null;
        }

        /// <summary>
        /// 设置可见关系增量回调。
        /// </summary>
        public static void SetVisibilityCallbacks(
            this AoiManagerComponent self,
            Action<long, long, int> onEnterVisible,
            Action<long, long, int> onLeaveVisible)
        {
            self.OnEnterVisible = onEnterVisible;
            self.OnLeaveVisible = onLeaveVisible;
        }

        /// <summary>
        /// 注册 AOI 条目并立即触发首次可见关系计算。
        /// 返回值是 AOI 内部句柄，后续更新/注销必须使用该句柄。
        /// </summary>
        public static long Register(this AoiManagerComponent self, AoiRegisterInfo registerInfo)
        {
            ValidateRegisterInfo(registerInfo);

            long identifier = self.GenerateIdentifier();

            var entry = new AoiEntry
            {
                Identifier = identifier,
                EntityRef = registerInfo.EntityRef,
                Tag = registerInfo.Tag,
                ShapeType = registerInfo.ShapeType,
                Position = registerInfo.Position,
                EnterRadius = registerInfo.EnterRadius,
                LeaveRadius = registerInfo.LeaveRadius,
                EnterHalfExtents = registerInfo.EnterHalfExtents,
                LeaveHalfExtents = registerInfo.LeaveHalfExtents,
                Version = AoiConstants.AOI_INITIAL_VERSION,
            };
            RefreshGeometryCache(entry);

            self.Entries.Add(entry.Identifier, entry);
            GetOrCreateTagSet(self, entry.Tag).Add(entry.Identifier);
            using var affectedEntityIds = HashComponent<long>.Create();
            affectedEntityIds.Add(entry.Identifier);
            ReindexEntry(self, entry, affectedEntityIds);
            MarkDirtyBatch(self, affectedEntityIds);
            self.RefreshDirtyRelations();
            return identifier;
        }

        /// <summary>
        /// 更新 AOI 中心位置并增量刷新受影响可见关系。
        /// </summary>
        public static void UpdatePosition(this AoiManagerComponent self, long identifier, float3 newPosition)
        {
            var entry = GetEntryOrThrow(self, identifier);
            if (entry.Position.Equals(newPosition))
            {
                return;
            }

            using var affectedEntityIds = HashComponent<long>.Create();
            affectedEntityIds.Add(identifier);
            CollectEntitiesInEntryCells(self, identifier, affectedEntityIds);

            entry.Position = newPosition;
            entry.Version++;
            RefreshGeometryCache(entry);
            ReindexEntry(self, entry, affectedEntityIds);
            MarkDirtyBatch(self, affectedEntityIds);
            self.RefreshDirtyRelations();
        }

        /// <summary>
        /// 更新 AOI 范围参数（进入/离开圆半径或进入/离开 AABB 半尺寸）。
        /// </summary>
        public static void UpdateRange(
            this AoiManagerComponent self,
            long identifier,
            float enterRadius,
            float leaveRadius,
            float3 enterHalfExtents,
            float3 leaveHalfExtents)
        {
            var entry = GetEntryOrThrow(self, identifier);
            entry.EnterRadius = enterRadius;
            entry.LeaveRadius = leaveRadius;
            entry.EnterHalfExtents = enterHalfExtents;
            entry.LeaveHalfExtents = leaveHalfExtents;
            entry.Version++;
            RefreshGeometryCache(entry);

            using var affectedEntityIds = HashComponent<long>.Create();
            affectedEntityIds.Add(identifier);
            ReindexEntry(self, entry, affectedEntityIds);
            MarkDirtyBatch(self, affectedEntityIds);
            self.RefreshDirtyRelations();
        }

        /// <summary>
        /// 更新 AOI 形状并刷新几何缓存与索引。
        /// </summary>
        public static void UpdateShape(
            this AoiManagerComponent self,
            long identifier,
            EAoiShapeType shapeType,
            float enterRadius,
            float leaveRadius,
            float3 enterHalfExtents,
            float3 leaveHalfExtents)
        {
            var entry = GetEntryOrThrow(self, identifier);
            entry.ShapeType = shapeType;
            entry.EnterRadius = enterRadius;
            entry.LeaveRadius = leaveRadius;
            entry.EnterHalfExtents = enterHalfExtents;
            entry.LeaveHalfExtents = leaveHalfExtents;
            entry.Version++;
            RefreshGeometryCache(entry);

            using var affectedEntityIds = HashComponent<long>.Create();
            affectedEntityIds.Add(identifier);
            ReindexEntry(self, entry, affectedEntityIds);
            MarkDirtyBatch(self, affectedEntityIds);
            self.RefreshDirtyRelations();
        }

        /// <summary>
        /// 更新条目 Tag，并重新计算邻域内受影响关系。
        /// </summary>
        public static void UpdateTag(this AoiManagerComponent self, long identifier, int newTag)
        {
            var entry = GetEntryOrThrow(self, identifier);
            if (entry.Tag == newTag)
            {
                return;
            }

            if (self.TagToEntityIds.TryGetValue(entry.Tag, out var oldTagSet))
            {
                oldTagSet.Remove(identifier);
                if (oldTagSet.Count == 0)
                {
                    self.TagToEntityIds.Remove(entry.Tag);
                }
            }

            entry.Tag = newTag;
            entry.Version++;
            GetOrCreateTagSet(self, newTag).Add(identifier);

            using var affectedEntityIds = HashComponent<long>.Create();
            affectedEntityIds.Add(identifier);
            CollectEntitiesInEntryCells(self, identifier, affectedEntityIds);
            MarkDirtyBatch(self, affectedEntityIds);
            self.RefreshDirtyRelations();
        }

        /// <summary>
        /// 注销 AOI 条目并清理所有索引与双向可见关系。
        /// </summary>
        public static void Unregister(this AoiManagerComponent self, long identifier)
        {
            var entry = GetEntryOrThrow(self, identifier);

            RemoveTagIndex(self, identifier, entry.Tag);
            RemoveSpatialIndex(self, identifier);
            RemoveAllVisibilityRelations(self, identifier, entry.Tag);

            self.Entries.Remove(identifier);
            self.DirtyEntityIds.Remove(identifier);
        }

        /// <summary>
        /// 按 Tag 查询已注册 AOI 句柄。
        /// </summary>
        public static List<long> QueryByTag(this AoiManagerComponent self, int tag)
        {
            var entityIds = new List<long>();
            self.QueryByTag(tag, entityIds);
            return entityIds;
        }
        
        /// <summary>
        /// 按 Tag 查询已注册 AOI 句柄。
        /// </summary>
        public static void QueryByTag(this AoiManagerComponent self, int tag, List<long> result)
        {
            result.Clear();
            if (!self.TagToEntityIds.TryGetValue(tag, out var entityIdSet))
            {
                return;
            }

            foreach (long entityId in entityIdSet)
            {
                result.Add(entityId);
            }
        }

        /// <summary>
        /// 查询某 AOI 句柄当前能看到的目标句柄集合。
        /// </summary>
        public static void GetVisibleTargets(this AoiManagerComponent self, long identifier, List<long> result, int tagFilter = AoiConstants.AOI_TAG_FILTER_ALL)
        {
            result.Clear();
            if (!self.VisibleTargetsByObserver.TryGetValue(identifier, out var targetSet))
            {
                return;
            }

            FilterByTag(self, targetSet, tagFilter, result);
        }

        /// <summary>
        /// 查询当前能看到某 AOI 句柄的观察者句柄集合。
        /// </summary>
        public static void GetVisibleObservers(this AoiManagerComponent self, long identifier, List<long> result, int tagFilter = AoiConstants.AOI_TAG_FILTER_ALL)
        {
            result.Clear();
            if (!self.VisibleObserversByTarget.TryGetValue(identifier, out var observerSet))
            {
                return;
            }

            FilterByTag(self, observerSet, tagFilter, result);
        }

        /// <summary>
        /// 刷新脏集合中的 AOI 句柄可见关系。
        /// </summary>
        public static void RefreshDirtyRelations(this AoiManagerComponent self)
        {
            if (self.DirtyEntityIds.Count == 0)
            {
                return;
            }

            var dirtyEntityIds = new List<long>(self.DirtyEntityIds);
            self.DirtyEntityIds.Clear();

            foreach (long observerId in dirtyEntityIds)
            {
                if (!self.Entries.TryGetValue(observerId, out var observerEntry))
                {
                    continue;
                }

                RecalculateObserverVisibleTargets(self, observerEntry);
            }
        }

        /// <summary>
        /// 对单个观察者句柄重算可见目标，并输出进入/离开增量。
        /// 同时维护 observer->target 与 target->observer 双向一致性。
        /// </summary>
        private static void RecalculateObserverVisibleTargets(AoiManagerComponent self, AoiEntry observerEntry)
        {
            using var newVisibleTargetIds = HashComponent<long>.Create();
            using var candidateTargetIds = HashComponent<long>.Create();
            GatherCandidateTargetIds(self, observerEntry.Identifier, candidateTargetIds);

            bool hasVisibleTargets = self.VisibleTargetsByObserver.TryGetValue(observerEntry.Identifier, out var oldVisibleTargetIds);

            foreach (long targetId in candidateTargetIds)
            {
                if (targetId == observerEntry.Identifier)
                {
                    continue;
                }

                if (!self.Entries.TryGetValue(targetId, out var targetEntry))
                {
                    continue;
                }

                bool isCurrentlyVisible = hasVisibleTargets && oldVisibleTargetIds.Contains(targetId);
                if (!CanSeeTarget(observerEntry, targetEntry, isCurrentlyVisible))
                {
                    continue;
                }

                newVisibleTargetIds.Add(targetId);
            }

            if (!hasVisibleTargets)
            {
                oldVisibleTargetIds = new HashSet<long>();
                self.VisibleTargetsByObserver.Add(observerEntry.Identifier, oldVisibleTargetIds);
            }

            var leavingTargetIds = new List<long>();
            foreach (long targetId in oldVisibleTargetIds)
            {
                if (!newVisibleTargetIds.Contains(targetId))
                {
                    leavingTargetIds.Add(targetId);
                }
            }

            foreach (long leavingTargetId in leavingTargetIds)
            {
                oldVisibleTargetIds.Remove(leavingTargetId);
                RemoveVisibleObserver(self, leavingTargetId, observerEntry.Identifier);
                if (self.Entries.TryGetValue(leavingTargetId, out var leavingTargetEntry))
                {
                    self.OnLeaveVisible?.Invoke(observerEntry.Identifier, leavingTargetId, leavingTargetEntry.Tag);
                }
            }

            foreach (long enteringTargetId in newVisibleTargetIds)
            {
                if (!oldVisibleTargetIds.Add(enteringTargetId))
                {
                    continue;
                }

                AddVisibleObserver(self, enteringTargetId, observerEntry.Identifier);
                if (self.Entries.TryGetValue(enteringTargetId, out var enteringTargetEntry))
                {
                    self.OnEnterVisible?.Invoke(observerEntry.Identifier, enteringTargetId, enteringTargetEntry.Tag);
                }
            }

            if (oldVisibleTargetIds.Count == 0)
            {
                self.VisibleTargetsByObserver.Remove(observerEntry.Identifier);
            }
        }

        /// <summary>
        /// 基于观察者覆盖网格收敛候选句柄集合。
        /// </summary>
        private static void GatherCandidateTargetIds(
            AoiManagerComponent self,
            long observerIdentifier,
            HashSet<long> candidateTargetIds)
        {
            if (!self.EntityToCellKeys.TryGetValue(observerIdentifier, out var coveredCells))
            {
                return;
            }

            foreach (AoiCellKey cellKey in coveredCells)
            {
                if (!self.CellToEntityIds.TryGetValue(cellKey, out var entityIds))
                {
                    continue;
                }

                foreach (long entityId in entityIds)
                {
                    candidateTargetIds.Add(entityId);
                }
            }
        }

        /// <summary>
        /// 精确几何判定。
        /// Circle 使用三维距离平方，AABB 使用三维边界包含判断。
        /// 当目标已在可见集合中时，使用 leave 阈值；否则使用 enter 阈值。
        /// </summary>
        private static bool CanSeeTarget(AoiEntry observerEntry, AoiEntry targetEntry, bool isCurrentlyVisible)
        {
            return observerEntry.ShapeType switch
            {
                EAoiShapeType.Circle => math.lengthsq(targetEntry.Position - observerEntry.Position)
                                        <= (isCurrentlyVisible ? observerEntry.LeaveRadiusSquared : observerEntry.EnterRadiusSquared),
                EAoiShapeType.Aabb => IsInsideBounds(
                    targetEntry.Position,
                    isCurrentlyVisible ? observerEntry.LeaveMinBounds : observerEntry.EnterMinBounds,
                    isCurrentlyVisible ? observerEntry.LeaveMaxBounds : observerEntry.EnterMaxBounds),
                _ => throw new InvalidOperationException($"Unsupported AOI shape: {observerEntry.ShapeType}"),
            };
        }

        private static void FilterByTag(AoiManagerComponent self, HashSet<long> sourceIds, int tagFilter, List<long> outResult)
        {
            var filteredIds = outResult;
            foreach (long sourceId in sourceIds)
            {
                if (!self.Entries.TryGetValue(sourceId, out var entry))
                {
                    continue;
                }

                if (tagFilter != AoiConstants.AOI_TAG_FILTER_ALL && entry.Tag != tagFilter)
                {
                    continue;
                }

                filteredIds.Add(sourceId);
            }
        }

        private static AoiEntry GetEntryOrThrow(AoiManagerComponent self, long identifier)
        {
            if (identifier == 0)
            {
                throw new ArgumentException($"Invalid AOI identifier: {identifier}");
            }

            if (!self.Entries.TryGetValue(identifier, out var entry))
            {
                throw new InvalidOperationException($"AOI entry not found: {identifier}");
            }

            return entry;
        }

        private static void ValidateRegisterInfo(AoiRegisterInfo registerInfo)
        {
            if (registerInfo == null)
            {
                throw new ArgumentNullException(nameof(registerInfo));
            }
            if (!registerInfo.EntityRef.Validate())
            {
                throw new ArgumentException($"Invalid AOI entity id: {registerInfo.EntityRef}");
            }
            switch (registerInfo.ShapeType)
            {
                case EAoiShapeType.Circle:
                    if (registerInfo.EnterRadius <= 0f)
                    {
                        throw new ArgumentException($"Invalid AOI enter radius: {registerInfo.EnterRadius}");
                    }
                    if (registerInfo.LeaveRadius <= 0f)
                    {
                        throw new ArgumentException($"Invalid AOI leave radius: {registerInfo.LeaveRadius}");
                    }
                    if (registerInfo.LeaveRadius < registerInfo.EnterRadius)
                    {
                        throw new ArgumentException($"AOI leave radius must be greater than or equal to enter radius: enter={registerInfo.EnterRadius}, leave={registerInfo.LeaveRadius}");
                    }
                    break;
                case EAoiShapeType.Aabb:
                    if (!IsPositiveHalfExtents(registerInfo.EnterHalfExtents))
                    {
                        throw new ArgumentException($"Invalid AOI enter half extents: {registerInfo.EnterHalfExtents}");
                    }
                    if (!IsPositiveHalfExtents(registerInfo.LeaveHalfExtents))
                    {
                        throw new ArgumentException($"Invalid AOI leave half extents: {registerInfo.LeaveHalfExtents}");
                    }
                    if (!IsGreaterThanOrEqual(registerInfo.LeaveHalfExtents, registerInfo.EnterHalfExtents))
                    {
                        throw new ArgumentException($"AOI leave half extents must be greater than or equal to enter half extents: enter={registerInfo.EnterHalfExtents}, leave={registerInfo.LeaveHalfExtents}");
                    }
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported AOI shape: {registerInfo.ShapeType}");
            }
        }

        private static HashSet<long> GetOrCreateTagSet(AoiManagerComponent self, int tag)
        {
            if (!self.TagToEntityIds.TryGetValue(tag, out var tagSet))
            {
                tagSet = new HashSet<long>();
                self.TagToEntityIds.Add(tag, tagSet);
            }

            return tagSet;
        }

        /// <summary>
        /// 校验并缓存几何中间值（进入/离开判定值与 leave 包围边界）。
        /// </summary>
        private static void RefreshGeometryCache(AoiEntry entry)
        {
            switch (entry.ShapeType)
            {
                case EAoiShapeType.Circle:
                {
                    if (entry.EnterRadius <= 0f)
                    {
                        throw new InvalidOperationException($"Invalid AOI enter radius for entry {entry.Identifier}: {entry.EnterRadius}");
                    }
                    if (entry.LeaveRadius <= 0f)
                    {
                        throw new InvalidOperationException($"Invalid AOI leave radius for entry {entry.Identifier}: {entry.LeaveRadius}");
                    }
                    if (entry.LeaveRadius < entry.EnterRadius)
                    {
                        throw new InvalidOperationException($"AOI leave radius must be greater than or equal to enter radius for entry {entry.Identifier}: enter={entry.EnterRadius}, leave={entry.LeaveRadius}");
                    }

                    entry.EnterRadiusSquared = entry.EnterRadius * entry.EnterRadius;
                    entry.LeaveRadiusSquared = entry.LeaveRadius * entry.LeaveRadius;
                    var enterExtents = new float3(entry.EnterRadius, entry.EnterRadius, entry.EnterRadius);
                    var leaveExtents = new float3(entry.LeaveRadius, entry.LeaveRadius, entry.LeaveRadius);
                    entry.EnterMinBounds = entry.Position - enterExtents;
                    entry.EnterMaxBounds = entry.Position + enterExtents;
                    entry.LeaveMinBounds = entry.Position - leaveExtents;
                    entry.LeaveMaxBounds = entry.Position + leaveExtents;
                    break;
                }
                case EAoiShapeType.Aabb:
                {
                    if (!IsPositiveHalfExtents(entry.EnterHalfExtents))
                    {
                        throw new InvalidOperationException($"Invalid AOI enter half extents for entry {entry.Identifier}: {entry.EnterHalfExtents}");
                    }
                    if (!IsPositiveHalfExtents(entry.LeaveHalfExtents))
                    {
                        throw new InvalidOperationException($"Invalid AOI leave half extents for entry {entry.Identifier}: {entry.LeaveHalfExtents}");
                    }
                    if (!IsGreaterThanOrEqual(entry.LeaveHalfExtents, entry.EnterHalfExtents))
                    {
                        throw new InvalidOperationException($"AOI leave half extents must be greater than or equal to enter half extents for entry {entry.Identifier}: enter={entry.EnterHalfExtents}, leave={entry.LeaveHalfExtents}");
                    }

                    entry.EnterRadiusSquared = 0f;
                    entry.LeaveRadiusSquared = 0f;
                    entry.EnterMinBounds = entry.Position - entry.EnterHalfExtents;
                    entry.EnterMaxBounds = entry.Position + entry.EnterHalfExtents;
                    entry.LeaveMinBounds = entry.Position - entry.LeaveHalfExtents;
                    entry.LeaveMaxBounds = entry.Position + entry.LeaveHalfExtents;
                    break;
                }
                default:
                    throw new InvalidOperationException($"Unsupported AOI shape: {entry.ShapeType}");
            }
        }

        /// <summary>
        /// 条目重建网格索引，并将旧/新覆盖区块中的实体标记为受影响对象。
        /// </summary>
        private static void ReindexEntry(AoiManagerComponent self, AoiEntry entry, HashSet<long> affectedEntityIds)
        {
            using var oldCoveredCells = HashComponent<AoiCellKey>.Create();
            if (self.EntityToCellKeys.TryGetValue(entry.Identifier, out var existingCoveredCells))
            {
                oldCoveredCells.UnionWith(existingCoveredCells);
            }

            CollectEntitiesInCells(self, oldCoveredCells, affectedEntityIds);

            var newCoveredCells = CalculateCoveredCells(entry);
            ReplaceCoveredCells(self, entry.Identifier, oldCoveredCells, newCoveredCells);
            CollectEntitiesInCells(self, newCoveredCells, affectedEntityIds);
        }

        /// <summary>
        /// 计算 AOI 包围盒覆盖到的所有网格 key。
        /// </summary>
        private static HashSet<AoiCellKey> CalculateCoveredCells(AoiEntry entry)
        {
            var coveredCells = new HashSet<AoiCellKey>();
            int minCellX = PositionToCell(entry.LeaveMinBounds.x);
            int maxCellX = PositionToCell(entry.LeaveMaxBounds.x);
            int minCellY = PositionToCell(entry.LeaveMinBounds.y);
            int maxCellY = PositionToCell(entry.LeaveMaxBounds.y);
            int minCellZ = PositionToCell(entry.LeaveMinBounds.z);
            int maxCellZ = PositionToCell(entry.LeaveMaxBounds.z);

            for (int cellX = minCellX; cellX <= maxCellX; cellX++)
            {
                for (int cellY = minCellY; cellY <= maxCellY; cellY++)
                {
                    for (int cellZ = minCellZ; cellZ <= maxCellZ; cellZ++)
                    {
                        coveredCells.Add(new AoiCellKey(cellX, cellY, cellZ));
                    }
                }
            }

            return coveredCells;
        }

        /// <summary>
        /// 增量替换网格覆盖：old-new 做移除，new-old 做添加。
        /// </summary>
        private static void ReplaceCoveredCells(
            AoiManagerComponent self,
            long identifier,
            HashSet<AoiCellKey> oldCoveredCells,
            HashSet<AoiCellKey> newCoveredCells)
        {
            foreach (AoiCellKey oldCellKey in oldCoveredCells)
            {
                if (newCoveredCells.Contains(oldCellKey))
                {
                    continue;
                }

                if (!self.CellToEntityIds.TryGetValue(oldCellKey, out var entityIdSet))
                {
                    continue;
                }

                entityIdSet.Remove(identifier);
                if (entityIdSet.Count == 0)
                {
                    self.CellToEntityIds.Remove(oldCellKey);
                }
            }

            foreach (AoiCellKey newCellKey in newCoveredCells)
            {
                if (!self.CellToEntityIds.TryGetValue(newCellKey, out var entityIdSet))
                {
                    entityIdSet = new HashSet<long>();
                    self.CellToEntityIds.Add(newCellKey, entityIdSet);
                }
                entityIdSet.Add(identifier);
            }

            self.EntityToCellKeys[identifier] = newCoveredCells;
        }

        /// <summary>
        /// 清理实体对应的全部网格索引。
        /// </summary>
        private static void RemoveSpatialIndex(AoiManagerComponent self, long identifier)
        {
            if (!self.EntityToCellKeys.TryGetValue(identifier, out var coveredCells))
            {
                return;
            }

            foreach (AoiCellKey cellKey in coveredCells)
            {
                if (!self.CellToEntityIds.TryGetValue(cellKey, out var entityIdSet))
                {
                    continue;
                }

                entityIdSet.Remove(identifier);
                if (entityIdSet.Count == 0)
                {
                    self.CellToEntityIds.Remove(cellKey);
                }
            }
            self.EntityToCellKeys.Remove(identifier);
        }

        private static void RemoveTagIndex(AoiManagerComponent self, long identifier, int tag)
        {
            if (!self.TagToEntityIds.TryGetValue(tag, out var entityIdSet))
            {
                return;
            }

            entityIdSet.Remove(identifier);
            if (entityIdSet.Count == 0)
            {
                self.TagToEntityIds.Remove(tag);
            }
        }

        /// <summary>
        /// 清理句柄作为观察者与目标时的全部可见关系，并触发离开事件。
        /// </summary>
        private static void RemoveAllVisibilityRelations(AoiManagerComponent self, long identifier, int selfTag)
        {
            if (self.VisibleTargetsByObserver.TryGetValue(identifier, out var visibleTargetIds))
            {
                foreach (long targetId in visibleTargetIds)
                {
                    RemoveVisibleObserver(self, targetId, identifier);
                    if (self.Entries.TryGetValue(targetId, out var targetEntry))
                    {
                        self.OnLeaveVisible?.Invoke(identifier, targetId, targetEntry.Tag);
                    }
                }
                self.VisibleTargetsByObserver.Remove(identifier);
            }

            if (!self.VisibleObserversByTarget.TryGetValue(identifier, out var visibleObserverIds))
            {
                return;
            }

            foreach (long observerId in visibleObserverIds)
            {
                if (!self.VisibleTargetsByObserver.TryGetValue(observerId, out var observerTargets))
                {
                    continue;
                }

                observerTargets.Remove(identifier);
                if (observerTargets.Count == 0)
                {
                    self.VisibleTargetsByObserver.Remove(observerId);
                }
                self.OnLeaveVisible?.Invoke(observerId, identifier, selfTag);
            }

            self.VisibleObserversByTarget.Remove(identifier);
        }

        private static void AddVisibleObserver(AoiManagerComponent self, long targetId, long observerId)
        {
            if (!self.VisibleObserversByTarget.TryGetValue(targetId, out var observerIds))
            {
                observerIds = new HashSet<long>();
                self.VisibleObserversByTarget.Add(targetId, observerIds);
            }

            observerIds.Add(observerId);
        }

        private static void RemoveVisibleObserver(AoiManagerComponent self, long targetId, long observerId)
        {
            if (!self.VisibleObserversByTarget.TryGetValue(targetId, out var observerIds))
            {
                return;
            }

            observerIds.Remove(observerId);
            if (observerIds.Count == 0)
            {
                self.VisibleObserversByTarget.Remove(targetId);
            }
        }

        private static void CollectEntitiesInEntryCells(AoiManagerComponent self, long identifier, HashSet<long> entityIds)
        {
            if (!self.EntityToCellKeys.TryGetValue(identifier, out var coveredCells))
            {
                return;
            }

            CollectEntitiesInCells(self, coveredCells, entityIds);
        }

        private static void CollectEntitiesInCells(AoiManagerComponent self, HashSet<AoiCellKey> cellKeys, HashSet<long> entityIds)
        {
            foreach (AoiCellKey cellKey in cellKeys)
            {
                if (!self.CellToEntityIds.TryGetValue(cellKey, out var cellEntityIds))
                {
                    continue;
                }

                foreach (long cellEntityId in cellEntityIds)
                {
                    entityIds.Add(cellEntityId);
                }
            }
        }

        /// <summary>
        /// 标记一批 AOI 句柄为 dirty，等待统一刷新。
        /// </summary>
        private static void MarkDirtyBatch(AoiManagerComponent self, HashSet<long> entityIds)
        {
            foreach (long entityId in entityIds)
            {
                self.DirtyEntityIds.Add(entityId);
            }
        }

        /// <summary>
        /// 生成新的 AOI 内部句柄。
        /// </summary>
        private static long GenerateIdentifier(this AoiManagerComponent self)
        {
            self.IdentifierGenerator++;
            if (self.IdentifierGenerator == 0)
            {
                self.IdentifierGenerator++;
            }

            return self.IdentifierGenerator;
        }

        /// <summary>
        /// 世界坐标转网格坐标。
        /// </summary>
        private static int PositionToCell(float position)
        {
            return (int)math.floor(position / AoiConstants.AOI_GRID_CELL_SIZE);
        }

        private static bool IsPositiveHalfExtents(float3 halfExtents)
        {
            return halfExtents.x > 0f && halfExtents.y > 0f && halfExtents.z > 0f;
        }

        private static bool IsGreaterThanOrEqual(float3 left, float3 right)
        {
            return left.x >= right.x && left.y >= right.y && left.z >= right.z;
        }

        private static bool IsInsideBounds(float3 position, float3 minBounds, float3 maxBounds)
        {
            return position.x >= minBounds.x
                   && position.x <= maxBounds.x
                   && position.y >= minBounds.y
                   && position.y <= maxBounds.y
                   && position.z >= minBounds.z
                   && position.z <= maxBounds.z;
        }
    }
}
