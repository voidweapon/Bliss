using System;
using Bliss.Mathematics;
using Unity.Mathematics;
using Drawing;
using UnityEngine;
using Sirenix;
using Sirenix.OdinInspector;

namespace Bliss.EditorTool.LevelEditorTool
{
    public class MovablePlatformEditorActor : AbstractLevelEditorActor
    {
        public MovablePlatformConfig Config =  new MovablePlatformConfig();


        protected override Type GetActorConfigType()
        {
            return typeof(MovablePlatformConfig);
        }

        protected override byte[] OnSerialize()
        {
            SetupConfigTransform(out Config.Position, out Config.Rotation, out var _);
            return Serializer.Serialize(Config);
        }

        protected override void OnDeserialize(byte[] data)
        {
            Config = Serializer.Deserialize(typeof(MovablePlatformConfig), data, 0, data.Length) as MovablePlatformConfig;
            if (Config != null) 
                SetupTransformFromConfig(Config.Position, Config.Rotation, new float3(1, 1, 1));
        }

        public override void DrawGizmos()
        {
            float3 point1 = float3.zero;
            float3 point2 = float3.zero;
            bool direction = false;
            
            if (Config != null)
            {
                point1 =  Config.Point1;
                point2 = Config.Point2;
                direction = Config.InitialDirection;
            }
            
            Draw.WireSphere(point1, 0.5f, Color.grey); 
            Draw.WireSphere(point2, 0.5f, Color.grey);
            if (!direction)
            {
                //forward
                Draw.DashedLine(point1, point2, 0.25f, 0.4f, Color.red);
                Draw.Arrowhead((point1 +  point2) / 2, (point2 - point1), new float3(0, 1, 0), 0.4f, Color.red);
            }
            else
            {
                //backword
                Draw.DashedLine(point2, point1, 0.25f, 0.4f, Color.red);
                Draw.Arrowhead((point1 +  point2) / 2, (point1 - point2), new float3(0, 1, 0), 0.4f, Color.red);

            }
        }

        [ButtonGroup("TransformUtility")]
        [Button]
        private void MoveToPoint1()
        {
            transform.position = Config.Point1;
        }
        
        [ButtonGroup("TransformUtility")]
        [Button]
        private void MoveToPoint2()
        {
            transform.position = Config.Point2;
        }


        [ToggleLeft]
        [ShowInInspector]
        private bool ConstrainMoveToLine;


        private void OnValidate()
        {
            if (ConstrainMoveToLine)
            {
                transform.position = mathEx.GetNearestPointInLineSegment(transform.position, Config.Point1, Config.Point2);
            }
        }
    }
}