using System.Collections.Generic;
using UnityEngine;

namespace Bliss.ViewModel.Debugger
{
    public class TransformStatDebugComponent : Entity, IAwake, IUpdate, IDestroy
    {
        public GameObject UI;
        public Varaince_DataUIGraphic TransformGraphic;
        public Varaince_DataUIGraphic RotationGraphic;

        public long Time;
        
        public Dictionary<int, Transform> WatchTransforms = new Dictionary<int, Transform>();
        public Dictionary<int, TransformTraceData> TransformsTrace = new Dictionary<int, TransformTraceData>();
        
        public class TransformTraceData
        {
            public Vector3 Position;
            public Quaternion Rotation;
            
            public Vector3 Velocity;
            public float AngularVelocity;

            public void Init(Transform transform)
            {
                Position = transform.position;
                Rotation = transform.rotation;
                Velocity = Vector3.zero;
                AngularVelocity = 0;;
            }

            public void Update(Transform transform, float deltaTime)
            {
                Velocity = (transform.position - Position) / deltaTime;
                AngularVelocity = Quaternion.Angle(transform.rotation, Rotation) / deltaTime;
                
                Position =  transform.position;
                Rotation = transform.rotation;
            }
        }
    }
}