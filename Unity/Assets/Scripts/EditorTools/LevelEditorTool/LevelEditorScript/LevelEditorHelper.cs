using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace Bliss.EditorTool.LevelEditorTool
{
    public static class LevelEditorHelper
    {
        public static PhysicalRigidActorConfig CreatePhysicalConfig(GameObject go)
        {
            var config = new PhysicalRigidActorConfig();
            
            Rigidbody goRigidBody = go.GetComponent<Rigidbody>();
            config.Name = go.name;
            config.Location = go.transform.position;
            config.Rotation = go.transform.rotation;
            config.IsStatic = goRigidBody == null;
            config.Shapes = new List<PhysicalShapeConfig>();
            
            var colliders = go.GetComponents<Collider>();
            if (colliders != null && colliders.Length > 0)
            {
                Transform rootTransform = go.transform;
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

                    var wPos = goTransform.TransformPoint(boxCollider.center);
                    var lPos = rootTransform.InverseTransformPoint(wPos);
                    shape.Location = rootTransform.InverseTransformPoint(goTransform.TransformPoint(boxCollider.center));
                    shape.Rotation = Quaternion.Inverse(rootTransform.rotation) * goTransform.rotation;
                    config.Shapes.Add(shape);
                }
            }
            
            return config;
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
            //config.MaterialId = CreateMaterial(collider.material).InstanceId;
        }
        
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
    }
}