using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using MagicPhysX;
using static MagicPhysX.NativeMethods;

namespace ScenePhysicalExport
{
    public static partial class SceneExporter
    {
        /// <summary>
        /// 功能函数，获取 hierarchy 名字
        /// </summary>
        /// <param name="go"></param>
        /// <returns></returns>
        static string GetGameObjectNameInHierarchy(Transform go)
        {
            if (go == null) return string.Empty;
            else
            {
                return GetGameObjectNameInHierarchy(go.parent) + "/" + go.name;
            }
        }
        
        static bool CanExport(GameObject go)
        {
            return go.activeInHierarchy;
        }
        
        public static bool Export(string scenePath, string outputPath)
        {
            EditorSceneManager.OpenScene(scenePath);
            Scene scene = EditorSceneManager.GetSceneByPath(scenePath);
            if (scene.IsValid())
            {
                return ExportCurrentScene(scene, outputPath);
            }
            else
            {
                return false;
            }
        }

        public static bool ExportCurrentScene(Scene scene, string outputPath)
        {
            try
            {
                _sharedMaterials.Clear();

                ExportScenePhysics(outputPath, ScanScene, scene);
   
                static string ScanScene(PxPhysicsContext physicsContext, Scene  scene)
                {
                    GameObject[] gameObjectArray = scene.GetRootGameObjects();
                    foreach (GameObject go in gameObjectArray)
                    {
                        if (CanExport(go))
                        {
                            ExportRootGameObject(physicsContext, go);
                        }
                    }

                    return scene.name;
                }
                
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Export Scene: {EditorSceneManager.GetActiveScene().path} error, error msg: {e}");
                return false;
            }
        }

        private static unsafe void ExportRootGameObject(PxPhysicsContext physicsContext, GameObject go)
        {
            List<IntPtr> shapes = CreatePxShapes(ref physicsContext, go);
            Rigidbody rigidBody = go.GetComponent<Rigidbody>();

            if (shapes.Count == 0 && rigidBody == null)
            {
                return;
            }

            PxRigidBody* pxRigidBody;
            var position = (PxVec3)go.transform.position;
            var rotation = (PxQuat)go.transform.rotation;
            var transform = PxTransform_new_5(&position, &rotation);
            if (rigidBody != null)
            {
                pxRigidBody = (PxRigidBody*)PxPhysics_createRigidDynamic_mut(physicsContext.Physics, &transform);
            }
            else
            {
                pxRigidBody = (PxRigidBody*)PxPhysics_createRigidStatic_mut(physicsContext.Physics, &transform);
            }
            PxActor_setName_mut((PxActor*)pxRigidBody, (byte*)Marshal.StringToHGlobalAnsi(go.name));
            
            foreach (PxShape* shape in shapes)
            {
                PxRigidActor_attachShape_mut((PxRigidActor*)pxRigidBody, shape);
            }

            PxBase* item = (PxBase*)pxRigidBody;
            PxCollection_add_mut(physicsContext.Collection, item, 0);
        }

        unsafe struct PxPhysicsContext
        {
            public PxPhysics* Physics;
            public PxCollection* Collection;
        }
        
        static Dictionary<PhysicsMaterial, IntPtr> _sharedMaterials = new Dictionary<PhysicsMaterial, IntPtr>();
        
        private static unsafe void ExportScenePhysics(string outputPath, Func<PxPhysicsContext, Scene, string> scanSceneFunc, Scene unityScene)
        {
            // create foundation(allocator, logging, etc...)
            var foundation = physx_create_foundation();

            // create physics system
            var physics = physx_create_physics(foundation);

            // create physics scene settings
            var sceneDesc = PxSceneDesc_new(PxPhysics_getTolerancesScale(physics));

            // you can create PhysX primitive(PxVec3, etc...) by C# struct
            sceneDesc.gravity = new PxVec3 { x = 0.0f, y = -9.81f, z = 0.0f };

            var dispatcher = phys_PxDefaultCpuDispatcherCreate(1, null, PxDefaultCpuDispatcherWaitForWorkMode.WaitForWork, 0);
            sceneDesc.cpuDispatcher = (PxCpuDispatcher*)dispatcher;
            sceneDesc.filterShader = get_default_simulation_filter_shader();

            // create physics scene
            var scene =  PxPhysics_createScene_mut(physics, &sceneDesc);
            var serializationRegistry = PxSerialization_createSerializationRegistry(physics);
            var collection = phys_PxCreateCollection();
            IntPtr pathIntPrt = default;
            try
            {
                var sceneName = scanSceneFunc(new PxPhysicsContext()
                {
                    Physics = physics,
                    Collection = collection,
                }, unityScene);
                
                string filename = outputPath + "/" + sceneName + ".xml";
                pathIntPrt = Marshal.StringToHGlobalAnsi(filename);
                PxSerialization_complete(collection, serializationRegistry, null ,false);
                
                var fileOutStream = PxDefaultFileOutputStream_new_alloc((byte*)pathIntPrt);
                if (PxDefaultFileOutputStream_isValid_mut(fileOutStream))
                {
                    uint shapeCount = PxCollection_getNbObjects(collection);
                    Debug.Log($"shape count: {shapeCount}");
                    bool result = PxSerialization_serializeCollectionToXml((PxOutputStream*)fileOutStream, collection, serializationRegistry, null, null, null);
                    if (!result)
                    {
                        throw new Exception($"PxSerialization_serializeCollectionToXml false");
                    }
                }
                else
                {
                    throw new Exception($"create PxDefaultFileOutputStream failed: {filename}");
                }
            }
            finally
            {
                // release resources
                PxCollection_release_mut(collection);
                PxSerializationRegistry_release_mut(serializationRegistry);
                PxScene_release_mut(scene);
                PxDefaultCpuDispatcher_release_mut(dispatcher);
                PxPhysics_release_mut(physics);
                Marshal.FreeHGlobal(pathIntPrt);
            }
        }

        private static unsafe List<IntPtr> CreatePxShapes(ref PxPhysicsContext physicsContext, GameObject go)
        {
            List<IntPtr> shapes = new List<IntPtr>();
            
            if (go.transform.lossyScale.x < 0 || go.transform.lossyScale.y < 0 || go.transform.lossyScale.z < 0)
            {
                Debug.LogError($" {go.name} 的transform scale存在负数, GameObject Name: {GetGameObjectNameInHierarchy(go.transform)}，请检查");
                return  shapes;
            }
            
            Transform rootTransform = go.transform.root;
            Transform goTransform = go.transform;

            BoxCollider[] boxColliders = go.GetComponents<BoxCollider>();
            foreach (BoxCollider boxCollider in boxColliders)
            {
                if (boxCollider.size.x < 0 || boxCollider.size.y < 0 || boxCollider.size.z < 0)
                {
                    Debug.LogError($"跳过BoxCollider {boxCollider.name} 因Size存在负数, GameObject Name: {GetGameObjectNameInHierarchy(boxCollider.transform)}，请检查");
                    continue;
                }

                PxShape* shape = CreateBoxCollider(physicsContext.Physics, boxCollider);
                if (shape == null)
                {
                    continue;
                }
                var localPose = CreateLocalPost(rootTransform, goTransform, boxCollider.center);
                PxShape_setLocalPose_mut(shape, &localPose);
                shapes.Add((IntPtr)shape);
            }
            
            SphereCollider[] sphereColliders = go.GetComponents<SphereCollider>();
            foreach (SphereCollider sphereCollider in sphereColliders)
            {
                if (sphereCollider.radius == 0f)
                {
                    Debug.LogError($"跳过BSphereCollider {sphereCollider.name} 因radius存在负数, GameObject Name: {GetGameObjectNameInHierarchy(sphereCollider.transform)}，请检查");
                    continue;
                }

                PxShape* shape = CreateSphereCollider(physicsContext.Physics, sphereCollider);
                if (shape == null)
                {
                    continue;
                }
                var localPose = CreateLocalPost(rootTransform, goTransform, sphereCollider.center);
                PxShape_setLocalPose_mut(shape, &localPose);
                shapes.Add((IntPtr)shape);
            }
            
            CapsuleCollider[] capsuleColliders = go.GetComponents<CapsuleCollider>();
            foreach (CapsuleCollider capsuleCollider in capsuleColliders)
            {
                if (capsuleCollider.radius == 0f || capsuleCollider.height == 0f)
                {
                    Debug.LogError($"跳过CapsuleCollider {capsuleCollider.name} 因radius/height存在负数, GameObject Name: {GetGameObjectNameInHierarchy(capsuleCollider.transform)}，请检查");
                    continue;
                }

                Quaternion baseRotation = Quaternion.identity;
                // if (capsuleCollider.direction == 0)
                // {
                //     baseRotation = Quaternion.AngleAxis(90, Vector3.forward);
                // }
                // else if (capsuleCollider.direction == 2)
                // {
                //     baseRotation = Quaternion.AngleAxis(90, Vector3.right);
                // }
                if (capsuleCollider.direction == 1)
                {
                    baseRotation = Quaternion.AngleAxis(90, Vector3.forward);
                }
                else if (capsuleCollider.direction == 2)
                {
                    baseRotation = Quaternion.AngleAxis(90, Vector3.up);
                }
                Quaternion colliderRotation = baseRotation * goTransform.rotation;
                
                PxShape* shape = CreateCapsuleCollider(physicsContext.Physics, capsuleCollider);
                if (shape == null)
                {
                    continue;
                }
                
                var position = (PxVec3)rootTransform.InverseTransformPoint(goTransform.TransformPoint(capsuleCollider.center));
                var quaternion = (PxQuat)(Quaternion.Inverse(rootTransform.rotation) * colliderRotation);
                var localPose =  PxTransform_new_5(&position, &quaternion);
                PxShape_setLocalPose_mut(shape, &localPose);
                shapes.Add((IntPtr)shape);
            }

            MeshCollider[] meshColliders = go.GetComponents<MeshCollider>();
            foreach (MeshCollider meshCollider in meshColliders)
            {
                if (meshCollider.sharedMesh == null || meshCollider.sharedMesh.vertexCount == 0)
                {
                    Debug.LogError($"跳过 MeshCollider {meshCollider.name} 因mesh丢失, GameObject Name: {GetGameObjectNameInHierarchy(meshCollider.transform)}，请检查");
                    continue;
                }
                
                PxShape* shape = CreateMeshCollider(physicsContext.Physics, meshCollider);
                if (shape == null)
                {
                    continue;
                }
                var position = (PxVec3)rootTransform.InverseTransformPoint(goTransform.position);
                var quaternion = (PxQuat)(Quaternion.Inverse(rootTransform.rotation) * goTransform.rotation);
                var localPose =  PxTransform_new_5(&position, &quaternion);
                PxShape_setLocalPose_mut(shape, &localPose);
                shapes.Add((IntPtr)shape);
            }
            
            TerrainCollider[] terrainColliders = go.GetComponents<TerrainCollider>();
            foreach (TerrainCollider terrainCollider in terrainColliders)
            {
                PxShape* shape = CreateHeightField(physicsContext.Physics, terrainCollider);
                if (shape == null)
                {
                    continue;
                }
                var position = (PxVec3)rootTransform.InverseTransformPoint(goTransform.position);
                var quaternion = (PxQuat)(Quaternion.Inverse(rootTransform.rotation) * goTransform.rotation);
                var localPose =  PxTransform_new_5(&position, &quaternion);
                PxShape_setLocalPose_mut(shape, &localPose);
                shapes.Add((IntPtr)shape);
            }
            
            for (int i = 0; i < go.transform.childCount; i++)
            {
                if (CanExport(go.transform.GetChild(i).gameObject))
                {
                    shapes.AddRange(CreatePxShapes(ref physicsContext, go.transform.GetChild(i).gameObject));
                }
            }

            return shapes;
        }

        #region Gen PxShape

        private static unsafe PxShape* CreateBoxCollider(PxPhysics* pxPhysics, BoxCollider boxCollider)
        {
            var halfExtents = PxVec3_new_3(
                boxCollider.size.x * Mathf.Abs(boxCollider.transform.lossyScale.x) / 2.0f,
                boxCollider.size.y * Mathf.Abs(boxCollider.transform.lossyScale.y) / 2.0f,
                boxCollider.size.z * Mathf.Abs(boxCollider.transform.lossyScale.z) / 2.0f);
            PxBoxGeometry boxGeometry = PxBoxGeometry_new_1(halfExtents);
            var material = CreateMaterial(pxPhysics, boxCollider.material);

            PxShapeFlags shapeFlags = GetShapeFlags(boxCollider);
            var shape = PxPhysics_createShape_mut(pxPhysics, (PxGeometry*)&boxGeometry, material, false, shapeFlags);
            PxShape_setName_mut(shape,  (byte*)Marshal.StringToHGlobalAnsi(boxCollider.name));
            PxShape_setContactOffset_mut(shape, boxCollider.contactOffset);
            SetShapeQueryFilterData(shape, boxCollider);
            return shape;
        }

        private static unsafe PxShape* CreateSphereCollider(PxPhysics* pxPhysics, SphereCollider sphereCollider)
        {
            float maxScale = Math.Max(Math.Max(Mathf.Abs(sphereCollider.transform.localScale.x),
                    Mathf.Abs(sphereCollider.transform.localScale.y)),
                Mathf.Abs(sphereCollider.transform.localScale.z));

            PxSphereGeometry sphereGeometry = PxSphereGeometry_new(sphereCollider.radius * maxScale);
            var material = CreateMaterial(pxPhysics, sphereCollider.material);
            
            PxShapeFlags shapeFlags = GetShapeFlags(sphereCollider);
            var shape = PxPhysics_createShape_mut(pxPhysics, (PxGeometry*)&sphereGeometry, material, false, shapeFlags);
            PxShape_setName_mut(shape,  (byte*)Marshal.StringToHGlobalAnsi(sphereCollider.name));
            PxShape_setContactOffset_mut(shape, sphereCollider.contactOffset);
            SetShapeQueryFilterData(shape, sphereCollider);
            return shape;
        }

        private static unsafe PxShape* CreateCapsuleCollider(PxPhysics* pxPhysics, CapsuleCollider capsuleCollider)
        {
            float maxScale = Math.Max(Math.Max(Mathf.Abs(capsuleCollider.transform.localScale.x), 
                    Mathf.Abs(capsuleCollider.transform.localScale.y)), 
                Mathf.Abs(capsuleCollider.transform.localScale.z));
            float radius = capsuleCollider.radius * maxScale;
            float height = capsuleCollider.height * capsuleCollider.transform.localScale.y / 2.0f - radius;
            
            PxCapsuleGeometry capsuleGeometry = PxCapsuleGeometry_new(radius, height);
            var material = CreateMaterial(pxPhysics, capsuleCollider.material);
            
            PxShapeFlags shapeFlags = GetShapeFlags(capsuleCollider);
            var shape = PxPhysics_createShape_mut(pxPhysics, (PxGeometry*)&capsuleGeometry, material, false, shapeFlags);
            PxShape_setName_mut(shape,  (byte*)Marshal.StringToHGlobalAnsi(capsuleCollider.name));
            PxShape_setContactOffset_mut(shape, capsuleCollider.contactOffset);
            SetShapeQueryFilterData(shape, capsuleCollider);
            return shape;
        }

        private static unsafe PxShape* CreateMeshCollider(PxPhysics* pxPhysics, MeshCollider meshCollider)
        {
            Mesh mesh = meshCollider.sharedMesh;
            Vector3 scale = meshCollider.transform.lossyScale;
            PxVec3[] meshVertices = new PxVec3[mesh.vertices.Length];
            for (int i = 0; i < mesh.vertices.Length; ++i)
            {
                Vector3 vertex = mesh.vertices[i];
                vertex.x *= scale.x;
                vertex.y *= scale.y;
                vertex.z *= scale.z;
                meshVertices[i] = vertex;
            }
            
            fixed (PxVec3* vertices = meshVertices)
            {
                fixed (int* triangles = mesh.triangles)
                {
                    PxTolerancesScale pxTolerancesScale = PxTolerancesScale_new(1f, 10f);
                    PxCookingParams cookingParams = PxCookingParams_new(&pxTolerancesScale);

                    if (meshCollider.convex)
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
                        
                        var material = CreateMaterial(pxPhysics, meshCollider.material);
                        PxShapeFlags shapeFlags = GetShapeFlags(meshCollider);
                        var shape = PxPhysics_createShape_mut(pxPhysics, (PxGeometry*)&convexMeshGeometry, material, false, shapeFlags);
                        PxShape_setName_mut(shape,  (byte*)Marshal.StringToHGlobalAnsi(meshCollider.name));
                        PxShape_setContactOffset_mut(shape, meshCollider.contactOffset);
                        SetShapeQueryFilterData(shape, meshCollider);
                        return shape;
                    }
                    else
                    {
                        PxTriangleMeshDesc triangleDesc = PxTriangleMeshDesc_new();
                        triangleDesc.points.count = (uint)meshVertices.Length;
                        triangleDesc.points.stride = (uint)sizeof(PxVec3);
                        triangleDesc.points.data = vertices;

                        triangleDesc.triangles.count = (uint)(mesh.triangles.Length / 3);
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
                        
                        var material = CreateMaterial(pxPhysics, meshCollider.material);
                        PxShapeFlags shapeFlags = GetShapeFlags(meshCollider);
                        var  shape = PxPhysics_createShape_mut(pxPhysics, (PxGeometry*)&triangleMeshGeometry, material, false, shapeFlags);
                        PxShape_setName_mut(shape,  (byte*)Marshal.StringToHGlobalAnsi(meshCollider.name));
                        PxShape_setContactOffset_mut(shape, meshCollider.contactOffset);
                        SetShapeQueryFilterData(shape, meshCollider);
                        return shape;
                    }
                }
            }
        }

        private static unsafe PxShape* CreateHeightField(PxPhysics* pxPhysics, TerrainCollider terrainCollider)
        {
            // int width = terrainCollider.terrainData.heightmapWidth;
            // int height = terrainCollider.terrainData.heightmapHeight;
            int width = terrainCollider.terrainData.heightmapResolution;
            int height = terrainCollider.terrainData.heightmapResolution;
            float[,] rawHeights = terrainCollider.terrainData.GetHeights(0, 0, width, height);
            Int16[] heights = new Int16[width * height];
            for (int x = 0; x < width; ++x)
            {
                for (int y = 0; y < height; ++y)
                {
                    heights[y * width + x] = (Int16)(rawHeights[x, y]*32767);
                }
            }

            float scaleX = terrainCollider.terrainData.heightmapScale.x;
            float scaleY = terrainCollider.terrainData.heightmapScale.y / 32767;
            float scaleZ = terrainCollider.terrainData.heightmapScale.z;

            PxHeightFieldDesc hfDesc = PxHeightFieldDesc_new();
            hfDesc.format = PxHeightFieldFormat.S16Tm;
            hfDesc.nbColumns          = (uint)height;
            hfDesc.nbRows             = (uint)width;
            hfDesc.convexEdgeThreshold = 4;
            hfDesc.samples.stride = (uint)sizeof(PxHeightFieldSample);

            PxHeightFieldSample[] pxSamples = new PxHeightFieldSample[width * height];

            PxHeightFieldSample sample;
            PxHeightFieldSample_setTessFlag_mut(&sample);
            for (int i = 0; i < width * height; i++)
            {
                sample.height = heights[i];
                pxSamples[i] = sample;
            }

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
                
                
                var material = CreateMaterial(pxPhysics, terrainCollider.material);
                PxShapeFlags shapeFlags = GetShapeFlags(terrainCollider);
                PxShape* shape = PxPhysics_createShape_mut(pxPhysics, (PxGeometry*)&heightFieldGeometry, material, false, shapeFlags);
                PxShape_setName_mut(shape,  (byte*)Marshal.StringToHGlobalAnsi(terrainCollider.name));
                PxShape_setContactOffset_mut(shape, terrainCollider.contactOffset);
                SetShapeQueryFilterData(shape, terrainCollider);
                return shape;
            }
        }

        #endregion

        #region Utilities

        private static unsafe PxMaterial* CreateMaterial(PxPhysics* pxPhysics, PhysicsMaterial material)
        {
            if (!_sharedMaterials.TryGetValue(material, out var materialPtr))
            {
                PxMaterial* pxMaterial = PxPhysics_createMaterial_mut(pxPhysics, material.staticFriction, material.dynamicFriction, material.bounciness);
                materialPtr = (IntPtr)pxMaterial;
                _sharedMaterials.Add(material, materialPtr);
            }
            return (PxMaterial*)materialPtr;
        }

        private static unsafe PxTransform CreateLocalPost(Transform rootTransform, Transform goTransform, Vector3 center)
        {
            var position = (PxVec3)rootTransform.InverseTransformPoint(goTransform.TransformPoint(center));
            var quaternion = (PxQuat)(Quaternion.Inverse(rootTransform.rotation) * goTransform.rotation);
            return PxTransform_new_5(&position, &quaternion);
        }

        private static PxShapeFlags GetShapeFlags(Collider collider)
        {
            PxShapeFlags shapeFlags = PxShapeFlags.Visualization | PxShapeFlags.SceneQueryShape;
            if (collider.isTrigger)
            {
                shapeFlags |= PxShapeFlags.TriggerShape;
            }
            else
            {
                shapeFlags |= PxShapeFlags.SimulationShape;
            }
            return shapeFlags;
        }
        
        private static unsafe void SetShapeQueryFilterData(PxShape* shape, Collider collider)
        {
            PxFilterData data;
            int layer = collider.gameObject.layer;
            data.word0 = (uint)(1 << layer);
            data.word1 = collider.isTrigger ? 1U : 0U;
            data.word2 = data.word3 = 0;
            PxShape_setQueryFilterData_mut(shape, &data);
        }

        #endregion
    }
    
}

namespace MagicPhysX
{
    public partial struct PxVec3
    {
        public static implicit operator PxVec3(Vector3 v)
        {
            return new PxVec3 { x = v.x, y = v.y, z = v.z };
        }
    }

    public partial struct PxQuat
    {
        public static implicit operator PxQuat(Quaternion q)
        {
            return new  PxQuat { x = q.x, y = q.y, z = q.z, w = q.w };
        }
    }
}