using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Mathematics;
using MemoryPack;

namespace ScenePhysicalExport
{
    public static partial class SceneExporter
    {
        static Dictionary<PhysicsMaterial, PhysicMaterialConfig> dicMatrials = new Dictionary<PhysicsMaterial, PhysicMaterialConfig>();
        
        public static unsafe bool ExportScenePhysicsToMemoryPack(Scene unityScene, string outputPath)
        {
            try
            {
                var sceneName = unityScene.name;
                var sceneConfig = new PhysicalSceneConfig()
                {
                    Actors = new List<PhysicalRigidActorConfig>(),
                    Materials = new List<PhysicMaterialConfig>(),
                };
            
                dicMatrials.Clear();
            
                GameObject[] gameObjectArray = unityScene.GetRootGameObjects();
                foreach (GameObject go in gameObjectArray)
                {
                    if (CanExport(go))
                    {
                        //ExportRootGameObject(sceneConfig, go);
                        WalkThroughGameObeject(sceneConfig, go);
                    }
                }

                foreach (var kv in dicMatrials)
                {
                    var matrialConfig = kv.Value;
                    sceneConfig.Materials.Add(matrialConfig);
                }

            
                var bin = MemoryPackSerializer.Serialize(sceneConfig);
                string filename = outputPath + "/" + sceneName + ".dat";
                System.IO.File.WriteAllBytes(filename, bin);

                return true;
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                return false;
            }
        }
        
        public static unsafe PhysicalRigidActorConfig ExportGameObjectToPhysicalRigidActorConfig(GameObject go)
        {
            List<PhysicalShapeConfig> shapes = CreatePhysicalShapes(go);
            Rigidbody rigidBody = go.GetComponent<Rigidbody>();

            if (shapes.Count == 0 && rigidBody == null)
            {
                return null;
            }
            
            var rigidActorConfig = new PhysicalRigidActorConfig();
            rigidActorConfig.Name = go.name;
            rigidActorConfig.Location = go.transform.position;
            rigidActorConfig.Rotation = go.transform.rotation;
            rigidActorConfig.IsStatic = rigidBody == null;
            rigidActorConfig.Shapes = shapes;

            return rigidActorConfig;
        }

        private static unsafe void ExportRootGameObject(PhysicalSceneConfig physicsContext, GameObject go)
        {
            var rigidActorConfig = ExportGameObjectToPhysicalRigidActorConfig(go);
            if(rigidActorConfig == null)
                return;
            
            physicsContext.Actors.Add(rigidActorConfig);
        }

        private static unsafe List<PhysicalShapeConfig> WalkThroughGameObeject(PhysicalSceneConfig physicsContext, GameObject go, Rigidbody rootRigidBody = null)
        {
            List<PhysicalShapeConfig> shapes = new List<PhysicalShapeConfig>();
            List<PhysicalShapeConfig> terrianShapes = new List<PhysicalShapeConfig>();

            if (go.transform.lossyScale.x < 0 || go.transform.lossyScale.y < 0 || go.transform.lossyScale.z < 0)
            {
                Debug.LogError($" {go.name} 的transform scale存在负数, GameObject Name: {GetGameObjectNameInHierarchy(go.transform)}，请检查");
                return  shapes;
            }

            Rigidbody goRigidBody = go.GetComponent<Rigidbody>();
            Rigidbody tempRootRigidBody = rootRigidBody;
            if (goRigidBody != null)
            {
                tempRootRigidBody = goRigidBody;
            }
            
            var colliders = go.GetComponents<Collider>();
            if (colliders != null && colliders.Length > 0)
            {
                Transform rootTransform = go.transform;
                Transform goTransform = go.transform;

                if (rootRigidBody != null)
                {
                    rootTransform = rootRigidBody.transform;
                }
                
                //BoxCollider
                BoxCollider[] boxColliders = go.GetComponents<BoxCollider>();
                foreach (BoxCollider boxCollider in boxColliders)
                {
                    if (boxCollider.size.x < 0 || boxCollider.size.y < 0 || boxCollider.size.z < 0)
                    {
                        Debug.LogError($"跳过BoxCollider {boxCollider.name} 因Size存在负数, GameObject Name: {GetGameObjectNameInHierarchy(boxCollider.transform)}，请检查");
                        continue;
                    }

                    var shape = CreateBoxCollider(boxCollider);
                    if (shape == null)
                    {
                        continue;
                    }

                    var wPos = goTransform.TransformPoint(boxCollider.center);
                    var lPos = rootTransform.InverseTransformPoint(wPos);
                    shape.Location = rootTransform.InverseTransformPoint(goTransform.TransformPoint(boxCollider.center));
                    shape.Rotation = Quaternion.Inverse(rootTransform.rotation) * goTransform.rotation;
                    //Debug.Log($"BoxCollider: {boxCollider.name}  location={shape.Location}");
                    shapes.Add(shape);
                }
                
                //SphereCollider
                SphereCollider[] sphereColliders = go.GetComponents<SphereCollider>();
                foreach (SphereCollider sphereCollider in sphereColliders)
                {
                    if (sphereCollider.radius == 0f)
                    {
                        Debug.LogError($"跳过BSphereCollider {sphereCollider.name} 因radius存在负数, GameObject Name: {GetGameObjectNameInHierarchy(sphereCollider.transform)}，请检查");
                        continue;
                    }

                    var shape = CreateSphereCollider(sphereCollider);
                    if (shape == null)
                    {
                        continue;
                    }
                    
                    shape.Location = rootTransform.InverseTransformPoint(goTransform.TransformPoint(sphereCollider.center));
                    shape.Rotation = Quaternion.Inverse(rootTransform.rotation) * goTransform.rotation;
                    //Debug.Log($"SphereCollider: {sphereCollider.name}  location={shape.Location}");
      
                    shapes.Add(shape);
                }
                
                //CapsuleCollider
                CapsuleCollider[] capsuleColliders = go.GetComponents<CapsuleCollider>();
                foreach (CapsuleCollider capsuleCollider in capsuleColliders)
                {
                    if (capsuleCollider.radius == 0f || capsuleCollider.height == 0f)
                    {
                        Debug.LogError($"跳过CapsuleCollider {capsuleCollider.name} 因radius/height存在负数, GameObject Name: {GetGameObjectNameInHierarchy(capsuleCollider.transform)}，请检查");
                        continue;
                    }

                    Quaternion baseRotation = Quaternion.identity;
                    if (capsuleCollider.direction == 0)
                    {
                        baseRotation = Quaternion.AngleAxis(90, Vector3.forward);
                    }
                    else if (capsuleCollider.direction == 2)
                    {
                        baseRotation = Quaternion.AngleAxis(90, Vector3.right);
                    }
                    Quaternion colliderRotation = baseRotation * goTransform.rotation;
                    
                    var shape = CreateCapsuleCollider(capsuleCollider);
                    if (shape == null)
                    {
                        continue;
                    }
                    shape.Location = rootTransform.InverseTransformPoint(goTransform.TransformPoint(capsuleCollider.center));
                    shape.Rotation = Quaternion.Inverse(rootTransform.rotation) * colliderRotation;
                    //Debug.Log($"CapsuleCollider: {capsuleCollider.name}  location={shape.Location}");
                    
                    shapes.Add(shape);
                }
                
                //MeshCollider
                MeshCollider[] meshColliders = go.GetComponents<MeshCollider>();
                foreach (MeshCollider meshCollider in meshColliders)
                {
                    if (meshCollider.sharedMesh == null || meshCollider.sharedMesh.vertexCount == 0)
                    {
                        Debug.LogError($"跳过 MeshCollider {meshCollider.name} 因mesh丢失, GameObject Name: {GetGameObjectNameInHierarchy(meshCollider.transform)}，请检查");
                        continue;
                    }
                    
                    var shape = CreateMeshCollider(meshCollider);
                    if (shape == null)
                    {
                        continue;
                    }
                    
                    shape.Location = rootTransform.InverseTransformPoint(goTransform.position);
                    shape.Rotation = Quaternion.Inverse(rootTransform.rotation) * goTransform.rotation;
                    //Debug.Log($"MeshCollider: {meshCollider.name}  location={shape.Location}");
                    shapes.Add(shape);
                }
                
                //TerrainCollider
                TerrainCollider[] terrainColliders = go.GetComponents<TerrainCollider>();
                foreach (TerrainCollider terrainCollider in terrainColliders)
                {
                    var shape = CreateHeightField(terrainCollider);
                    if (shape == null)
                    {
                        continue;
                    }

                    shape.Location = rootTransform.InverseTransformPoint(goTransform.position);
                    shape.Rotation = Quaternion.Inverse(rootTransform.rotation) * goTransform.rotation;
                    Debug.Log($"TerrainCollider: {terrainCollider.name}  location={shape.Location}");
                    terrianShapes.Add(shape);
                }
            }
            
            //Scan Child
            for (int i = 0; i < go.transform.childCount; i++)
            {
                if (CanExport(go.transform.GetChild(i).gameObject))
                {
                    shapes.AddRange(WalkThroughGameObeject(physicsContext, go.transform.GetChild(i).gameObject, tempRootRigidBody));
                }
            }

            if (terrianShapes.Count > 0)
            {
                //terrian 单独一个Rigidbody
                var rigidActorConfig = new PhysicalRigidActorConfig();
                rigidActorConfig.Name = go.name;
                rigidActorConfig.Location = go.transform.position;
                rigidActorConfig.Rotation = go.transform.rotation;
                rigidActorConfig.IsStatic = goRigidBody == null;
                rigidActorConfig.IsKinematic = goRigidBody != null && goRigidBody.isKinematic;
                rigidActorConfig.UseGravity = goRigidBody != null && goRigidBody.useGravity;
                rigidActorConfig.Shapes = new List<PhysicalShapeConfig>(terrianShapes);
            
                physicsContext.Actors.Add(rigidActorConfig);
            }

            if (rootRigidBody == null && shapes.Count > 0)
            {
                //已当前节点Attach Shapes
                var rigidActorConfig = new PhysicalRigidActorConfig();
                rigidActorConfig.Name = go.name;
                rigidActorConfig.Location = go.transform.position;
                rigidActorConfig.Rotation = go.transform.rotation;
                rigidActorConfig.IsStatic = goRigidBody == null;
                rigidActorConfig.IsKinematic = goRigidBody != null && goRigidBody.isKinematic;
                rigidActorConfig.UseGravity = goRigidBody != null && goRigidBody.useGravity;
                rigidActorConfig.Shapes = new List<PhysicalShapeConfig>(shapes);
            
                physicsContext.Actors.Add(rigidActorConfig);
                shapes.Clear();
                
                //Debug.Log($"rigidActor[{rigidActorConfig.Name }]: IsStatic={rigidActorConfig.IsStatic}  Location={rigidActorConfig.Location}  Rotation={rigidActorConfig.Rotation}  Shapes={rigidActorConfig.Shapes.Count}");
                return shapes;
            }
            else
            {
                return shapes;
            }
        }

        private static unsafe List<PhysicalShapeConfig> CreatePhysicalShapes(GameObject go)
        {
            List<PhysicalShapeConfig> shapes = new List<PhysicalShapeConfig>();

            if (go.transform.lossyScale.x < 0 || go.transform.lossyScale.y < 0 || go.transform.lossyScale.z < 0)
            {
                Debug.LogError($" {go.name} 的transform scale存在负数, GameObject Name: {GetGameObjectNameInHierarchy(go.transform)}，请检查");
                return  shapes;
            }
            
            Transform rootTransform = go.transform.root;
            Transform goTransform = go.transform;

            //BoxCollider
            BoxCollider[] boxColliders = go.GetComponents<BoxCollider>();
            foreach (BoxCollider boxCollider in boxColliders)
            {
                if (boxCollider.size.x < 0 || boxCollider.size.y < 0 || boxCollider.size.z < 0)
                {
                    Debug.LogError($"跳过BoxCollider {boxCollider.name} 因Size存在负数, GameObject Name: {GetGameObjectNameInHierarchy(boxCollider.transform)}，请检查");
                    continue;
                }

                var shape = CreateBoxCollider(boxCollider);
                if (shape == null)
                {
                    continue;
                }
                
                shape.Location = rootTransform.InverseTransformPoint(goTransform.TransformPoint(boxCollider.center));
                shape.Rotation = Quaternion.Inverse(rootTransform.rotation) * goTransform.rotation;
                //Debug.Log($"BoxCollider: {boxCollider.name}  location={shape.Location}");
                shapes.Add(shape);
            }
            
            //SphereCollider
            SphereCollider[] sphereColliders = go.GetComponents<SphereCollider>();
            foreach (SphereCollider sphereCollider in sphereColliders)
            {
                if (sphereCollider.radius == 0f)
                {
                    Debug.LogError($"跳过BSphereCollider {sphereCollider.name} 因radius存在负数, GameObject Name: {GetGameObjectNameInHierarchy(sphereCollider.transform)}，请检查");
                    continue;
                }

                var shape = CreateSphereCollider(sphereCollider);
                if (shape == null)
                {
                    continue;
                }
                
                shape.Location = rootTransform.InverseTransformPoint(goTransform.TransformPoint(sphereCollider.center));
                shape.Rotation = Quaternion.Inverse(rootTransform.rotation) * goTransform.rotation;
                //Debug.Log($"SphereCollider: {sphereCollider.name}  location={shape.Location}");
  
                shapes.Add(shape);
            }
            
            //CapsuleCollider
            CapsuleCollider[] capsuleColliders = go.GetComponents<CapsuleCollider>();
            foreach (CapsuleCollider capsuleCollider in capsuleColliders)
            {
                if (capsuleCollider.radius == 0f || capsuleCollider.height == 0f)
                {
                    Debug.LogError($"跳过CapsuleCollider {capsuleCollider.name} 因radius/height存在负数, GameObject Name: {GetGameObjectNameInHierarchy(capsuleCollider.transform)}，请检查");
                    continue;
                }

                Quaternion baseRotation = Quaternion.identity;
                if (capsuleCollider.direction == 1)
                {
                    baseRotation = Quaternion.AngleAxis(90, Vector3.forward);
                }
                else if (capsuleCollider.direction == 2)
                {
                    baseRotation = Quaternion.AngleAxis(90, Vector3.up);
                }
                Quaternion colliderRotation = baseRotation * goTransform.rotation;
                
                var shape = CreateCapsuleCollider(capsuleCollider);
                if (shape == null)
                {
                    continue;
                }
                
                shape.Location = rootTransform.InverseTransformPoint(goTransform.TransformPoint(capsuleCollider.center));
                shape.Rotation = Quaternion.Inverse(rootTransform.rotation) * colliderRotation;
                //Debug.Log($"CapsuleCollider: {capsuleCollider.name}  location={shape.Location}");
                
                shapes.Add(shape);
            }
            
            //MeshCollider
            MeshCollider[] meshColliders = go.GetComponents<MeshCollider>();
            foreach (MeshCollider meshCollider in meshColliders)
            {
                if (meshCollider.sharedMesh == null || meshCollider.sharedMesh.vertexCount == 0)
                {
                    Debug.LogError($"跳过 MeshCollider {meshCollider.name} 因mesh丢失, GameObject Name: {GetGameObjectNameInHierarchy(meshCollider.transform)}，请检查");
                    continue;
                }
                
                var shape = CreateMeshCollider(meshCollider);
                if (shape == null)
                {
                    continue;
                }
                
                shape.Location = rootTransform.InverseTransformPoint(goTransform.position);
                shape.Rotation = Quaternion.Inverse(rootTransform.rotation) * goTransform.rotation;
                //Debug.Log($"MeshCollider: {meshCollider.name}  location={shape.Location}");
                shapes.Add(shape);
            }
            
            //TerrainCollider
            TerrainCollider[] terrainColliders = go.GetComponents<TerrainCollider>();
            foreach (TerrainCollider terrainCollider in terrainColliders)
            {
                var shape = CreateHeightField(terrainCollider);
                if (shape == null)
                {
                    continue;
                }

                shape.Location = rootTransform.InverseTransformPoint(goTransform.position);
                shape.Rotation = Quaternion.Inverse(rootTransform.rotation) * goTransform.rotation;
                
                shapes.Add(shape);
            }
            
            //Scan Child
            for (int i = 0; i < go.transform.childCount; i++)
            {
                if (CanExport(go.transform.GetChild(i).gameObject))
                {
                    shapes.AddRange(CreatePhysicalShapes(go.transform.GetChild(i).gameObject));
                }
            }

            return shapes;
        }


        private static PhysicalShapeConfig CreateBoxCollider(BoxCollider boxCollider)
        {
            var halfExtents = new float3(
                boxCollider.size.x * Mathf.Abs(boxCollider.transform.lossyScale.x) / 2.0f,
                boxCollider.size.y * Mathf.Abs(boxCollider.transform.lossyScale.y) / 2.0f,
                boxCollider.size.z * Mathf.Abs(boxCollider.transform.lossyScale.z) / 2.0f);

            var boxConfig = new BoxColliderConfig()
            {
                HalfExtents = halfExtents,
            };
            
            SetupColliderConfig(boxConfig, boxCollider);
            
            return boxConfig;
        }
        
        private static PhysicalShapeConfig CreateSphereCollider(SphereCollider sphereCollider)
        {
            float maxScale = Math.Max(Math.Max(Mathf.Abs(sphereCollider.transform.localScale.x),
                    Mathf.Abs(sphereCollider.transform.localScale.y)),
                Mathf.Abs(sphereCollider.transform.localScale.z));

            var sphereConfig = new SphereColliderConfig()
            {
                Radius = sphereCollider.radius * maxScale,
            };
            
            SetupColliderConfig(sphereConfig, sphereCollider);
            
            return sphereConfig;
        }
        
        private static PhysicalShapeConfig CreateCapsuleCollider(CapsuleCollider capsuleCollider)
        {
            float maxScale = Math.Max(Math.Max(Mathf.Abs(capsuleCollider.transform.localScale.x), 
                    Mathf.Abs(capsuleCollider.transform.localScale.y)), 
                Mathf.Abs(capsuleCollider.transform.localScale.z));
            float radius = capsuleCollider.radius * maxScale;
            float height = capsuleCollider.height * capsuleCollider.transform.localScale.y / 2.0f - radius;

            var capsuleConfig = new CapsuleColliderConfig()
            {
                Radius = radius,
                Height = height,
                //Direction = capsuleCollider.direction
                Direction = 0, //Direction保持为0, 由旋转矩阵来确定方向
            };
            
            SetupColliderConfig(capsuleConfig, capsuleCollider);
            
            return capsuleConfig;
        }

        private static MeshColliderConfig CreateMeshCollider(MeshCollider meshCollider)
        {
            Mesh mesh = meshCollider.sharedMesh;
            Vector3 scale = meshCollider.transform.lossyScale;
            
            var meshConfig = new MeshColliderConfig();
            meshConfig.IsConvex = meshCollider.convex;
            meshConfig.Vertices = new float3[mesh.vertices.Length];
            meshConfig.Triangles = new int[mesh.triangles.Length];
            
            for (int i = 0; i < mesh.vertices.Length; ++i)
            {
                Vector3 vertex = mesh.vertices[i];
                vertex.x *= scale.x;
                vertex.y *= scale.y;
                vertex.z *= scale.z;
                meshConfig.Vertices[i] = vertex;
            }
            
            for (int i = 0; i < mesh.triangles.Length; ++i)
            {
                meshConfig.Triangles[i] = mesh.triangles[i];
            }
            
            SetupColliderConfig(meshConfig, meshCollider);
            
            return meshConfig;
        }

        private static HeightFieldConfig CreateHeightField(TerrainCollider terrainCollider)
        {
            int width = terrainCollider.terrainData.heightmapResolution;
            int height = terrainCollider.terrainData.heightmapResolution;
            float[,] rawHeights = terrainCollider.terrainData.GetHeights(0, 0, width, height);

            var heightFieldConfig = new HeightFieldConfig()
            {
                Width  = width,
                Height =  height,
            };

            heightFieldConfig.Heights = new short[width * height];
            for (int x = 0; x < width; ++x)
            {
                for (int y = 0; y < height; ++y)
                {
                    heightFieldConfig.Heights[y * width + x] = (short)(rawHeights[x, y] * 32767);
                }
            }
            
            float scaleX = terrainCollider.terrainData.heightmapScale.x;
            float scaleY = terrainCollider.terrainData.heightmapScale.y / 32767;
            float scaleZ = terrainCollider.terrainData.heightmapScale.z;
            heightFieldConfig.HeightmapScale = new float3(scaleX, scaleY, scaleZ);
            
            SetupColliderConfig(heightFieldConfig, terrainCollider);
            
            return heightFieldConfig;
        }

        private static void SetupColliderConfig(PhysicalShapeConfig config, Collider collider)
        {
            config.Location = collider.transform.position;
            config.Rotation = collider.transform.rotation;
            config.Scale = collider.transform.lossyScale;
            config.IsTrigger = collider.isTrigger;
            config.Layer = collider.gameObject.layer;
            config.ContactOffset = collider.contactOffset;
            config.Tag = collider.gameObject.tag;
            config.Name = collider.gameObject.name;
            config.MaterialId = CreateMaterial(collider.material).InstanceId;
        }
        
        private static PhysicMaterialConfig CreateMaterial(PhysicsMaterial material)
        {
            if (!dicMatrials.TryGetValue(material, out var materialConfig))
            {
                materialConfig = new PhysicMaterialConfig()
                {
                    InstanceId = material.GetInstanceID(),
                    DynamicFriction = material.staticFriction,
                    StaticFriction = material.dynamicFriction,
                    Bounciness = material.bounciness,
                };
                dicMatrials.Add(material, materialConfig);
            }
            return materialConfig;
        }
    }
}