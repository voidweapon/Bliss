using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Bliss.Core;
using Bliss.Core.EventSystem;
using Bliss.Timer;
using MemoryPack;
using MagicPhysX;
using Unity.Mathematics;
using static MagicPhysX.NativeMethods; 


namespace Bliss;

[EntitySystemOf(typeof(PhysicalWorldComponent))]
public static partial class PhysicalWorldComponentSystems
{
    private static void Awake(this PhysicalWorldComponent self)
    {
        unsafe
        {
            self.Physics = physx_create_physics(PhysicalFunction.GetFoundation());

            var sceneDesc = PxSceneDesc_new(PxPhysics_getTolerancesScale(self.Physics));
            sceneDesc.gravity = new PxVec3(){x = 0, y = -9.8f, z = 0};

            self.Dispatcher = phys_PxDefaultCpuDispatcherCreate(1, null, PxDefaultCpuDispatcherWaitForWorkMode.WaitForWork, 0);
            sceneDesc.cpuDispatcher =  (PxCpuDispatcher*)self.Dispatcher;
            sceneDesc.filterShader = get_default_simulation_filter_shader();
        }
        
    }
    
    private static void Destroy(this PhysicalWorldComponent self)
    {
        unsafe
        {
            PxDefaultCpuDispatcher_release_mut(self.Dispatcher);
            PxPhysics_release_mut(self.Physics);

            self.Dispatcher = null;
            self.Physics = null;
        }
    }

    public static bool GetIgnoreLayerCollision(this PhysicalWorldComponent self, int layer1, int layer2)
    {
        return phys_PxGetGroupCollisionFlag((ushort)layer1, (ushort)layer2);
    }

    private static void SetupColliderLayer(this PhysicalWorldComponent self, bool[] collisionTable)
    {
        int index = 0;
        for (ushort i = 0; i < 32; i++)
        {
            for (ushort j = 0; j < 32; j++)
            {
                phys_PxSetGroupCollisionFlag(i, j, collisionTable[index++]);
            }
        }
    }

}