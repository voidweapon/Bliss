using Bliss.CharacterControllers;
using Bliss.Core;
using Bliss.Mathematics;
using Unity.Mathematics;

namespace Bliss.CharacterMovement
{
    [EntitySystemOf(typeof(CharacterMovementComponent))]
    public static partial class CharacterMovementComponentSystems
    {
        private static void Awake(this CharacterMovementComponent self)
        {
            self.MovementMode = EMovementMode.None;

            self.bReplicateMovement = true;
            self.LogicTimeStamp = 0;
            self.CurrentJumpCount = 0;
            
            self.TransformCmp = self.Parent.GetComponent<TransformComponent>();
            
            var movementController = new CharacterMovementController();
            movementController.MovementCmp = self;
            self.MovementController = movementController;

            
            self.LocalNetRole = ENetRole.Authority;
            self.RemoteNetRole = ENetRole.Authority;
            var netRoleCmp = self.Parent.GetComponent<NetRoleComponent>();
            if (netRoleCmp != null)
            {
                self.LocalNetRole = netRoleCmp.LocalNetRole;
                self.RemoteNetRole = netRoleCmp.RemoteNetRole;
            }
            
            self.GroundShift = math.ceillog2((int)EMovementMode.Max);
            self.CustomModeThr = 2 * (1 << self.GroundShift);
            self.GroundMask = (1 << self.GroundShift) - 1;

            // This should be greater than tolerated player timeout * 2.
            self.MinTimeBetweenTimeStampResets = 4 * 60f;
            self.LastTimeStampResetServerTime = 0f;
            
            self.NetworkSimulatedSmoothLocationTime = 0.100f;
            self.NetworkSimulatedSmoothRotationTime = 0.050f;
            self.ListenServerNetworkSimulatedSmoothLocationTime = 0.040f;
            self.ListenServerNetworkSimulatedSmoothRotationTime = 0.033f;
            self.NetworkMaxSmoothUpdateDistance = 2.56f;
            self.NetworkNoSmoothUpdateDistance = 3.84f;

            self.NetworkMoveDataContainer.Clear();
        }
        private static void Destroy(this CharacterMovementComponent self)
        {
            self.bReplicateMovement = false;
            self.MovementController = null;
            self.NetworkMoveDataContainer.Clear();
        }

        public static void LogicUpdate(this CharacterMovementComponent self, long deltaTimeMS, float deltaTime)
        {
            self.LogicTimeStamp += deltaTimeMS;
            
            if(self.LocalNetRole == ENetRole.SimulatedProxy)
            {
                self.SimulateUpdate(deltaTime);
            }
            else if(self.LocalNetRole >= ENetRole.AutonomousProxy)
            {
                self.PerformUpdate(deltaTime);
            }
        }
        
        public static void SetRuntimeData(this CharacterMovementComponent self, CharacterRuntimeData runtimeData)
        {
            self.RuntimeDataRef = runtimeData;
        }

        public static float MaxStableMoveSpeed(this CharacterMovementComponent self)
        {
            return self.MaxStableMoveSpeed;
        }

        #region Force 
        public static  void AddImpose(this CharacterMovementComponent self, float3 impose)
        {
            self.TotalImpose += impose;
        }

        public static  void ClearImpose(this CharacterMovementComponent self)
        {
            self.TotalImpose = float3.zero;
        }

        public static  void SetConstantForce(this CharacterMovementComponent self, ref int forceId, float3 force)
        {
            if (forceId == 0)
            {
                forceId = (++self._forceIdGenerator);
            }

            if (!self._dicId2ConstantForce.TryGetValue(forceId, out var value))
            {
                self._dicId2ConstantForce[forceId] = float3.zero;
            }
            self. _dicId2ConstantForce[forceId] = force;
        }
        
        public static  float3 GetConstantForce(this CharacterMovementComponent self)
        {
            float3 result = float3.zero;
            foreach (var partialForce in self._dicId2ConstantForce)
            {
                result += partialForce.Value;
            }

            return result;
        }
        
        public static  void RemoveConstantForce(this CharacterMovementComponent self, ref int forceId)
        {
            self._dicId2ConstantForce.Remove(forceId);
            forceId = 0;
        }

        public static  void ClearConstantForce(this CharacterMovementComponent self)
        {
            self._dicId2ConstantForce.Clear();
        }

        public static  void ClearForce(this CharacterMovementComponent self)
        {
            self.ClearImpose();
            self.ClearConstantForce();
        }
        #endregion
        
        #region Network Movement Client

        private static void SimulateUpdate(this CharacterMovementComponent self, float deltaTime)
        {
            
        }

        private static void PerformUpdate(this CharacterMovementComponent self, float deltaTime)
        {
            if (self.LocalNetRole == ENetRole.AutonomousProxy)
            {
                var clientData = self.GetPredictionData_Client_CharacterMovement();
                if (clientData != null && clientData.bUpdatePosition)
                {
                    self.ClientUpdatePositionAfterServerUpdate(deltaTime);
                }

                //TODO: Check Character is Controlled by Local Player
                bool bShouldPerformControlledCharMove = true;

                if (bShouldPerformControlledCharMove)
                {
                    self.ControlledCharacterMove(deltaTime);
                }
            }
        }
        
        private static void ControlledCharacterMove(this CharacterMovementComponent self, float deltaTime)
        {
            if (self.LocalNetRole == ENetRole.Authority)
            {
                self.PerformMovement(deltaTime);
            }
            else if (self.LocalNetRole == ENetRole.AutonomousProxy)
            {
                self.ReplicateMoveToServer(deltaTime);
            }
        }

        private static void ClientUpdatePositionAfterServerUpdate(this CharacterMovementComponent self, float deltaTime)
        {
            
        }
        
        private static void PerformMovement(this CharacterMovementComponent self, float deltaTime)
        {
            
        }

        private static void ReplicateMoveToServer(this CharacterMovementComponent self, float deltaTime)
        {
            var clientData = self.GetPredictionData_Client_CharacterMovement();
            if (clientData == null)
            {
                return;
            }
            
            deltaTime = clientData.UpdateTimeStampAndDeltaTime(deltaTime, self.MinTimeBetweenTimeStampResets);
            
        }

        public static void BeforeUpdateCharacterMovement(this CharacterMovementComponent self, float deltaTime)
        { 
            if (self.LocalNetRole == ENetRole.AutonomousProxy)
            {
                self.OnBeforeCCUpdate_Client(deltaTime);
            }
            else if (self.LocalNetRole == ENetRole.Authority && self.RemoteNetRole ==  ENetRole.AutonomousProxy)
            {
                self.OnBeforeCCUpdate_Server(deltaTime);
            }
        }
        
        public static void EndUpdateCharacterMovement(this CharacterMovementComponent self, float deltaTime)
        {
            if (self.LocalNetRole == ENetRole.AutonomousProxy)
            {
                //Client
                self.OnEndCCUpdate_Client();
            }
            else if (self.LocalNetRole == ENetRole.Authority && self.RemoteNetRole == ENetRole.AutonomousProxy)
            {
                //Server
                self.OnEndCCUpdate_Server();
            }
        }

        private static void OnBeforeCCUpdate_Client(this CharacterMovementComponent self,float deltaTime)
        {
            var clientData = self.GetPredictionData_Client_CharacterMovement();
            if (clientData == null)
            {
                return;
            }
            
            deltaTime = clientData.UpdateTimeStampAndDeltaTime(self.LogicTimeStamp, self.MinTimeBetweenTimeStampResets);
            
            // Find the oldest (unacknowledged) important move (OldMove).
            // Don't include the last move because it may be combined with the next new move.
            // A saved move is interesting if it differs significantly from the last acknowledged move
            SavedMove_Character oldMove = null;
            if (clientData.LastAckedMove != null)
            {
                int NumSavedMoves = clientData.SavedMoves.Count;
                for (int i = 0; i < NumSavedMoves - 1; i++)
                {
                    var currentMove = clientData.SavedMoves[i];
                    if (currentMove.IsImportantMove(clientData.LastAckedMove))
                    {
                        oldMove = currentMove;
                        break;
                    }
                }
            }
            self.OldMove = oldMove;
            
            // Get a SavedMove object to store the movement in.
            var newMove = clientData.CreateSavedMove();
            if (newMove == null)
            {
                return;
            }
                
            newMove.SetMoveFor(
                self,
                self.PackNetworkMovementMode(),
                self.GetMaxSpeed(),
                deltaTime,
                0, self.RoundAcceleration(0),
                clientData);
                
            // see if the two moves could be combined
            if (clientData.PendingMove != null && clientData.PendingMove.CanCombineWith(newMove, self, clientData.MaxMoveDeltaTime))
            {
                //TODO: Only combine and move back to the start location if we don't move back in to a spot that would make us collide with something new.

                
                newMove.CombineWith(clientData.PendingMove, self, float3.zero);


                newMove.SetInitialPosition(self);

                if (clientData.SavedMoves.Count > 0 && clientData.SavedMoves[clientData.SavedMoves.Count - 1] == clientData.PendingMove)
                {
                    clientData.FreeMove(clientData.PendingMove);
                }
                clientData.PendingMove = null;
            }

            self.CurrentMove = newMove;
        }



        private static void OnEndCCUpdate_Client(this CharacterMovementComponent self)
        {
            var clientData = self.GetPredictionData_Client_CharacterMovement();
            if (clientData == null)
            {
                return;
            }

            var oldMove = self.OldMove;
            var newMove = self.CurrentMove;

            if (newMove == null)
            {
                return;
            }
            self.OldMove = null;
            self.CurrentMove = null;
                
            newMove.PostUpdate(self, self.PackNetworkMovementMode());

            if (self.bReplicateMovement)
            {
                clientData.SavedMoves.Add(newMove, out _);

                bool bCanDelayMove = self.CanDelaySendingMove(newMove);
                if (bCanDelayMove && clientData.PendingMove == null)
                {
                    float netMoveDeltaSeconds = math.clamp(self.GetClientNetSendDeltaTime(),  1f/120f, 1f/5f);
                    float secondsSinceLastMoveSent = TimeInfo.Instance.RealTimeSecondSinceStartup - clientData.ClientUpdateRealTime;
                        
                    if (secondsSinceLastMoveSent < netMoveDeltaSeconds)
                    {
                        // Delay sending this move.
                        clientData.PendingMove = newMove;
                        return;
                    }
                }
                    
                clientData.ClientUpdateRealTime =  TimeInfo.Instance.RealTimeSecondSinceStartup;
                    
                bool bSendServerMove = true;

                // Send move to server if this character is replicating movement
                if (bSendServerMove)
                {
                    self.CallServerMove(newMove, clientData.PendingMove, oldMove);
                }
            }

            clientData.PendingMove = null;
        }

        public static void CallServerMove(this CharacterMovementComponent self,
            SavedMove_Character newMove,
            SavedMove_Character pendingMove,
            SavedMove_Character oldMove)
        { 
            if (newMove == null) return;
            
            self.NetworkMoveDataContainer.ClientFillNetworkMoveData(newMove, pendingMove, oldMove);

            using var msg = self.NetworkMoveDataContainer.ToMessage(self.Parent.Id, true);
            
            //Send To Server
#if Client
            var session = MapHelper.GetPlayerSession(self.Scene());
            session.Send(msg);     
#endif
        }
        
        
        
        private static NetworkPredictionData_Client_CharacterMovement GetPredictionData_Client_CharacterMovement(this CharacterMovementComponent self)
        {
            if (self.ClientData == null)
            {
                self.ClientData = new NetworkPredictionData_Client_CharacterMovement(ENetMode.NM_Client,
                    self.NetworkMaxSmoothUpdateDistance,
                    self.NetworkNoSmoothUpdateDistance,
                    self.ListenServerNetworkSimulatedSmoothLocationTime,
                    self.NetworkSimulatedSmoothLocationTime,
                    self.ListenServerNetworkSimulatedSmoothRotationTime,
                    self.NetworkSimulatedSmoothRotationTime);
            }
            return self.ClientData;
        }
        
        public static byte PackNetworkMovementMode(this CharacterMovementComponent self)
        {
            if (self.MovementMode != EMovementMode.Custom)
            {
                byte GroundMoveBit = self.GroundMovementMode == EMovementMode.Walking ? (byte)0 : (byte)1;
                return (byte)((byte)self.MovementMode | (byte)(GroundMoveBit << self.GroundShift));
            }
            else
            {
                return (byte)(self.CustomMovementMode + self.CustomModeThr);
            }
        }
        
        public  static bool CanDelaySendingMove(this CharacterMovementComponent self, SavedMove_Character newMove)
        {
            // Don't delay moves that change movement mode over the course of the move.
            if (newMove.StartPackedMovementMode != newMove.EndPackedMovementMode) 
            {
                return false;
            }

            // If we know we don't want to combine this move, reduce latency and avoid misprediction by flushing immediately.
            if (newMove.bForceNoCombine)
            {
                return false;
            }

            return true;
        }

        public static float GetClientNetSendDeltaTime(this CharacterMovementComponent self)
        {
            var netMoveDelta = GameNetworkManager.Instance.ClientNetSendMoveDeltaTime;
            return netMoveDelta;
        }
        
        public static float GetMaxSpeed(this CharacterMovementComponent self)
        {
            switch (self.MovementMode)
            {
                case EMovementMode.Walking:
                case EMovementMode.NavWalking:
                    return self.MaxStableMoveSpeed;
                case EMovementMode.Falling:
                    return self.MaxAirMoveSpeed;
                default:
                    return self.MaxStableMoveSpeed;

            }
        }
        
        public static float3 RoundAcceleration(this CharacterMovementComponent self, float3 inAccel)
        {
            return mathEx.QuantizeVector(inAccel, 10);
        }
        #endregion
        
        #region Network Movement Server

        public static void ServerMove_ServerReceive(this CharacterMovementComponent self, C2M_CharacterMoveData message)
        {
            self.NetworkMoveDataContainer.Serialize(message);
            self.ServerMove_HandleMoveData(self.NetworkMoveDataContainer);
        }

        private static void ServerMove_HandleMoveData(this CharacterMovementComponent self, CharacterNetworkMoveDataContainer moveDataContainer)
        {
            var serverData = self.GetPredictionData_Server_CharacterMovement();
            if (serverData == null)
            {
                return;
            }
            
            //Optional Old Move
            if (moveDataContainer.bHasOldMove)
            {
                self.SetCurrentNetworkMoveData(moveDataContainer.OldMoveData);
                self.ServerMove_PerformMovement(moveDataContainer.OldMoveData);
            }
            
            // Optional pending move as part of "dual move"
            if (moveDataContainer.bHasPendingMove)
            {
                self.SetCurrentNetworkMoveData(moveDataContainer.PendingMoveData);
                self.ServerMove_PerformMovement(moveDataContainer.PendingMoveData);
            }
            
            // Final standard move
            if (moveDataContainer.NewMoveData != null)
            {
                self.SetCurrentNetworkMoveData(moveDataContainer.NewMoveData);
                self.ServerMove_PerformMovement(moveDataContainer.NewMoveData);
            }
            
            self.SetCurrentNetworkMoveData(null);
        }

        public static void ServerMove_PerformMovement(this CharacterMovementComponent self, CharacterNetworkMoveData moveData)
        {
            var serverData = self.GetPredictionData_Server_CharacterMovement();
            if (serverData == null)
            {
                return;
            }
            
            float clientTimeStamp = moveData.TimeStamp;
            if(!self.VerifyClientTimeStamp(clientTimeStamp, serverData))
            {
                float serverClientTimeStamp = serverData.CurrentClientTimeStamp;
                if (serverClientTimeStamp > 1f 
                    && math.abs(serverClientTimeStamp - clientTimeStamp) > CharacterMovementCVars.NetServerMoveTimestampExpiredWarningThreshold)
                {
                    Log.Warning($"ServerMove: TimeStamp expired:{clientTimeStamp}, CurrentClientTimeStamp: {serverClientTimeStamp}, LastReceivedClientTimeStamp: {serverData.LastReceivedClientTimeStamp}");
                }
                return;
            }

            float3 clientAccel = moveData.Acceleration.Value;
            byte clientMoveFlags = moveData.CompressedMoveFlags;
            
            float deltaTime = serverData.GetServerMoveDeltaTime(clientTimeStamp, 1f);
            if (deltaTime > 0f)
            {
                serverData.CurrentClientTimeStamp = clientTimeStamp;
                serverData.ServerAccumulatedClientTimeStamp += deltaTime;
                serverData.ServerTimeStamp = TimeInfo.Instance.RealTimeSecondSinceStartup;
                serverData.ServerTimeStampLastServerMove = serverData.ServerTimeStamp;

                self.MoveAutonomous(clientTimeStamp, deltaTime, clientMoveFlags, clientAccel);

                //TODO: For Quick Test
                {
                    float3 location = moveData.Location.Value;
                    quaternion rotation = quaternion.Euler(moveData.Rotation);
                    self.KinematicCharacterCmp.SetPositionAndRotation(location, rotation);
                    self.TransformCmp.SetPositionAndRotation(location, rotation);
                    serverData.PendingAdjustment.bAckGoodMove = true;
                }
            }
            
            // Validate move only after old and first dual portion, after all moves are completed.
            if (moveData.NetworkMoveType == CharacterNetworkMoveData.ENetworkMoveType.NewMove)
            {
                
            }
        }

        public static void MoveAutonomous(this CharacterMovementComponent self, float clientTimeStamp, float deltaTime, byte clientMoveFlags, float3 clientAccel)
        {
            
        }
        

        public static bool VerifyClientTimeStamp(this CharacterMovementComponent self, float timeStamp, NetworkPredictionData_Server_CharacterMovement serverData)
        {
            bool bFirstMoveAfterForcedUpdates = serverData.bTriggeringForcedUpdates;
            bool bNeedsForcedUpdate = false;
            bool bIsValid = self.IsClientTimeStampValid(timeStamp, serverData, out bool bTimeStampResetDetected);
            if (bIsValid)
            {
                if (bTimeStampResetDetected)
                {
                    self.LastTimeStampResetServerTime = TimeInfo.Instance.RealTimeSecondSinceStartup;
                    serverData.CurrentClientTimeStamp -= self.MinTimeBetweenTimeStampResets;
                }
                else
                {
                    self.ProcessClientTimeStampForTimeDiscrepancy(timeStamp, serverData);
                }
                
                if (bFirstMoveAfterForcedUpdates)
                {
                    serverData.CurrentClientTimeStamp = timeStamp;
                    serverData.ServerTimeStamp = TimeInfo.Instance.RealTimeSecondSinceStartup;
                }
            }
            else
            {
                if (bTimeStampResetDetected)
                {
                    Log.Debug($"TimeStamp expired. Before TimeStamp Reset. CurrentClientTimeStamp: {serverData.CurrentClientTimeStamp}, LastReceivedClientTimeStamp: {serverData.LastReceivedClientTimeStamp}, TimeStamp: {timeStamp}");
                }
                else
                {
                    
                }
            }

            serverData.LastReceivedClientTimeStamp = timeStamp;
            serverData.bLastRequestNeedsForcedUpdates = bNeedsForcedUpdate;
            
            return bIsValid;
        }

        /// <summary>
        /// Processes client timestamps from ServerMoves, detects and protects against time discrepancy between client-reported times and server time
        /// Called by UCharacterMovementComponent::VerifyClientTimeStamp() for valid timestamps.
        /// </summary>
        /// <param name="self"></param>
        /// <param name="clientTimeStamp"></param>
        /// <param name="serverData"></param>
        private static void ProcessClientTimeStampForTimeDiscrepancy(this CharacterMovementComponent self, float clientTimeStamp, NetworkPredictionData_Server_CharacterMovement serverData)
        {
            // Should only be called on server in network games
            
            // Movement time discrepancy detection and resolution (potentially caused by client speed hacks, time manipulation)
            // Track client reported time deltas through ServerMove RPCs vs actual server time, when error accumulates enough
            // trigger prevention measures where client must "pay back" the time difference
        }

        /// <summary>
        /// Internal const check for client timestamp validity without side-effects. 
        /// </summary>
        /// <param name="self"></param>
        /// <param name="timeStamp"></param>
        /// <param name="serverData"></param>
        /// <param name="bTimeStampResetDetected"></param>
        /// <returns></returns>
        private static bool IsClientTimeStampValid(this CharacterMovementComponent self, float timeStamp, NetworkPredictionData_Server_CharacterMovement serverData, out bool bTimeStampResetDetected)
        {
            bTimeStampResetDetected = false;
            if (timeStamp < 0f || math.isfinite(timeStamp))
            {
                return false;
            }

            // Very large deltas happen around a TimeStamp reset.
            float deltaTimeStamp = (timeStamp - serverData.CurrentClientTimeStamp);
            if (math.abs(deltaTimeStamp) > self.MinTimeBetweenTimeStampResets * 0.5f)
            {
                // Client is resetting TimeStamp to increase accuracy.
                bTimeStampResetDetected = true;
                if (deltaTimeStamp < 0f)
                {
                    // Validate that elapsed time since last reset is reasonable, otherwise client could be manipulating resets.
                    if (TimeInfo.Instance.RealTimeSecondSinceStartup - self.LastTimeStampResetServerTime < (self.MinTimeBetweenTimeStampResets * 0.5f))
                    {
                        // Reset too recently
                        return false;
                    }
                    else
                    {
                        // TimeStamp accepted with reset
                        return true;
                    }
                }
                else
                {
                    // We already reset the TimeStamp, but we just got an old outdated move before the switch, not valid.
                    return false;
                }
            }

            // If TimeStamp is in the past, move is outdated, not valid.
            if( timeStamp <= serverData.CurrentClientTimeStamp )
            {
                return false;
            }
            
            // Precision issues (or reordered timestamps from old moves) can cause very small or zero deltas which cause problems.
            if (deltaTimeStamp < CharacterMovementComponent.MIN_TICK_TIME)
            {
                return false;
            }
	
            // TimeStamp valid.
            return true;
        }

        /// <summary>
        /// Check for Server-Client disagreement in position or other movement state important enough to trigger a client correction.
        /// </summary>
        /// <param name="self"></param>
        /// <param name="clientTimeStamp"></param>
        /// <param name="deltaTime"></param>
        /// <param name="accel"></param>
        /// <param name="location"></param>
        /// <param name="clientMovementMode"></param>
        /// <param name="clientMovementBase"></param>
        /// <param name="clientMovementBaseBoneName"></param>
        private static void ServerMoveHandleClientError(this CharacterMovementComponent self, 
            float clientTimeStamp, float deltaTime,
            float3 accel, float3 location, byte clientMovementMode,
            long clientMovementBase, string clientMovementBaseBoneName)
        {
            
        }

        public static void SendClientAdjustment(this CharacterMovementComponent self)
        {
#if DOTNET
            
            var serverData = self.GetPredictionData_Server_CharacterMovement();
            if (serverData == null)
            {
                return;
            }

            if (serverData.PendingAdjustment.TimeStamp <= 0f)
            {
                return;
            }
            
            if(serverData.PendingAdjustment.bAckGoodMove)
            {
               M2C_AckGoodCharacterMoveData msg_goodMoveData = M2C_AckGoodCharacterMoveData.Create(true); 
               MapMessageHelper.SendToClient(self.Parent, msg_goodMoveData);
            }
            else
            {
                M2C_CharacterMovementRepData msg_repData = M2C_CharacterMovementRepData.Create(true);
                msg_repData.ReplicatedMovement ??= new CharacterRepMovement();
                msg_repData.ReplicatedMovement.CopyFrom(self.ReplicatedMovement); 
                MapMessageHelper.SendToClient(self.Parent, msg_repData);
            }

            serverData.PendingAdjustment.TimeStamp = 0f;
            serverData.PendingAdjustment.bAckGoodMove = false;
            serverData.bForceClientUpdate = false;
#endif
        }

        private static void OnBeforeCCUpdate_Server(this CharacterMovementComponent self, float deltaTime)
        {
            
        }
        

        private static void OnEndCCUpdate_Server(this CharacterMovementComponent self)
        {
            
        }

        public static void GaterCurrentMovement(this CharacterMovementComponent self)
        {
            self.ReplicatedMovement.Location = self.TransformCmp.Transform.Location;
            self.ReplicatedMovement.Rotation = math.Euler(self.TransformCmp.Transform.Rotation);
            self.ReplicatedMovement.LinearVelocity = self.KinematicCharacterCmp.Velocity;
            self.ReplicatedMovement.AngularVelocity = float3.zero;
        }

        public static void SetCurrentNetworkMoveData(this CharacterMovementComponent self, CharacterNetworkMoveData moveData)
        {
            self.CurrentNetworkMoveData = moveData;
        }
        
        public static NetworkPredictionData_Server_CharacterMovement GetPredictionData_Server_CharacterMovement(this CharacterMovementComponent self)
        {
            if (self.ServerData == null)
            {
                self.ServerData = new NetworkPredictionData_Server_CharacterMovement();
            }
            return self.ServerData;
        }
        #endregion
    }
    
    public class CharacterMovementController : ICharacterMovementController
    {
        public CharacterMovementComponent MovementCmp;
        public void UpdateRotation(ref quaternion currentRotation, float deltaTime)
        {
            var Motor = MovementCmp.KinematicCharacterCmp;
            var moveInput = MovementCmp.RuntimeDataRef.IntentMovement;
            var OrientationSharpness = MovementCmp.OrientationSharpness;
            float3 forward = Motor.CharacterForward;
            
            if (!moveInput.Equals(float3.zero) && OrientationSharpness > 0f)
            {
                forward = moveInput;
            }

            if (!MovementCmp.IsSmoothRotationChange)
            {
                currentRotation = quaternion.LookRotation(forward, Motor.CharacterUp);
            }
            else
            {
                float3 smoothedLookInputDirection = mathEx.slerp(Motor.CharacterForward, forward, 1f - math.exp(-OrientationSharpness * deltaTime));
                currentRotation = quaternion.LookRotation(smoothedLookInputDirection, Motor.CharacterUp);
            }

            if (currentRotation.IsNaN())
            {
                System.Diagnostics.Debugger.Break();
            }
        }

        public void UpdateVelocity(ref float3 currentVelocity, float deltaTime)
        {
            var Motor = MovementCmp.KinematicCharacterCmp;
            var runtimeData = MovementCmp.RuntimeDataRef;
            var moveInput = runtimeData.IntentMovement;
            
            if (Motor.GroundingStatus.IsStableOnGround)
            {
                // Reorient velocity on slope
                currentVelocity = Motor.GetDirectionTangentToSurface(currentVelocity, Motor.GroundingStatus.GroundNormal) * math.length(currentVelocity);

                float3 moveDir = Motor.CharacterForward;
                float moveSpeed = 0f;
                float moveMagnitudeFactor = 1f;
                
                moveDir = math.normalizesafe(moveInput);
                moveSpeed = MovementCmp.MaxStableMoveSpeed();
                moveMagnitudeFactor = math.length(moveInput);
                
                // Calculate target velocity
                float3 inputRight = math.cross(moveDir, Motor.CharacterUp);
                float3 reorientedInput = math.normalizesafe(math.cross(Motor.GroundingStatus.GroundNormal, inputRight)) * moveMagnitudeFactor;
                var targetMovementVelocity = reorientedInput * moveSpeed;
                
                // Smooth movement Velocity
                currentVelocity = math.lerp(currentVelocity, targetMovementVelocity, 1f - math.exp(-MovementCmp.StableMovementSharpness * deltaTime));
            }
            else
            {
                if (math.length(currentVelocity) > 0)
                {
                    float3 addedVelocity = moveInput * MovementCmp.AirAccelerationSpeed * deltaTime;
                    float3  currentVelocityOnInputPlane = mathEx.ProjectOnPlane(currentVelocity, Motor.CharacterUp);

                    // Limit air velocity from inputs
                    if (math.length(currentVelocityOnInputPlane) < MovementCmp.MaxAirMoveSpeed)
                    { 
                        // clamp addedVel to make total vel not exceed max vel on inputs plane
                        float3 newTotal = mathEx.ClampMagnitude(currentVelocityOnInputPlane + addedVelocity, MovementCmp.MaxAirMoveSpeed);
                        addedVelocity = newTotal - currentVelocityOnInputPlane;
                    }
                    else
                    {
                        // Make sure added vel doesn't go in the direction of the already-exceeding velocity
                        if (math.dot(currentVelocityOnInputPlane, addedVelocity) > 0)
                        {
                            addedVelocity = mathEx.ProjectOnPlane(addedVelocity, math.normalize(currentVelocityOnInputPlane));
                        }
                    }
                    
                    // Prevent air-climbing sloped walls
                    if (Motor.GroundingStatus.FoundAnyGround)
                    {
                        if (math.dot(currentVelocity + addedVelocity, addedVelocity) > 0)
                        {
                            float3 perpendicularObstructionNormal = math.cross(math.cross(Motor.CharacterUp, Motor.GroundingStatus.GroundNormal), Motor.CharacterUp);
                            addedVelocity = mathEx.ProjectOnPlane(addedVelocity, perpendicularObstructionNormal);
                        }
                    }
                    
                    
                    // Apply added velocity
                    currentVelocity += addedVelocity;
                }
                
                if (!MovementCmp.IgnoreGravity)
                {
                    // Gravity
                    currentVelocity += MovementCmp.Gravity * deltaTime;
                }
                
                if (!MovementCmp.IgnoreAirDrag)
                {
                    // Drag
                    currentVelocity *= (1f / (1f + (MovementCmp.AirDrag * deltaTime)));
                }
            }
#if Client
            
            Bliss.Debugger.GameDebugStats.GameDebugStates.Instance.SetDebugStat("Jump", "WantsToJump", runtimeData.WantsToJump, new float4(0, 0, 0, 1), true);
            Bliss.Debugger.GameDebugStats.GameDebugStates.Instance.SetDebugStat("Jump", "JumpConsumed", runtimeData.JumpConsumed, new float4(0, 0, 0, 1), true);
            Bliss.Debugger.GameDebugStats.GameDebugStates.Instance.SetDebugStat("Jump", "JumpedThisFrame", runtimeData.JumpedThisFrame, new float4(0, 0, 0, 1), true);
            Bliss.Debugger.GameDebugStats.GameDebugStates.Instance.SetDebugStat("Jump", "CurrentJumpCount", MovementCmp.CurrentJumpCount, new float4(0, 0, 0, 1), true);
            Bliss.Debugger.GameDebugStats.GameDebugStates.Instance.SetDebugStat("Jump", "TimeSinceLastInJumpableEnvironment", runtimeData.TimeSinceLastInJumpableEnvironment, new float4(0, 0, 0, 1), false);
#endif

            //Jump
            runtimeData.JumpedThisFrame = false;
            if (runtimeData.WantsToJump && !runtimeData.JumpConsumed && MovementCmp.CurrentJumpCount < MovementCmp.MaxJumpCount)
            {
                // See if we actually are allowed to jump
                bool inJumpableEnvironment = MovementCmp.AllowJumpingWhenSliding
                    ? Motor.GroundingStatus.FoundAnyGround
                    : Motor.GroundingStatus.IsStableOnGround;

#if Client
                Bliss.Debugger.GameDebugStats.GameDebugStates.Instance.SetDebugStat("Jump", "inJumpableEnvironment", inJumpableEnvironment, new float4(0, 0, 0, 1), true);
#endif
                
                if (inJumpableEnvironment || runtimeData.TimeSinceLastInJumpableEnvironment <= MovementCmp.JumpPostGroundingGraceTime)
                {
                    // Calculate jump direction before ungrounding
                    float3 jumpDirection = Motor.CharacterUp;
                    if (Motor.GroundingStatus.FoundAnyGround && !Motor.GroundingStatus.IsStableOnGround)
                    {
                        jumpDirection = Motor.GroundingStatus.GroundNormal;
                    }
                    
                    Motor.ForceUnground();
                    
                    currentVelocity += (jumpDirection * MovementCmp.JumpUpSpeed) - math.project(currentVelocity, Motor.CharacterUp);
                    currentVelocity += (math.normalizesafe(moveInput) * MovementCmp.JumpScalableForwardSpeed);
                    
                    runtimeData.JumpedThisFrame = true;
                    runtimeData.JumpConsumed = true;
                    runtimeData.JumpConsumedTimeStamp = TimeInfo.Instance.ClientFrameTime();
                    
                    MovementCmp.CurrentJumpCount++;
                }
            }

            //Force
            float3 totalForce = MovementCmp.GetConstantForce();
            currentVelocity += totalForce * deltaTime;
            if (!MovementCmp.TotalImpose.Equals(float3.zero))
            {
                currentVelocity += MovementCmp.TotalImpose;
                MovementCmp.ClearImpose();
            }

            if (float.IsNaN(currentVelocity.y))
            {
                System.Diagnostics.Debugger.Break();
            }
#if Client
            Bliss.Debugger.GameDebugStats.GameDebugStates.Instance.SetDebugStat("Jump", "currentVelocity", currentVelocity, new float4(0, 0, 0, 1));
#endif
        }

        public void BeforeCharacterUpdate(float deltaTime)
        {
            MovementCmp.BeforeUpdateCharacterMovement(deltaTime);
        }

        public void PostGroundingUpdate(float deltaTime)
        {
            var Motor = MovementCmp.KinematicCharacterCmp;
            
            if (Motor.LastGroundingStatus.IsStableOnGround
                && !Motor.GroundingStatus.IsStableOnGround)
            {
                //Leave Ground 
            }
            
            if (!Motor.LastGroundingStatus.IsStableOnGround
                && Motor.GroundingStatus.IsStableOnGround)
            {
                //Land

            }
        }

        public void AfterCharacterUpdate(float deltaTime)
        {
            var Motor = MovementCmp.KinematicCharacterCmp;
            var runtimeData = MovementCmp.RuntimeDataRef;
            MovementCmp.RuntimeDataRef.CharacterUp = Motor.CharacterUp;
            
            bool inJumpableEnvironment = MovementCmp.AllowJumpingWhenSliding
                ? Motor.GroundingStatus.FoundAnyGround
                : Motor.GroundingStatus.IsStableOnGround;
            
            if (inJumpableEnvironment)
            {
                // If we're on a ground surface, reset jumping values
                if (!runtimeData.JumpedThisFrame)
                {
                    runtimeData.JumpConsumed = false;
                    runtimeData.JumpConsumedTimeStamp = 0;
                    MovementCmp.CurrentJumpCount = 0;
                }

                runtimeData.TimeSinceLastInJumpableEnvironment = 0f;
            }
            else
            {
                // Keep track of time since we were last able to jump (for grace period)
                runtimeData.TimeSinceLastInJumpableEnvironment += deltaTime;
            }
            
            MovementCmp.EndUpdateCharacterMovement(deltaTime);
        }

        public bool IsColliderValidForCollisions(GameplayCollider coll)
        {
            if (MovementCmp.IgnoredColliders.Count == 0)
            {
                return true;
            }
            
            EntityRef<GameplayCollider> collRef = coll;
            return !MovementCmp.IgnoredColliders.Contains(collRef);
        }

        public void OnGroundHit(GameplayCollider hitCollider, float3 hitNormal, float3 hitPoint,
            ref HitStabilityReport hitStabilityReport)
        {
            
        }

        public void OnMovementHit(GameplayCollider hitCollider, float3 hitNormal, float3 hitPoint,
            ref HitStabilityReport hitStabilityReport)
        {
            
        }

        public void ProcessHitStabilityReport(GameplayCollider hitCollider, float3 hitNormal, float3 hitPoint,
            float3 atCharacterPosition, quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport)
        {
            
        }

        public void OnDiscreteCollisionDetected(GameplayCollider hitCollider)
        {
            
        }

        public long GetLogicTimeStamp()
        {
            return this.MovementCmp.LogicTimeStamp;
        }
    }
}