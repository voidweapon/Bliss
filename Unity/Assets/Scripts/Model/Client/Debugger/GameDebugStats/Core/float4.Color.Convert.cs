using Unity.Mathematics;
using UnityEngine;

namespace Bliss
{
    public  static class ColorConvert
    {
        public static float4 Convert(Color color)
        {
            return new float4(color.r, color.g, color.b, color.a);
        }
        
        public static Color Convert(float4 color)
        {
            return new Color(color.x, color.y, color.z, color.w);
        }
        
    }
}