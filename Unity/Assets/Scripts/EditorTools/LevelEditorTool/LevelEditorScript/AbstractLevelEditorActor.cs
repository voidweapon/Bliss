using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Bliss;
using Drawing;
using Sirenix.Utilities;
using Unity.Mathematics;
using UnityEngine;
using UnityEditor;


namespace Bliss.EditorTool.LevelEditorTool
{
    public abstract class AbstractLevelEditorActor : MonoBehaviourGizmos
    {
        [Sirenix.OdinInspector.ReadOnly]
        public long NetworkIdentity;

        public SceneActorData Serialize()
        {
            string prefabAssetPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(gameObject);
            return new SceneActorData()
            {
                PrefabPath = prefabAssetPath,
                NetworkIdentity = NetworkIdentity,
                TypeIdentifier = GetActorConfigType().FullName.GetStableHashCode(),
                Data = OnSerialize()
            };
        }

        public void Deserialize(SceneActorData sceneActorData)
        {
            NetworkIdentity = sceneActorData.NetworkIdentity;
            OnDeserialize(sceneActorData.Data);
        }

        public virtual void EndInit()
        {
            
        }

        protected abstract Type GetActorConfigType();
        
        protected abstract byte[] OnSerialize();

        protected abstract void OnDeserialize(byte[] data);

        protected void SetupConfigTransform(out float3 position, out quaternion rotation, out float3 scale)
        {
            position = transform.position;
            rotation = transform.rotation;
            scale = transform.localScale;
        }
        
        protected void SetupTransformFromConfig(float3 position, quaternion rotation, float3 scale)
        {
            transform.position = position;
            transform.rotation = rotation;
            transform.localScale = scale;
        }
    }
}


