namespace Bliss
{
    public static partial class PhysicsInvokeType
    {
        public const int RigidbodyAwake = 401;
        public const int RigidbodyDestroy = 402;
        public const int WakeUp = 403;
        public const int AddForceAtPosition = 404;
        public const int MovePosition = 405;
        public const int MoveRotation = 406;
        public const int MovePositionAndRotation = 407;
        public const int SetPosition = 408;
        public const int SetRotation = 409;
        public const int SetCenterOfMass = 410;
        public const int SetVelocity = 411;
        public const int SetAngularVelocity = 412;
        public const int SetMass = 413;
        
        public const int BoxColliderAwake = 501;
        public const int BoxColliderDestroy = 502;
        public const int BoxColliderSetCenter = 503;
        public const int BoxColliderSetSize = 504;
        
        public const int CapsuleColliderAwake = 505;
        public const int CapsuleColliderDestroy = 506;
        public const int CapsuleColliderSetCenter = 507;
        public const int CapsuleColliderSetRadius = 508;
        public const int CapsuleColliderSetHeight = 509;
        public const int CapsuleColliderSetDirection = 510;
        
        public const int SphereColliderAwake = 511;
        public const int SphereColliderDestroy = 512;
        public const int SphereColliderSetCenter = 513;
        public const int SphereColliderSetRadius = 514;
        
        public const int MeshColliderAwake = 515;
        public const int MeshColliderDestroy = 516;
        
        public const int HeightFieldColliderAwake = 517;
        public const int HeightFieldColliderDestroy = 518;
    }
}