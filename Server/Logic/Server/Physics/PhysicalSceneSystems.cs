#nullable enable
using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Bliss.Core;
using Unity.Mathematics;
using MagicPhysX;
using MemoryPack;
using static MagicPhysX.NativeMethods; 

namespace Bliss.Physics;

[EntitySystemOf(typeof(PhysicalScene))]
public static unsafe partial class PhysicalSceneSystems
{
    private const uint NO_PHYSX_ERROR = 0;
    private static readonly Action<Entity, float> simulationCallback = simulate;

    private static void Awake(this PhysicalScene self, PhysicalWorldComponent pxWorld)
    {
        self.PhysicalWorld = pxWorld;
        
        self.PxDispatcher = phys_PxDefaultCpuDispatcherCreate(1, null, PxDefaultCpuDispatcherWaitForWorkMode.WaitForWork, 0);

        var sceneDesc = PxSceneDesc_new(PxPhysics_getTolerancesScale(pxWorld.Physics));
        sceneDesc.gravity = PhysicalScene.DefaultGravity;
        sceneDesc.cpuDispatcher =  (PxCpuDispatcher*)self.PxDispatcher;
        sceneDesc.filterShader = get_default_simulation_filter_shader();
        
        self.PxScene = pxWorld.Physics->CreateSceneMut(&sceneDesc);
        
        self.ConfigureSimulationEventCallbacks(ref sceneDesc);
        self.ConfigureQueryFilterCallbacks();
    }

    /// <summary>
    /// Registers physical simulation at the owning World's final fixed-step priority.
    /// </summary>
    /// <param name="self">The physical scene to start.</param>
    /// <exception cref="InvalidOperationException">The scene is absent, disposed, has another primary entry, or its World is faulted.</exception>
    /// <exception cref="ObjectDisposedException">The owning World scheduler has been disposed.</exception>
    public static void StartUpdates(this PhysicalScene self)
    {
        if (self == null || self.IsDisposed)
        {
            throw new InvalidOperationException("A live physical scene is required to start updates.");
        }

        if (self.HasLogicUpdate(simulationCallback))
        {
            return;
        }

        self.RegisterLogicUpdate(GameplayWorldLogicPriority.PHYSICS, simulationCallback);
    }

    /// <summary>
    /// Stops the physical simulation entry if it is registered.
    /// </summary>
    /// <param name="self">The physical scene to stop.</param>
    public static void StopUpdates(this PhysicalScene self)
    {
        if (self == null || !self.HasLogicUpdate(simulationCallback))
        {
            return;
        }

        self.UnregisterLogicUpdate();
    }

    private static void Destroy(this PhysicalScene self)
    {
        self.ReleaseSimulationEventCallbacks();
        self.ReleaseQueryFilterCallbacks();
        
        PxScene_release_mut(self.PxScene);
        PxDefaultCpuDispatcher_release_mut(self.PxDispatcher);
        
        self.PxScene = null;
        self.PxDispatcher = null;
        
        self.StopUpdates();
        self.PhysicalWorld = null;
    }

    #region Simulation Events

    private static void ConfigureSimulationEventCallbacks(this PhysicalScene self, ref PxSceneDesc sceneDesc)
    {
        self.ReleaseSimulationEventCallbacks();

        self._simulationEventCallbackHandle = GCHandle.Alloc(self);
        self._simulationEventCallbackHandleAllocated = true;

        SimulationEventCallbackInfo callbackInfo = default;
        void* handlePtr = (void*)GCHandle.ToIntPtr(self._simulationEventCallbackHandle);

        callbackInfo.collision_callback = &OnContact;
        callbackInfo.collision_user_data = handlePtr;

        callbackInfo.trigger_callback = &OnTrigger;
        callbackInfo.trigger_user_data = handlePtr;

        callbackInfo.constraint_break_callback = &OnConstraintBreak;
        callbackInfo.constraint_break_user_data = handlePtr;

        callbackInfo.wake_sleep_callback = &OnWakeSleep;
        callbackInfo.wake_sleep_user_data = handlePtr;

        callbackInfo.advance_callback = &OnAdvance;
        callbackInfo.advance_user_data = handlePtr;
        
        self._simulationEventCallbackPtr = create_simulation_event_callbacks(&callbackInfo);
        if (self._simulationEventCallbackPtr is null)
        {
            Log.Error("[PhysxScene] Failed to create simulation event callbacks");
            self.ReleaseSimulationEventCallbacks();
            return;
        }
        sceneDesc.simulationEventCallback = self._simulationEventCallbackPtr;
    }
    
    private static void ReleaseSimulationEventCallbacks(this PhysicalScene self)
    {
        if (self._simulationEventCallbackPtr is not null)
        {
            destroy_simulation_event_callbacks(self._simulationEventCallbackPtr);
            self._simulationEventCallbackPtr = null;
        }

        if (self._simulationEventCallbackHandleAllocated)
        {
            self._simulationEventCallbackHandle.Free();
            self._simulationEventCallbackHandleAllocated = false;
        }
    }
    
    private static bool TryGetSceneFromUserData(void* userData, out PhysicalScene? scene)
    {
        scene = null;
        if (userData is null)
            return false;

        try
        {
            var handle = GCHandle.FromIntPtr((nint)userData);
            if (!handle.IsAllocated)
                return false;
            scene = handle.Target as PhysicalScene;
            return scene is not null;
        }
        catch
        {
            scene = null;
            return false;
        }
    }
    
    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static unsafe void OnContact(void* userData, PxContactPairHeader* pairHeader, PxContactPair* pairs, uint nbPairs)
    {
        if (userData is null || pairHeader is null || pairs is null || nbPairs == 0)
            return;

        if (!TryGetSceneFromUserData(userData, out var scene) || scene is null)
            return;
        // Intentionally no-op for now; ensures native never calls a null function pointer.
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static unsafe void OnTrigger(void* userData, PxTriggerPair* pairs, uint count)
    {
        if (userData is null || pairs is null || count == 0)
            return;

        if (!TryGetSceneFromUserData(userData, out var scene) || scene is null)
            return;

        // Intentionally no-op for now; ensures native never calls a null function pointer.
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static unsafe void OnConstraintBreak(void* userData, PxConstraintInfo* constraints, uint count)
    {
        if (userData is null || constraints is null || count == 0)
            return;

        if (!TryGetSceneFromUserData(userData, out var scene) || scene is null)
            return;

        // for (uint i = 0; i < count; i++)
        // {
        //     PxConstraintInfo info = constraints[i];
        //     if (info.constraint is null)
        //         continue;
        //
        //     // Find the PhysxJoint whose Constraint pointer matches the broken one.
        //     // The Joints dictionary is keyed by PxJoint*, so iterate to find a match.
        //     PhysxJoint? broken = null;
        //     foreach (var kvp in scene.Joints)
        //     {
        //         if (kvp.Value.Constraint == info.constraint)
        //         {
        //             broken = kvp.Value;
        //             break;
        //         }
        //     }
        //
        //     if (broken is not null)
        //         scene.NotifyConstraintBroken(broken);
        // }
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static unsafe void OnWakeSleep(void* userData, PxActor** actors, uint count, bool isWaking)
    {
        if (userData is null || actors is null || count == 0)
            return;

        if (!TryGetSceneFromUserData(userData, out var scene) || scene is null)
            return;

        // Intentionally no-op for now; ensures native never calls a null function pointer.
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static unsafe void OnAdvance(void* userData, PxRigidBody** bodyBuffer, PxTransform* poseBuffer, uint count)
    {
        if (userData is null || bodyBuffer is null || poseBuffer is null || count == 0)
            return;

        if (!TryGetSceneFromUserData(userData, out var scene) || scene is null)
            return;

        // Intentionally no-op for now; ensures native never calls a null function pointer.
    }
    #endregion

    #region Simulation Step
    private static void simulate(Entity entity, float deltaTime)
    {
        PhysicalScene self = (PhysicalScene)entity;
        LogicScene scene = self.Scene();
        if (scene.SceneType == ELogicSceneType.Level &&
            !scene.GetComponent<LevelGameWorldComponent>().IsEnabled)
        {
            return;
        }

        GameplayWorld world = self.GetLevelGameplayWorld()
            ?? throw new InvalidOperationException("The physical scene has no owning GameplayWorld.");
        try
        {
            self.stepSimulation(deltaTime);
        }
        catch (Exception failure)
        {
            world.StopWithFailure(failure);
            throw;
        }
    }

    private static void stepSimulation(this PhysicalScene self, float deltaTime)
    {
        if (self.PxScene == null)
        {
            throw new InvalidOperationException("The physical scene has no native PhysX scene.");
        }

        self.PxScene->SimulateMut(deltaTime, null, null, 0, true);
            
        uint error = NO_PHYSX_ERROR;
        if (!PxScene_fetchResults_mut(self.PxScene, true, &error) || error != NO_PHYSX_ERROR)
        {
            throw new InvalidOperationException($"PhysX fetch results failed with error code {error}.");
        }
        
    }
    #endregion
    
    #region Scene Query
    
    public static bool Raycast(this PhysicalScene self, float3 origin, float3 direction, float maxDistance, int layerMask, out GameplayRaycastHit hitInfo)
    {

        PxVec3 pxOrigin = origin.ToPxVec3();
        PxVec3 pxDirection = direction.ToPxVec3();
        
        bool hasResult = self.InnerRaycastSingle(&pxOrigin, pxDirection, maxDistance, (uint)layerMask, out PxRaycastHit hitResult);
        hitInfo = self.ConvertRaycastHitResult(hitResult);
        return hasResult;
    }
    
    public static int RaycastAllNonAlloc(this PhysicalScene self, float3 origin, float3 direction, float maxDistance, int layerMask, GameplayRaycastHit[] results)
    {
        PxVec3 pxOrigin = origin.ToPxVec3();
        PxVec3 pxDirection = direction.ToPxVec3();

        fixed (PxRaycastHit* hits = self.RaycastHitCache)
        {
            int count = self.InnerRaycastAll(&pxOrigin, pxDirection, maxDistance, (uint)layerMask, hits, (uint)self.RaycastHitCache.Length);
            int actualCount = self.ConvertRaycastHitResults(results,count);
            return actualCount;
        }
    }

    public static bool SpereCast(this PhysicalScene self, float3 origin, float radius, float3 direction, float maxDistance, int layerMask, out GameplayRaycastHit hitInfo)
    {
        PxSphereGeometry sphere = PxSphereGeometry_new(radius);
        PxTransform transform = CreatePxTransform(origin);

        bool hasResult = self.InnerSweepSingle((PxGeometry*)&sphere, &transform, direction.ToPxVec3(), maxDistance, (uint)layerMask, out PxSweepHit hitResult);
        hitInfo = self.ConvertSweepHitResult(hitResult);
        return hasResult;
    }
    
    public static int SpereCastNonAlloc(this PhysicalScene self, float3 origin, float radius, float3 direction, float maxDistance, int layerMask, GameplayRaycastHit[] results)
    {
        PxSphereGeometry sphere = PxSphereGeometry_new(radius);
        PxTransform transform = CreatePxTransform(origin);
        
        fixed (PxSweepHit* hits = self.SweepHitCache)
        {
            int count = self.InnerSweepAll((PxGeometry*)&sphere, &transform, direction.ToPxVec3(), maxDistance, (uint)layerMask, hits, (uint)self.SweepHitCache.Length);
            
            var actualCount = self.ConvertSweepHitResults(results, count);
            return actualCount;
        }
    }

    public static bool BoxCast(this PhysicalScene self, float3 origin, float3 halfExtents, float3 direction, quaternion orientation, float maxDistance, int layerMask, out GameplayRaycastHit hitInfo)
    {
        PxBoxGeometry box = PxBoxGeometry_new_1(halfExtents.ToPxVec3());
        PxTransform transform = CreatePxTransform(origin, orientation);

        bool hasResult = self.InnerSweepSingle((PxGeometry*)&box, &transform, direction.ToPxVec3(), maxDistance, (uint)layerMask, out PxSweepHit hitResult);
        hitInfo = self.ConvertSweepHitResult(hitResult);
        return hasResult;
    }

    public static int BoxCastNonAlloc(this PhysicalScene self, float3 origin, float3 halfExtents, float3 direction, quaternion orientation, float maxDistance, int layerMask, GameplayRaycastHit[] results)
    {
        PxBoxGeometry box = PxBoxGeometry_new_1(halfExtents.ToPxVec3());
        PxTransform transform = CreatePxTransform(origin, orientation);
    
        fixed (PxSweepHit* hits = self.SweepHitCache)
        {
            int count = self.InnerSweepAll((PxGeometry*)&box, &transform, direction.ToPxVec3(), maxDistance, (uint)layerMask, hits, (uint)self.SweepHitCache.Length);
        
            var actualCount = self.ConvertSweepHitResults(results, count);
            return actualCount;
        }
    }
    
    public static bool CapsuleCast(this PhysicalScene self, float3 point1, float3 point2, float radius, float3 direction, float maxDistance, int layerMask, out GameplayRaycastHit hitInfo)
    {
        float3 shapeDir = point2 - point1;
        float3 center = (point1 + point2) / 2.0f;
        float halfHeigh = math.length(shapeDir) - 2.0f * radius;
        quaternion ration = Unity.Physics.Math.FromToRotation(new float3(0, 1, 0), shapeDir);
    
        PxCapsuleGeometry capsule = PxCapsuleGeometry_new(radius, halfHeigh);
        PxTransform transform = CreatePxTransform(center, ration);

        bool hasResult = self.InnerSweepSingle((PxGeometry*)&capsule, &transform, direction.ToPxVec3(), maxDistance, (uint)layerMask, out PxSweepHit hitResult);
        hitInfo = self.ConvertSweepHitResult(hitResult);
        return hasResult;
    }
    
    public static int CapsuleCastNonAlloc(this PhysicalScene self, float3 point1, float3 point2, float radius, float3 direction, float maxDistance, int layerMask, GameplayRaycastHit[] results)
    {
        float3 shapeDir = point2 - point1;
        float3 center = (point1 + point2) / 2.0f;
        float halfHeigh = math.length(shapeDir) - 2.0f * radius;
        quaternion ration = Unity.Physics.Math.FromToRotation(new float3(0, 1, 0), shapeDir);
    
        PxCapsuleGeometry capsule = PxCapsuleGeometry_new(radius, halfHeigh);
        PxTransform transform = CreatePxTransform(center, ration);
    
        fixed (PxSweepHit* hits = self.SweepHitCache)
        {
            int count = self.InnerSweepAll((PxGeometry*)&capsule, &transform, direction.ToPxVec3(), maxDistance, (uint)layerMask, hits, (uint)self.SweepHitCache.Length);

            var actualCount = self.ConvertSweepHitResults(results, count);
            return actualCount;
        }
    }
    
    public static bool OverlapSpere(this PhysicalScene self, float3 origin, float radius, int layerMask, out Entity hitInfo)
    {
        PxSphereGeometry sphere = PxSphereGeometry_new(radius);
        PxTransform transform = CreatePxTransform(origin);

        bool hasResult = self.InnerOverlapSingle((PxGeometry*)&sphere, &transform, (uint)layerMask, out PxOverlapHit hitResult);
        hitInfo = self.ConvertOverlapHitResult(hitResult);
        return hasResult;
    }
    
    public static int OverlapSpereNonAlloc(this PhysicalScene self, float3 origin, float radius,int layerMask, Entity[] results)
    {
        PxSphereGeometry sphere = PxSphereGeometry_new(radius);
        PxTransform transform = CreatePxTransform(origin);

        fixed (PxOverlapHit* hits = self.OverlapHitCache)
        {
            int count = self.InnerOverlapAll((PxGeometry*)&sphere, &transform, (uint)layerMask, hits, (uint)self.OverlapHitCache.Length);
            self.ConvertOverlapHitResults(results, results.Length);
            return count;
        }
    }
    
    public static bool OverlapCapsule(this PhysicalScene self, float3 point1, float3 point2, float radius,  int layerMask, out Entity hitInfo)
    {
        float3 shapeDir = point2 - point1;
        float3 center = (point1 + point2) / 2.0f;
        float halfHeigh = math.length(shapeDir) - 2.0f * radius;
        quaternion ration = Unity.Physics.Math.FromToRotation(new float3(0, 1, 0), shapeDir);
    
        PxCapsuleGeometry capsule = PxCapsuleGeometry_new(radius, halfHeigh);
        PxTransform transform = CreatePxTransform(center, ration);

        bool hasResult = self.InnerOverlapSingle((PxGeometry*)&capsule, &transform, (uint)layerMask, out PxOverlapHit hitResult);
        hitInfo = self.ConvertOverlapHitResult(hitResult);
        return hasResult;
    }
    
    public static int OverlapCapsuleNonAlloc(this PhysicalScene self, float3 point1, float3 point2, float radius, int layerMask, Entity[] results)
    {
        float3 shapeDir = point2 - point1;
        float3 center = (point1 + point2) / 2.0f;
        float halfHeigh = math.length(shapeDir) - 2.0f * radius;
        quaternion ration = Unity.Physics.Math.FromToRotation(new float3(0, 1, 0), shapeDir);
    
        PxCapsuleGeometry capsule = PxCapsuleGeometry_new(radius, halfHeigh);
        PxTransform transform = CreatePxTransform(center, ration);
        fixed (PxOverlapHit* hits = self.OverlapHitCache)
        {
            int count = self.InnerOverlapAll((PxGeometry*)&capsule, &transform, (uint)layerMask, hits, (uint)self.OverlapHitCache.Length);
            self.ConvertOverlapHitResults(results, results.Length);
            return count;
        }
    }

    public static bool ComputePenetration(this PhysicalScene self, 
        GameplayCollider colliderA, float3 positionA, quaternion rotationA,
        GameplayCollider colliderB, float3 positionB, quaternion rotationB,
        out float3 direction, out float distance)
    {
        PxShape* shapeA = self.GetShapePtrByEntity(colliderA);
        PxTransform transformA = CreatePxTransform(positionA, rotationA);
        
        PxShape* shapeB = self.GetShapePtrByEntity(colliderB);
        PxTransform transformB = CreatePxTransform(positionB, rotationB);
        
        
        PxVec3 outDirection = new PxVec3();
        float outDistance = 0f;
        PxGeometryQuery_computePenetration(&outDirection, &outDistance, shapeA->GetGeometry(), &transformA, shapeB->GetGeometry(), &transformB, PxGeometryQueryFlags.SimdGuard);
        direction = new float3(outDirection.x, outDirection.y, outDirection.z);
        distance = outDistance;
        return true;
    }

    public static bool GetIgnoreLayerCollision(this PhysicalScene scene, int layer1, int layer2)
    {
        return scene.PhysicalWorld.GetIgnoreLayerCollision(layer1, layer2);
    }
    
    private static bool InnerRaycastSingle(this PhysicalScene self, 
        PxVec3* origin, PxVec3 unitDir, 
        float maxDistance, uint layerMask,
        out PxRaycastHit hitResult)
    {
        PxHitFlags outputFlags = PxHitFlags.Default;
        
        PxFilterData filterData = PxFilterData_new_2(layerMask, 1 ,0 ,0);
        PxQueryFlags queryFlags = PxQueryFlags.Dynamic | PxQueryFlags.Static | PxQueryFlags.Prefilter;
        PxQueryFilterData queryFilterData = PxQueryFilterData_new_1(&filterData, queryFlags);

        PxRaycastHit hitInfo;
        bool hasResult = self.PxScene->QueryExtRaycastSingle(origin, &unitDir, maxDistance, outputFlags, &hitInfo, &queryFilterData, self.QueryFilterCallback, null);
        hitResult = hitInfo;

        return hasResult;
    }
    
    private static int InnerRaycastAll(this PhysicalScene self, 
        PxVec3* origin, PxVec3 unitDir, 
        float maxDistance, uint layerMask,
        PxRaycastHit* hitBuffer, uint hitBufferSize)
    {
        PxHitFlags outputFlags = PxHitFlags.Default;
        
        PxFilterData filterData = PxFilterData_new_2(layerMask, 1 ,0 ,0);
        PxQueryFlags queryFlags = PxQueryFlags.Dynamic | PxQueryFlags.Static | PxQueryFlags.Prefilter;
        PxQueryFilterData queryFilterData = PxQueryFilterData_new_1(&filterData, queryFlags);

        bool blockingHit;
        int count = self.PxScene->QueryExtRaycastMultiple(origin, &unitDir, maxDistance, outputFlags, hitBuffer, hitBufferSize, &blockingHit, &queryFilterData, self.QueryFilterCallback, null);

        return count;
    }

    private static bool InnerSweepSingle(this PhysicalScene self, 
        PxGeometry* geometry,
        PxTransform* pose, PxVec3 unitDir, 
        float maxDistance, uint layerMask,
        out PxSweepHit hitResult)
    {
        PxHitFlags outputFlags = PxHitFlags.Default;
        
        PxFilterData filterData = PxFilterData_new_2(layerMask, 1 ,0 ,0);
        PxQueryFlags queryFlags = PxQueryFlags.Dynamic | PxQueryFlags.Static | PxQueryFlags.Prefilter;
        PxQueryFilterData queryFilterData = PxQueryFilterData_new_1(&filterData, queryFlags);

        PxSweepHit hitInfo;
        bool hasResult = self.PxScene->QueryExtSweepSingle(geometry, pose, &unitDir, maxDistance, outputFlags, &hitInfo, &queryFilterData, self.QueryFilterCallback, null, 0);
        hitResult = hitInfo;

        return hasResult;
    }

    private static int InnerSweepAll(this PhysicalScene self, 
        PxGeometry* geometry,
        PxTransform* pose, PxVec3 unitDir, 
        float maxDistance, uint layerMask,
        PxSweepHit* hitBuffer, uint hitBufferSize)
    {
        PxHitFlags outputFlags = PxHitFlags.Default;
        
        PxFilterData filterData = PxFilterData_new_2(layerMask, 0 ,0 ,0);
        PxQueryFlags queryFlags = PxQueryFlags.Dynamic | PxQueryFlags.Static | PxQueryFlags.Prefilter;
        PxQueryFilterData queryFilterData = PxQueryFilterData_new_1(&filterData, queryFlags);

        bool blockingHit;
        int count = self.PxScene->QueryExtSweepMultiple(geometry, pose, &unitDir, maxDistance, outputFlags, hitBuffer, hitBufferSize, &blockingHit, &queryFilterData, self.QueryFilterCallback, null, 0);
        return count;
    }
    
    private static bool InnerOverlapSingle(this PhysicalScene self, 
        PxGeometry* geometry,
        PxTransform* pose,
        uint layerMask,
        out PxOverlapHit hitResult)
    {
        PxFilterData filterData = PxFilterData_new_2(layerMask, 1 ,0 ,0);
        PxQueryFlags queryFlags = PxQueryFlags.Dynamic | PxQueryFlags.Static | PxQueryFlags.Prefilter;
        PxQueryFilterData queryFilterData = PxQueryFilterData_new_1(&filterData, queryFlags);

        PxOverlapHit hitInfo;
        bool hasResult = self.PxScene->QueryExtOverlapAny(geometry, pose, &hitInfo,  &queryFilterData, self.QueryFilterCallback);
        hitResult = hitInfo;

        if (hasResult)
        {
            Log.Info($"hit name={Marshal.PtrToStringAnsi((IntPtr)hitResult.shape->GetName())}");
        }
        return hasResult;
    }
    
    private static int InnerOverlapAll(this PhysicalScene self, 
        PxGeometry* geometry,
        PxTransform* pose,
        uint layerMask,
        PxOverlapHit* hitResult,
        uint hitResultSize)
    {
        PxFilterData filterData = PxFilterData_new_2(layerMask, 1 ,0 ,0);
        PxQueryFlags queryFlags = PxQueryFlags.Dynamic | PxQueryFlags.Static | PxQueryFlags.Prefilter;
        PxQueryFilterData queryFilterData = PxQueryFilterData_new_1(&filterData, queryFlags);

        int count = self.PxScene->QueryExtOverlapMultiple(geometry, pose, hitResult,  hitResultSize, &queryFilterData, self.QueryFilterCallback);

        return count;
    }
    
    private static GameplayRaycastHit ConvertRaycastHitResult(this PhysicalScene self, PxRaycastHit hitResult)
    {
        var hitInfo = new GameplayRaycastHit()
        {
            Collider = self.GetEntityByShapePtr(hitResult.shape),
            point = hitResult.position.ToFloat3(),
            normal = hitResult.normal.ToFloat3(),
            distance = hitResult.distance,
        };
        return hitInfo;
    }
    
    private static int ConvertRaycastHitResults(this PhysicalScene self, GameplayRaycastHit[] results, int count)
    {
        int actualCount = math.min(count, results.Length);
        for (int i = 0; i < actualCount; i++)
        {
            results[i] = new GameplayRaycastHit()
            {
                Collider = self.GetEntityByShapePtr(self.RaycastHitCache[i].shape),
                point = self.RaycastHitCache[i].position.ToFloat3(),
                normal = self.RaycastHitCache[i].normal.ToFloat3(),
                distance = self.RaycastHitCache[i].distance,
            };
        }
        return actualCount;
    }


    private static GameplayRaycastHit ConvertSweepHitResult(this PhysicalScene self, PxSweepHit hitResult)
    {
        var hitInfo = new GameplayRaycastHit()
        {
            Collider = self.GetEntityByShapePtr(hitResult.shape),
            point = hitResult.position.ToFloat3(),
            normal = hitResult.normal.ToFloat3(),
            distance = hitResult.distance,
            name = Marshal.PtrToStringAnsi((IntPtr)hitResult.shape->GetName()),
            
        };
        return hitInfo;
    }
    
    private static int ConvertSweepHitResults(this PhysicalScene self, GameplayRaycastHit[] results, int count)
    {
        int actualCount = math.min(count, results.Length);
        for (int i = 0; i < actualCount; i++)
        {
            results[i] = new GameplayRaycastHit()
            {
                Collider = self.GetEntityByShapePtr(self.SweepHitCache[i].shape),
                point = self.SweepHitCache[i].position.ToFloat3(),
                normal = self.SweepHitCache[i].normal.ToFloat3(),
                distance = self.SweepHitCache[i].distance,
                name = Marshal.PtrToStringAnsi((IntPtr)self.SweepHitCache[i].shape->GetName()),
            };
        }
        return actualCount;
    }
    
    private static Entity ConvertOverlapHitResult(this PhysicalScene self, PxOverlapHit hitResult)
    {
        return self.GetEntityByShapePtr(hitResult.shape);
    }
    
    private static int ConvertOverlapHitResults(this PhysicalScene self, Entity[] results, int count)
    {
        int actualCount = math.min(count, results.Length);
        for (int i = 0; i < actualCount; i++)
        {
            results[i] = self.GetEntityByShapePtr(self.OverlapHitCache[i].shape);
        }
        return actualCount;
    }

    private static GameplayCollider GetEntityByShapePtr(this PhysicalScene self, PxShape* shape)
    {
        return null!;
    }
    
    private static PxShape* GetShapePtrByEntity(this PhysicalScene self, GameplayCollider collider)
    {
        return null!;
    }
    
    private static void ConfigureQueryFilterCallbacks(this PhysicalScene self)
    {
        //c++ 当类函数调用所以会塞一个对象指针进来, static函数需要在第一个参数上接这个对象指针
        self.PreFilter = (void* userData, PxFilterData* filterData, PxShape* shape, PxRigidActor* actor, PxHitFlags queryFlags) =>
        {
            PxFilterData shapeFilterData = shape->GetQueryFilterData();
            if ((shapeFilterData.word0 & filterData->word0) != 0U)
            {
                if (filterData->word1 == 1)
                {
                    return PxQueryHitType.Block;
                }

                return PxQueryHitType.Touch;
            }

            return PxQueryHitType.None;
        };

        self.PostFilter = (void* userData, PxFilterData* filterData, PxQueryHit* hit) =>
        {
            return PxQueryHitType.Touch;
        };

        self.Destructor = (void* userData) => { };
        
        var reportPtr = Marshal.AllocHGlobal(Marshal.SizeOf<PxQueryFilterCallback>());
        self.QueryFilterCallback = (PxQueryFilterCallback*)reportPtr;
        self.QueryFilterCallback->vtable_ = PhysicalScene.Native.CreateVTable(self.PreFilter, self.PostFilter, self.Destructor);
    }

    private static void ReleaseQueryFilterCallbacks(this PhysicalScene self)
    {
        PhysicalScene.Native.FreeVTable(self.QueryFilterCallback->vtable_);
        Marshal.FreeHGlobal((IntPtr)self.QueryFilterCallback);
        
        self.PreFilter = null;
        self.PostFilter = null;
        self.Destructor = null;
    }
    
    #endregion
    
    public static unsafe void LoadActorFromMemoryPackFile(this PhysicalScene self, string sceneName)
    { 
        string path = Path.GetFullPath($"../ServerRes/ScenePhysicals/{sceneName}.dat");
        Log.Info($"Start Load Level[{sceneName}] Physical Data: {path}");

        if (!File.Exists(path))
        {
            Log.Info($"Not Found Level[{sceneName}] Physical Data: {path}");
            return;
        }
        
        var bin = File.ReadAllBytes(path);
        var sceneConfig = MemoryPackSerializer.Deserialize<PhysicalSceneConfig>(bin);

        if (sceneConfig == null)
        {
            Log.Error($"Failed to deserialize scene config from {path}");
            return;
        }

        var pxPhysics = self.PhysicalWorld.Physics;
        
        foreach (var materialConfig in sceneConfig.Materials)
        {
            var pxMaterial = PxPhysics_createMaterial_mut(self.PhysicalWorld.Physics,
                materialConfig.StaticFriction, materialConfig.DynamicFriction, materialConfig.Bounciness);
            
            self.PhysicalWorld.MaterialsRef.Add(materialConfig.InstanceId, (IntPtr)pxMaterial);
        }

        foreach (var actorConfig in sceneConfig.Actors)
        {
            if (actorConfig.Shapes == null || actorConfig.Shapes.Count == 0)
            {
                continue;
            }
            
            var position = actorConfig.Location.ToPxVec3();
            var rotation = actorConfig.Rotation.ToPxQuat();
            var transform = PxTransform_new_5(&position, &rotation);
            
            PxRigidBody* pxRigidBody;
            if (actorConfig.IsStatic)
            {
                pxRigidBody = (PxRigidBody*)PxPhysics_createRigidStatic_mut(pxPhysics, &transform);
            }
            else
            {
                pxRigidBody = (PxRigidBody*)PxPhysics_createRigidDynamic_mut(pxPhysics, &transform);
                pxRigidBody->SetMassMut(1f);
            }
            var namePtr = Marshal.StringToHGlobalAnsi(actorConfig.Name);
            PxActor_setName_mut((PxActor*)pxRigidBody, (byte*)namePtr);
            PxRigidActor_setGlobalPose_mut((PxRigidActor*)pxRigidBody, &transform, false);
            var gameplayRigidbody = self.__CreateGameplayRigidbody(pxRigidBody, 0, actorConfig.Location, actorConfig.Rotation, actorConfig.IsStatic, false);


            PxShape* shape = null;
            foreach (var shapeConfig in actorConfig.Shapes)
            {
                Log.Info($"Start Load Shape[{shapeConfig.Name}]");
                // if (shapeConfig is HeightFieldConfig)
                // {
                //     Log.Info($"Skip  Shape[{shapeConfig.Name}] For Test");
                //     continue;
                // }
                switch (shapeConfig)
                {
                    case BoxColliderConfig boxColliderConfig:
                        shape = self.CreateBoxCollider(pxPhysics, boxColliderConfig);
                        break;
                    case SphereColliderConfig sphereColliderConfig:
                        shape = self.CreateSphereCollider(pxPhysics, sphereColliderConfig);
                        break;
                    case CapsuleColliderConfig capsuleColliderConfig:
                        shape = self.CreateCapsuleCollider(pxPhysics, capsuleColliderConfig);
                        break;
                    case MeshColliderConfig meshColliderConfig:
                        shape = self.CreateMeshCollider(pxPhysics, meshColliderConfig);
                        break;
                    case HeightFieldConfig heightFieldConfig:
                        shape = self.CreateHeightField(pxPhysics, heightFieldConfig);
                        break;
                }

                if (shape != null)
                {
                    self.__CreateGameplayCollider(gameplayRigidbody.Parent, shape, shapeConfig.IsTrigger);
                    PxRigidActor_attachShape_mut((PxRigidActor*)pxRigidBody, shape);
                }
                Log.Info($"End Load Shape[{shapeConfig.Name}]");
            }

            PxScene_addActor_mut(self.PxScene, (PxActor*)pxRigidBody, null);
        }
        
        Log.Info($"End Load Level[{sceneName}] Physical Data: {path}");
    }

    #region 从配置构建物理场景
    private static unsafe PxShape* CreateBoxCollider(this PhysicalScene self, PxPhysics* pxPhysics, BoxColliderConfig boxColliderConfig)
    {
        PxMaterial* material = (PxMaterial*)self.PhysicalWorld.MaterialsRef[boxColliderConfig.MaterialId];
        PxBoxGeometry boxGeometry = PxBoxGeometry_new_1(boxColliderConfig.HalfExtents.ToPxVec3());
        PxShapeFlags shapeFlags = GetShapeFlags(boxColliderConfig);
        
        var shape = PxPhysics_createShape_mut(pxPhysics, (PxGeometry*)&boxGeometry, material, false, shapeFlags);
        
        self.SetupPxShape(shape, boxColliderConfig);
        
        return shape;
    }
    
    private static unsafe PxShape* CreateSphereCollider(this PhysicalScene self, PxPhysics* pxPhysics, SphereColliderConfig sphereColliderConfig)
    {
        PxMaterial* material = (PxMaterial*)self.PhysicalWorld.MaterialsRef[sphereColliderConfig.MaterialId];
        PxSphereGeometry sphereGeometry = PxSphereGeometry_new(sphereColliderConfig.Radius);
        PxShapeFlags shapeFlags = GetShapeFlags(sphereColliderConfig);
        
        var shape = PxPhysics_createShape_mut(pxPhysics, (PxGeometry*)&sphereGeometry, material, false, shapeFlags);

        self.SetupPxShape(shape, sphereColliderConfig);
        
        return shape;
    }
    
    private static unsafe PxShape* CreateCapsuleCollider(this PhysicalScene self, PxPhysics* pxPhysics, CapsuleColliderConfig capsuleColliderConfig)
    {
        PxMaterial* material = (PxMaterial*)self.PhysicalWorld.MaterialsRef[capsuleColliderConfig.MaterialId];
        PxCapsuleGeometry capsuleGeometry = PxCapsuleGeometry_new(capsuleColliderConfig.Radius, capsuleColliderConfig.Height);
        PxShapeFlags shapeFlags = GetShapeFlags(capsuleColliderConfig);
        
        var shape = PxPhysics_createShape_mut(pxPhysics, (PxGeometry*)&capsuleGeometry, material, false, shapeFlags);

        self.SetupPxShape(shape, capsuleColliderConfig);
        
        return shape;
    }
    
    private static unsafe PxShape* CreateMeshCollider(this PhysicalScene self, PxPhysics* pxPhysics, MeshColliderConfig meshColliderConfig)
    {
        PxMaterial* material = (PxMaterial*)self.PhysicalWorld.MaterialsRef[meshColliderConfig.MaterialId];

        PxVec3[] meshVertices = new PxVec3[meshColliderConfig.Vertices.Length];
        for (int i = 0; i < meshColliderConfig.Vertices.Length; ++i)
        {
            meshVertices[i] = meshColliderConfig.Vertices[i].ToPxVec3();
        }
        
        fixed (PxVec3* vertices = meshVertices)
        {
            fixed (int* triangles = meshColliderConfig.Triangles)
            {
                PxTolerancesScale pxTolerancesScale = PxTolerancesScale_new(1f, 10f);
                PxCookingParams cookingParams = PxCookingParams_new(&pxTolerancesScale);

                if (meshColliderConfig.IsConvex)
                {
                    PxConvexMeshDesc convexDesc = PxConvexMeshDesc_new();
                    convexDesc.points.count = (uint)meshVertices.Length;
                    convexDesc.points.stride = (uint)sizeof(PxVec3);
                    convexDesc.points.data = vertices;
                    convexDesc.flags = PxConvexFlags.ComputeConvex;
                    
                    PxDefaultMemoryOutputStream* buf = PxDefaultMemoryOutputStream_new_alloc(
                        PxFoundation_getAllocatorCallback_mut(phys_PxGetFoundation()));
                    PxConvexMeshCookingResult result;
                    if (!phys_PxCookConvexMesh(&cookingParams, &convexDesc, (PxOutputStream*)buf, &result))
                    {
                        return null;
                    }
                    
                    PxDefaultMemoryInputData* input = PxDefaultMemoryInputData_new_alloc(PxDefaultMemoryOutputStream_getData(buf),
                        PxDefaultMemoryOutputStream_getSize(buf));
                    PxConvexMesh* pxConvexMesh =  PxPhysics_createConvexMesh_mut(pxPhysics, (PxInputStream*)input);

                    PxMeshScale meshScale = PxMeshScale_new(); 
                    PxConvexMeshGeometry convexMeshGeometry = PxConvexMeshGeometry_new(pxConvexMesh, &meshScale, PxConvexMeshGeometryFlags.TightBounds);
                    PxShapeFlags shapeFlags = GetShapeFlags(meshColliderConfig);
                    
                    var shape = PxPhysics_createShape_mut(pxPhysics, (PxGeometry*)&convexMeshGeometry, material, false, shapeFlags);
                    self.SetupPxShape(shape, meshColliderConfig);
                    return shape;
                }
                else
                {
                    PxTriangleMeshDesc triangleDesc = PxTriangleMeshDesc_new();
                    triangleDesc.points.count = (uint)meshVertices.Length;
                    triangleDesc.points.stride = (uint)sizeof(PxVec3);
                    triangleDesc.points.data = vertices;

                    triangleDesc.triangles.count = (uint)(meshColliderConfig.Triangles.Length / 3);
                    triangleDesc.triangles.stride = 3 * sizeof(UInt32);
                    triangleDesc.triangles.data = triangles;
                        
                    PxDefaultMemoryOutputStream* writeBuffer = PxDefaultMemoryOutputStream_new_alloc(
                        PxFoundation_getAllocatorCallback_mut(phys_PxGetFoundation()));
                    PxTriangleMeshCookingResult result;
                    if (!phys_PxCookTriangleMesh(&cookingParams, &triangleDesc, (PxOutputStream*)writeBuffer, &result))
                    {
                        return null;
                    }
                    PxDefaultMemoryInputData* readBuffer = PxDefaultMemoryInputData_new_alloc(PxDefaultMemoryOutputStream_getData(writeBuffer),
                        PxDefaultMemoryOutputStream_getSize(writeBuffer));
                    PxTriangleMesh* triangleMesh = PxPhysics_createTriangleMesh_mut(pxPhysics, (PxInputStream*)readBuffer);
                    
                    PxMeshScale meshScale = PxMeshScale_new(); 
                    PxTriangleMeshGeometry triangleMeshGeometry = PxTriangleMeshGeometry_new(triangleMesh, &meshScale, PxMeshGeometryFlags.TightBounds);
                    
                    PxShapeFlags shapeFlags = GetShapeFlags(meshColliderConfig);
                    var  shape = PxPhysics_createShape_mut(pxPhysics, (PxGeometry*)&triangleMeshGeometry, material, false, shapeFlags);
                    
                    self.SetupPxShape(shape, meshColliderConfig);
                    return shape;
                }
            }
        }
    }
    
    private static unsafe PxShape* CreateHeightField(this PhysicalScene self, PxPhysics* pxPhysics, HeightFieldConfig heightFieldConfig)
    {
        PxMaterial* material = (PxMaterial*)self.PhysicalWorld.MaterialsRef[heightFieldConfig.MaterialId];
        
        int width = heightFieldConfig.Width;
        int height = heightFieldConfig.Height;
        
        PxHeightFieldDesc hfDesc = PxHeightFieldDesc_new();
        hfDesc.format = PxHeightFieldFormat.S16Tm;
        hfDesc.nbColumns          = (uint)heightFieldConfig.Height;
        hfDesc.nbRows             = (uint)heightFieldConfig.Width;
        hfDesc.convexEdgeThreshold = 4;
        hfDesc.samples.stride = (uint)sizeof(PxHeightFieldSample);

        PxHeightFieldSample[] pxSamples = new PxHeightFieldSample[width * height];

        PxHeightFieldSample sample;
        PxHeightFieldSample_setTessFlag_mut(&sample);
        for (int i = 0; i < width * height; i++)
        {
            sample.height = heightFieldConfig.Heights[i];
            pxSamples[i] = sample;
        }
        
        float scaleX = heightFieldConfig.Scale.x;
        float scaleY = heightFieldConfig.Scale.y;
        float scaleZ = heightFieldConfig.Scale.z;
        
        fixed (PxHeightFieldSample* pxSamplesPtr = pxSamples)
        {
            hfDesc.samples.data = pxSamplesPtr;
                
            PxDefaultMemoryOutputStream* writeBuffer = PxDefaultMemoryOutputStream_new_alloc(
                PxFoundation_getAllocatorCallback_mut(phys_PxGetFoundation()));
  
            if (!phys_PxCookHeightField(&hfDesc, (PxOutputStream*)writeBuffer))
            {
                return null;
            }
            PxDefaultMemoryInputData* readBuffer = PxDefaultMemoryInputData_new_alloc(PxDefaultMemoryOutputStream_getData(writeBuffer),
                PxDefaultMemoryOutputStream_getSize(writeBuffer));
                
            PxHeightField* heightField = PxPhysics_createHeightField_mut(pxPhysics, (PxInputStream*)readBuffer);
            PxHeightFieldGeometry heightFieldGeometry = PxHeightFieldGeometry_new(heightField, PxMeshGeometryFlags.TightBounds, scaleY, scaleX, scaleZ);
                
            PxShapeFlags shapeFlags = GetShapeFlags(heightFieldConfig);
            PxShape* shape = PxPhysics_createShape_mut(pxPhysics, (PxGeometry*)&heightFieldGeometry, material, false, shapeFlags);

            self.SetupPxShape(shape, heightFieldConfig);
            return shape;
        }
    }

    private static unsafe void SetupPxShape(this PhysicalScene self, PxShape* shape, PhysicalShapeConfig  shapeConfig)
    {
        var namePtr = Marshal.StringToHGlobalAnsi(shapeConfig.Name);
        var transform = CreatePxTransform(shapeConfig.Location, shapeConfig.Rotation);
        PxShape_setLocalPose_mut(shape, &transform);
        PxShape_setName_mut(shape,  (byte*)namePtr);
        PxShape_setContactOffset_mut(shape, shapeConfig.ContactOffset);
        SetShapeQueryFilterData(shape, shapeConfig);
    }
    
    private static PxShapeFlags GetShapeFlags(PhysicalShapeConfig collider)
    {
        return GetShapeFlags(collider.IsTrigger);
    }
    
    private static PxShapeFlags GetShapeFlags(bool isTrigger)
    {
        PxShapeFlags shapeFlags = PxShapeFlags.Visualization | PxShapeFlags.SceneQueryShape;
        if (isTrigger)
        {
            shapeFlags |= PxShapeFlags.TriggerShape;
        }
        else
        {
            shapeFlags |= PxShapeFlags.SimulationShape;
        }
        return shapeFlags;
    }
        
    private static void SetShapeQueryFilterData(PxShape* shape, PhysicalShapeConfig collider)
    {
        SetShapeQueryFilterData(shape, collider.Layer, collider.IsTrigger);
    }
    
    private static void SetShapeQueryFilterData(PxShape* shape, int layer, bool isTrigger)
    {
        //PxFilterData data = PxFilterData_new_2((uint)(1 << collider.Layer), collider.IsTrigger ? 1U : 0U, 0U, 0U);
        PxFilterData data = default;
        data.word0 = (uint)(1 << layer);
        data.word1 = isTrigger ? 1U : 0U;
        data.word2 = 0;
        data.word3 = 0;
        PxShape_setQueryFilterData_mut(shape, &data);
    }
    #endregion
    
    public static Entity CreateGameplayRigidbodyComponent(this PhysicalScene self,
        int layer, float3 position, quaternion rotation,
        bool isStatic = false, bool isKinematic = false)
    {
        if (isStatic)
        {
            isKinematic = false;
        }

        var entity = self.AddChild<Entity>();
        entity.AddComponent<GameplayLayer, int>(layer);
        
        var transformComponent = entity.AddComponent<TransformComponent>();
        transformComponent.SetPositionAndRotation(position, rotation);
        
        self.AddGameplayRigidbodyComponent(entity, isStatic, isKinematic);
        return entity;
    }
    
    public static void AddGameplayRigidbodyComponent(this PhysicalScene self, Entity entity, bool isStatic = false, bool isKinematic = false)
    {
        if (isStatic)
        {
            isKinematic = false;
        }
        var transform = entity.GetComponent<TransformComponent>().Transform;
        var position = transform.Location.ToPxVec3();
        var rotation = transform.Rotation.ToPxQuat();
        var pxTransform = PxTransform_new_5(&position, &rotation);
        
        var pxPhysics = self.PhysicalWorld.Physics;
            
        PxRigidBody* pxRigidBody;
        if (isStatic)
        {
            pxRigidBody = (PxRigidBody*)PxPhysics_createRigidStatic_mut(pxPhysics, &pxTransform);
        }
        else
        {
            pxRigidBody = (PxRigidBody*)PxPhysics_createRigidDynamic_mut(pxPhysics, &pxTransform);
            pxRigidBody->SetMassMut(1f);
        }
        
        PxRigidActor_setGlobalPose_mut((PxRigidActor*)pxRigidBody, &pxTransform, false);
        PxScene_addActor_mut(self.PxScene, (PxActor*)pxRigidBody, null);
        
        self.__CreateGameplayRigidbody(pxRigidBody, 0, transform.Location, transform.Rotation, isStatic, isKinematic);
    }

    public static Entity CreateGameplayBoxCollider(this PhysicalScene self,
        int layer, float3 position, quaternion rotation,
        ref BoxColliderInitParams initParams)
    {
        var entity = self.AddChild<Entity>();
        entity.AddComponent<GameplayLayer, int>(layer);
        
        var transformComponent = entity.AddComponent<TransformComponent>();
        transformComponent.SetPositionAndRotation(position, rotation);
        
        self.AddGameplayBoxCollider(entity, layer, ref initParams);
        return entity;
    }

    public static void AddGameplayBoxCollider(this PhysicalScene self, Entity entity, int layer, ref BoxColliderInitParams initParams)
    {
        var halfExtents = initParams.Size / 2.0f;
        var transform = CreatePxTransform(float3.zero, quaternion.identity);
        PxBoxGeometry boxGeometry = PxBoxGeometry_new_1(halfExtents.ToPxVec3());
        PxShapeFlags shapeFlags = GetShapeFlags(initParams.IsTrigger);
        
        var shape = PxPhysics_createShape_mut(self.PhysicalWorld.Physics, (PxGeometry*)&boxGeometry, null, false, shapeFlags);
        PxShape_setLocalPose_mut(shape, &transform);
        PxShape_setContactOffset_mut(shape, 0);
        SetShapeQueryFilterData(shape, layer, initParams.IsTrigger);

        initParams.PxShape = shape;
        self.__CreateGameplayCollider(entity, shape, initParams.IsTrigger);
    }
    

    public static void RemoveGameplayRigidbody(this PhysicalScene self, GameplayRigidbody gameplayRigidbody)
    {
        self.RigidbodyDict.RemoveByValue(gameplayRigidbody);
    }
    
    public static void RemoveGameplayCollider(this PhysicalScene self, GameplayCollider gameplayCollider)
    {
        self.ColliderDict.RemoveByValue(gameplayCollider);
    }
    
    private static PxTransform CreatePxTransform(float3 origin)
    {
        PxVec3 pxOrigin = origin.ToPxVec3();
        return PxTransform_new_1(&pxOrigin);
    }
    
    private static PxTransform CreatePxTransform(float3 origin, quaternion rotation)
    {
        PxVec3 pxOrigin = origin.ToPxVec3();
        PxQuat pxRRotation = rotation.ToPxQuat();
        return PxTransform_new_5(&pxOrigin, &pxRRotation);
    }

    private static GameplayRigidbody __CreateGameplayRigidbody(this PhysicalScene self, PxRigidBody* actor,
        int layer, float3 position, quaternion rotation,
        bool isStatic, bool isKinematic)
    {
        var entity = self.AddChild<SceneActor>();
        entity.AddComponent<GameplayLayer, int>(layer);
        var transform = entity.AddComponent<TransformComponent>();
        transform.SetPositionAndRotation(position, rotation);
        
        var gameplayRigidbody = entity.AddComponent<GameplayRigidbody, GameplayRigidbodyInitParams>(new GameplayRigidbodyInitParams()
        {
            IsStatic = isStatic,
            IsKinematic = isKinematic,
            RigidBodyPtr = actor,
        });
        self.RigidbodyDict.Add((IntPtr)actor,  gameplayRigidbody);

        return gameplayRigidbody;
    }
    
    private static GameplayCollider __CreateGameplayCollider(this PhysicalScene self, Entity entity, PxShape* shape, bool isTrigger)
    {
        GameplayCollider gameplayCollider = null!;
        var pxGeometry = shape->GetGeometry();
        if ((PxSphereGeometry*)pxGeometry != null)
        {
            gameplayCollider = entity.AddChild<GameplaySphereCollider, SphereColliderInitParams>(new SphereColliderInitParams()
            {
                PxShape = shape,
                IsTrigger = isTrigger,
                Center = shape->GetLocalPose().p.ToFloat3(),
                Radius = ((PxSphereGeometry*)pxGeometry)->radius,
            });
        }

        if ((PxBoxGeometry*)pxGeometry != null)
        {
            gameplayCollider = entity.AddChild<GameplayBoxCollider, BoxColliderInitParams>(new BoxColliderInitParams()
            {
                PxShape = shape,
                IsTrigger = isTrigger,
                Center = shape->GetLocalPose().p.ToFloat3(),
                Size = ((PxBoxGeometry*)pxGeometry)->halfExtents.ToFloat3() * 2.0f,
            });
        }
        
        if ((PxCapsuleGeometry*)pxGeometry != null)
        {
            var capsuleGeometry = (PxCapsuleGeometry*)pxGeometry;
            gameplayCollider =   entity.AddChild<GameplayCapsuleCollider, CapsuleColliderInitParams>(new CapsuleColliderInitParams()
            {
                PxShape = shape,
                IsTrigger = isTrigger,
                Center = shape->GetLocalPose().p.ToFloat3(),
                Radius = capsuleGeometry->radius,
                Height = PxTypeHelper.GetUnitCapsuleHeight(capsuleGeometry->halfHeight, capsuleGeometry->radius),
                Direction = 1,
            });
        }

        if ((PxConvexMeshGeometry*)pxGeometry != null || (PxTriangleMeshGeometry*)pxGeometry != null)
        {
            var convexMeshGeometry = (PxConvexMeshGeometry*)pxGeometry;
            gameplayCollider = entity.AddChild<GameplayMeshCollider, GameplayMeshColliderInitParams>(new GameplayMeshColliderInitParams()
            {
                PxShape = shape,
                IsTrigger = isTrigger,
                Convex = convexMeshGeometry != null,
            });
        }

        if ((PxHeightFieldGeometry*)pxGeometry != null)
        {
            gameplayCollider = entity.AddChild<GameplayHeightFieldCollider, GameplayHeightFieldColliderInitParams>(new GameplayHeightFieldColliderInitParams()
            {
                PxShape = shape,
                EnableTreeCollider = true,
            });
        }

        if (gameplayCollider != null)
        {
            self.ColliderDict.Add((IntPtr)shape, gameplayCollider);
            return gameplayCollider;
        }
        
        throw new NotImplementedException("Unsupported geometry type");
    }
}