using System;
using System.Collections.Generic;
using MagicPhysX;

namespace Bliss;

public unsafe class PhysicalWorldComponent : Entity, IAwake, IDestroy
{
    public PxFoundation* Functions;
    public PxPhysics* Physics;
    public PxDefaultCpuDispatcher* Dispatcher;
    
    public Dictionary<int, IntPtr> MaterialsRef = new Dictionary<int, IntPtr>();
}