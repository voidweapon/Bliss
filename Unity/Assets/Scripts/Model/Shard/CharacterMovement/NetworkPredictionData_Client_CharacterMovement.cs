using System.Collections.Generic;
using Bliss.Base.Collections;
using Unity.Mathematics;

namespace Bliss.CharacterMovement
{
    public class NetworkPredictionData_Client_CharacterMovement
    {
        public FixedCircularArray<SavedMove_Character> SavedMoves;
        public List<SavedMove_Character> SavedMovesPool;
        public int MaxSavedMoveCount = 96;
        public int MaxFreeMoveCount = 96;
        public SavedMove_Character PendingMove;
        public SavedMove_Character LastAckedMove;

        /// <summary>
        ///  when true, update the position (via ClientUpdatePosition)
        /// </summary>
        public bool bUpdatePosition;
        
        
        /** Timestamp of last time it sent a servermove() to the server. This is an increasing timestamp from the owning UWorld (undilated, real time seconds). Used for holding off on sending movement updates to save bandwidth. */
        public float ClientUpdateRealTime;

        /** Current TimeStamp for sending new Moves to the Server. This time resets to zero at a frequency of MinTimeBetweenTimeStampResets and increases with this component's tick delta time. */
        public float CurrentTimeStamp;

        /** Last World timestamp (undilated, real time) at which we received a server ack for a move. This could be either a good move or a correction from the server. */
        public float LastReceivedAckRealTime;
        
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
        public float LastCorrectionDelta;

        /// <summary>
        /// Used to track time of last correction
        /// </summary>
        public float LastCorrectionTime;

        /// <summary>
        /// Max time delta between server updates over which client smoothing is allowed to interpolate.
        /// Default 0.5f
        /// </summary>
        public float MaxClientSmoothingDeltaTime = 0.5f;

        /// <summary>
        /// Used to track the timestamp of the last server move.
        /// </summary>
        public float SmoothingServerTimeStamp;
        
        /// <summary>
        /// Used to track the client time as we try to match the server.
        /// </summary>
        public float SmoothingClientTimeStamp;

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
        /// Default 0.1s
        /// </summary>
        public float SmoothNetUpdateTime = 0.1f;
        
        /// <summary>
        /// How long to take to smoothly interpolate from the old pawn rotation on the client to the corrected one sent by the server.  Must be >= 0. Not used for linear smoothing.
        /// Default 0.05s
        /// </summary>
        public float SmoothNetUpdateRotationTime = 0.05f;

        /// <summary>
        ///  Max delta time for a given move, in real seconds
        /// Default 0.125s
        /// </summary>
        public float MaxMoveDeltaTime = 0.125f;

        public NetworkPredictionData_Client_CharacterMovement(
            ENetMode netMode,
            float networkMaxSmoothUpdateDistance,
            float networkNoSmoothUpdateDistance,
            float listenServerNetworkSimulatedSmoothLocationTime,
            float networkSimulatedSmoothLocationTime,
            float listenServerNetworkSimulatedSmoothRotationTime,
            float networkSimulatedSmoothRotationTime,
            float maxClientSmoothingDeltaTime = 0.5f,
            float smoothNetUpdateTime = 0.1f,
            float smoothNetUpdateRotationTime = 0.05f,
            float maxMoveDeltaTime = 0.125f)
        {
            this.MaxClientSmoothingDeltaTime =  maxClientSmoothingDeltaTime;
            this.SmoothNetUpdateTime = smoothNetUpdateTime;
            this.SmoothNetUpdateRotationTime = smoothNetUpdateRotationTime;
            this.MaxMoveDeltaTime = maxMoveDeltaTime;

            this.MaxClientSmoothingDeltaTime = 0.5f;
            this.MaxSmoothNetUpdateDist = networkMaxSmoothUpdateDistance;
            this.NoSmoothNetUpdateDist = networkNoSmoothUpdateDistance;

            bool bIsListenServer = netMode == ENetMode.NM_ListenServer;
            this.SmoothNetUpdateTime = bIsListenServer? listenServerNetworkSimulatedSmoothLocationTime : networkSimulatedSmoothLocationTime;
            this.SmoothNetUpdateRotationTime = bIsListenServer? listenServerNetworkSimulatedSmoothRotationTime : networkSimulatedSmoothRotationTime;

            this.MaxClientSmoothingDeltaTime = 2.0f * this.MaxMoveDeltaTime;
            
            this.MaxFreeMoveCount = 96;
            this.MaxSavedMoveCount = 96;
            this.SavedMovesPool = new List<SavedMove_Character>(this.MaxFreeMoveCount);
            this.SavedMoves = new FixedCircularArray<SavedMove_Character>(this.MaxSavedMoveCount);
        }

        public SavedMove_Character CreateSavedMove()
        {
            if (SavedMoves.Count >= MaxSavedMoveCount)
            {
                Log.Warning($"CreateSavedMove: Hit limit of {SavedMoves.Count} saved moves (timing out or very bad ping?");
                // Free all saved moves
                for (int i=0; i < SavedMoves.Count; i++)
                {
                    FreeMove(SavedMoves[i]);
                }
                SavedMoves.Clear();
            }

            if (SavedMovesPool.Count == 0)
            {
                // No free moves, allocate a new one.
                var newMove = new  SavedMove_Character();
                newMove.Clear();
                return newMove;
            }
            else
            {
                // Pull from the free pool
                var firstFree = SavedMovesPool[SavedMovesPool.Count - 1];
                SavedMovesPool.RemoveAt(SavedMovesPool.Count - 1);
                firstFree.Clear();
                return firstFree;
            }
        }

        public void FreeMove(SavedMove_Character savedMove)
        {
            if (savedMove != null)
            {
                if (SavedMovesPool.Count < MaxSavedMoveCount)
                {
                    SavedMovesPool.Add(savedMove);
                }

                if (PendingMove == savedMove)
                {
                    PendingMove = null;
                }

                if (LastAckedMove == savedMove)
                {
                    LastAckedMove = null;
                }
            }
        }
        
        public int GetSavedMoveIndex( float timeStamp)
        {
            if(SavedMoves.Count > 0 )
            {
                // If LastAckedMove isn't using an old TimeStamp (before reset), we can prevent the iteration if incoming TimeStamp is outdated
                if( LastAckedMove != null && !LastAckedMove.bOldTimeStampBeforeReset && (timeStamp <= LastAckedMove.TimeStamp) )
                {
                    return -1;
                }

                // Otherwise see if we can find this move.
                for (int index=0; index<SavedMoves.Count; index++)
                {
                    SavedMove_Character currentMove = SavedMoves[index];
                    if( currentMove.TimeStamp == timeStamp )
                    {
                        return index;
                    }
                }
            }
            return -1;
        }
        
        public void AckMove(int ackedMoveIndex)
        {
            if (ackedMoveIndex != -1)
            {
                var ackedMove = SavedMoves[ackedMoveIndex];
                if (LastAckedMove != null)
                {
                    FreeMove(LastAckedMove);
                }

                LastAckedMove = ackedMove;
		    
                // Free expired moves.
                for(int moveIndex = 0; moveIndex<ackedMoveIndex; moveIndex++)
                {
                    var move = SavedMoves[moveIndex];
                    FreeMove(move);
                }
		    
                // And finally cull all of those, so only the unacknowledged moves remain in SavedMoves.
                SavedMoves.RemoveRange(0, ackedMoveIndex + 1);
            }

            LastReceivedAckRealTime = TimeInfo.Instance.ClientNow();
        }
        
        public float UpdateTimeStampAndDeltaTime( float deltaTime , float minTimeBetweenTimeStampResets)
        {
            // Reset TimeStamp regularly to combat float accuracy decreasing over time.
            if (CurrentTimeStamp > minTimeBetweenTimeStampResets)
            {
                CurrentTimeStamp -= minTimeBetweenTimeStampResets;
		    
                // Mark all buffered moves as having old time stamps, so we make sure to not resend them.
                // That would confuse the server.

                for (int i = 0; i < SavedMoves.Count; i++)
                {
                    SavedMoves[i].bOldTimeStampBeforeReset = true;
                }

                if (LastAckedMove != null)
                {
                    LastAckedMove.bOldTimeStampBeforeReset = true;
                }
            }
            
            // Update Current TimeStamp.
            CurrentTimeStamp += deltaTime;
            float clientDeltaTime = deltaTime;
            
            // Server uses TimeStamps to derive DeltaTime which introduces some rounding errors.
            // Make sure we do the same, so MoveAutonomous uses the same inputs and is deterministic!!
            if (SavedMoves.Count > 0)
            {
                var previousMove = SavedMoves[SavedMoves.Count - 1];
                if (!previousMove.bOldTimeStampBeforeReset)
                {
                    // How server will calculate its deltatime to update physics.
                    float serverDeltaTime = CurrentTimeStamp - previousMove.TimeStamp;
                    // Have client always use the Server's DeltaTime. Otherwise our physics simulation will differ and we'll trigger too many position corrections and increase our network traffic.
                    clientDeltaTime = serverDeltaTime;
                }
            }

            return math.min(clientDeltaTime, MaxMoveDeltaTime);
        }
    }
}