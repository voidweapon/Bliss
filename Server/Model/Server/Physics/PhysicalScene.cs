using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Bliss.Base.Collections;
using MagicPhysX;

namespace Bliss.Physics;

public unsafe partial class PhysicalScene : Entity, IAwake<PhysicalWorldComponent>, IDestroy
{
    public PxDefaultCpuDispatcher* PxDispatcher;
    public PxScene* PxScene;
    
    public PxSimulationEventCallback* _simulationEventCallbackPtr;
    public GCHandle _simulationEventCallbackHandle;
    public bool _simulationEventCallbackHandleAllocated;
    
    public PxQueryFilterCallback* QueryFilterCallback;
    public DPreFilter PreFilter;
    public DPostFilter PostFilter;
    public DDestructor Destructor;
    
    public readonly PxSweepHit[] SweepHitCache = new PxSweepHit[256];
    public readonly PxOverlapHit[] OverlapHitCache = new PxOverlapHit[256];
    public readonly PxRaycastHit[] RaycastHitCache = new PxRaycastHit[256];
    
    public static readonly PxVec3 DefaultGravity = new PxVec3(){x = 0, y = -9.8f, z = 0};

    public PhysicalWorldComponent PhysicalWorld;
    public long UpdateTimer;
    
    public DoublyDictionary<IntPtr, GameplayCollider> ColliderDict = new DoublyDictionary<IntPtr, GameplayCollider>();
    public DoublyDictionary<IntPtr, GameplayRigidbody> RigidbodyDict = new DoublyDictionary<IntPtr, GameplayRigidbody>();
}

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public unsafe delegate PxQueryHitType DPreFilter(void* userData, PxFilterData* filterData, PxShape* shape, PxRigidActor* actor, PxHitFlags queryFlags);

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public unsafe delegate PxQueryHitType DPostFilter(void* userData, PxFilterData* filterData, PxQueryHit* hit);

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public unsafe delegate void DDestructor(void* userData);