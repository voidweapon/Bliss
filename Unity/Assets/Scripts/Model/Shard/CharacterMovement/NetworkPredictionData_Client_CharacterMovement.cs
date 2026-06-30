using Unity.Mathematics;

namespace Bliss.CharacterMovement
{
    public class NetworkPredictionData_Client_CharacterMovement
    {
        /// <summary>
        /// Used for position smoothing in net games
        /// </summary>
        public float3 OriginalMeshTranslationOffset;
        
        /// <summary>
        /// World space offset of the mesh. Target value is zero offset. Used for position smoothing in net games.
        /// </summary>
        public float3 MeshTranslationOffset;

        /// <summary>
        /// Used for rotation smoothing in net games (only used by linear smoothing).
        /// </summary>
        public quaternion OriginalMeshRotationOffset;
        
        /// <summary>
        ///  Component space offset of the mesh. Used for rotation smoothing in net games.
        /// </summary>
        public quaternion MeshRotationOffset;
        
        /// <summary>
        /// Target for mesh rotation interpolation.
        /// </summary>
        public quaternion MeshRotationTarget;

        /// <summary>
        /// Used for remembering how much time has passed between server corrections 
        /// </summary>
        public long LastCorrectionDelta;

        /// <summary>
        /// Used to track time of last correction
        /// </summary>
        public long LastCorrectionTime;

        /// <summary>
        /// Max time delta between server updates over which client smoothing is allowed to interpolate.
        /// Default 0.5f
        /// </summary>
        public long MaxClientSmoothingDeltaTime = 500;

        /// <summary>
        /// Used to track the timestamp of the last server move.
        /// </summary>
        public long SmoothingServerTimeStamp;
        
        /// <summary>
        /// Used to track the client time as we try to match the server.
        /// </summary>
        public long SmoothingClientTimeStamp;

        /// <summary>
        /// Maximum distance character is allowed to lag behind server location when interpolating between updates.
        /// </summary>
        public float MaxSmoothNetUpdateDist;
        
        /// <summary>
        /// Maximum distance beyond which character is teleported to the new server location without any smoothing.
        /// </summary>
        public float NoSmoothNetUpdateDist;

        /// <summary>
        /// How long to take to smoothly interpolate from the old pawn position on the client to the corrected one sent by the server.  Must be >= 0. Not used for linear smoothing.
        /// Default 0.1S
        /// </summary>
        public long SmoothNetUpdateTime = 100;
        
        /// <summary>
        /// How long to take to smoothly interpolate from the old pawn rotation on the client to the corrected one sent by the server.  Must be >= 0. Not used for linear smoothing.
        /// Default 0.05
        /// </summary>
        public long SmoothNetUpdateRotationTime = 50;

        /// <summary>
        ///  Max delta time for a given move, in real seconds
        /// Default 0.125
        /// </summary>
        public long MaxMoveDeltaTime = 125;
    }
}