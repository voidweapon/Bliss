using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Mathematics;

namespace ScenePhysicalExportTest
{
    public class ScenePhysicalDataSpliterTest
    {
        public static bool ExportCurrentScene(Scene scene, string outputPath)
        {
            return false;
        }

        private static unsafe void WalkThroughGameObeject(GameObject go, GameObject parentGo, Rigidbody rootRigidBody = null)
        {
            Rigidbody rigidBody = go.GetComponent<Rigidbody>();
            
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
                    
                }
            }
            
            //Scan Child
            for (int i = 0; i < go.transform.childCount; i++)
            {
                if (CanExport(go.transform.GetChild(i).gameObject))
                {
                    
                }
            }
        }
        
        static bool CanExport(GameObject go)
        {
            return go.activeInHierarchy;
        }
    }
}