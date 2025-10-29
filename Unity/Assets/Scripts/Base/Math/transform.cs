using Unity.Mathematics;

namespace Bliss.Mathematics
{
    public struct transform
    {
        public float3 Location;
        public quaternion Rotation;
        public float3 Scale;

        public transform(float3 location)
        {
            Location = location;
            Rotation = quaternion.identity;
            Scale = new float3(1.0f, 1.0f, 1.0f);
        }
        
        public transform(float3 location, quaternion rotation)
        {
            Location = location;
            Rotation = rotation;
            Scale = new float3(1.0f, 1.0f, 1.0f);
        }
        
        public transform(float3 location, quaternion rotation, float3 scale)
        {
            Location = location;
            Rotation = rotation;
            Scale = scale;
        }

        public void SetTranslation(float3 newTranslation)
        {
            Location = newTranslation;
        }

        public void CopyTranslation(transform other)
        {
            Location = other.Location;
        }

        public void AddToTranslation(float3 deltaTranslation)
        {
            Location += deltaTranslation;
        }
        
        public void SetRotation(quaternion newRotation)
        {
            Rotation = newRotation;
        }

        public void CopyRotation(transform other)
        {
            Rotation = other.Rotation;
        }

        public void ConcatenateRotation(quaternion deltaRotation)
        {
            Rotation = math.mul(Rotation, deltaRotation);
        }

        public void SetTranslationAndRotation(float3 newTranslation, quaternion newRotation)
        {
            Location = newTranslation;
            Rotation = newRotation;
        }
        
        public void SetScale3D(float3 newScale)
        {
            Scale = newScale;
        }

        public void CopyScale3D(transform other)
        {
            Scale = other.Scale;
        }

        // Local To World
        public float3 TransformPosition(float3 position)
        {
            return  math.rotate(Rotation, Scale * position) + Location;
        }
        
        public float3 TransformPositionNoScale(float3 position)
        {
            return  math.rotate(Rotation, position) + Location;
        }
        
        public float3 TransformVector(float3 vector)
        {
            return  math.rotate(Rotation, Scale * vector);
        }
        
        public float3 TransformVectorNoScale(float3 vector)
        {
            return  math.rotate(Rotation, vector);
        }
        
        //  World To Local
        public float3 InverseTransformPosition(float3 position)
        {
            return  Rotation.UnrotateVector(position - Location) * GetSafeScaleReciprocal();
        }
        
        public float3 InverseTransformPositionNoScale(float3 position)
        {
            return  Rotation.UnrotateVector(position - Location);
        }
        
        public float3 InverseTransformVector(float3 vector)
        {
            return  Rotation.UnrotateVector(vector) * GetSafeScaleReciprocal();
        }
        
        public float3 InverseTransformVectorNoScale(float3 vector)
        {
            return Rotation.UnrotateVector(vector);
        }

        public quaternion TransformRotation(quaternion rotation)
        {
            return math.mul(Rotation, rotation);
        }

        public quaternion InverseTransformRotation(quaternion rotation)
        {
            return math.mul(math.inverse(Rotation), rotation);
        }
        
        public float3 GetSafeScaleReciprocal(float tolerance = float.Epsilon)
        {
            float3 safeReciprocalScale;
            if (math.abs(Scale.x) <= tolerance)
            {
                safeReciprocalScale.x = 0f;
            }
            else
            {
                safeReciprocalScale.x = 1f / Scale.x;
            }
            
            if (math.abs(Scale.y) <= tolerance)
            {
                safeReciprocalScale.y = 0f;
            }
            else
            {
                safeReciprocalScale.y = 1f / Scale.y;
            }
            
            if (math.abs(Scale.z) <= tolerance)
            {
                safeReciprocalScale.z = 0f;
            }
            else
            {
                safeReciprocalScale.z = 1f / Scale.z;
            }
            
            return safeReciprocalScale;
        }
    }
}