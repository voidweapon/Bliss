using UnityEngine;

namespace Bliss
{
    public class RuntimeLightProbeAsset : ScriptableObject
    {
        [SerializeField]
        public UnityEngine.Rendering.SphericalHarmonicsL2[] coefficients;
        [SerializeField]
        public LightProbes lightProbes;
    }
}