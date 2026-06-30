using UnityEngine;

namespace Bliss
{
    public class RuntimeLightDataAsset : ScriptableObject
    {
        public Texture2D[] lightmaps;
        public Texture2D[] lightmapsDir;
        public Texture2D[] shadowMasks;
        public LightmapsMode lightmapsMode;
        public RuntimeLightProbeAsset lightProbesAsset;
        public bool hasRealtimeLights;
    }
}