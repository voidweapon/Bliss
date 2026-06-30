using Unity.Cinemachine;
using UnityEngine;

namespace Bliss.CameraExtension
{
    [AddComponentMenu("")] // Don't display in add component menu
    [SaveDuringPlay]
    public class CinemachineFramingTransposerEx : CinemachineFramingTransposer
    {
        public AnimationCurve CameraDistanceCurve;
        public override void MutateCameraState(ref Unity.Cinemachine.CameraState curState, float deltaTime)
        {
            float cachedDistance = m_CameraDistance;
            float x = curState.RawOrientation.eulerAngles.x % 360f;
            if(x > 180f) x  -= 360f;
            float alpha = (x + 90f) / 180f;
            m_CameraDistance = CameraDistanceCurve.Evaluate(alpha) * cachedDistance;
            base.MutateCameraState(ref curState, deltaTime);
            m_CameraDistance = cachedDistance;
        }
    }
}