#ifndef _BLISS_SDF_H__
#define _BLISS_SDF_H__

float sdSegment( in float2 p, in float2 a, in float2 b )
{
    float2 pa = p-a, ba = b-a;
    float h = clamp( dot(pa,ba)/dot(ba,ba), 0.0, 1.0 );
    return length( pa - ba*h );
}

#endif //_BLISS_SDF_H__
