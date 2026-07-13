namespace Bliss.CharacterMovement
{
    public class NetworkPredictionData_Server_CharacterMovement
    {
        /** Timestamp from the client of most recent ServerMove() processed for this player. Reset occasionally for timestamp resets (to maintain accuracy). */
        public float CurrentClientTimeStamp;

        /** Timestamp from the client of most recent ServerMove() received for this player, including rejected requests. */
        public float LastReceivedClientTimeStamp;

        /** Timestamp of total elapsed client time. Similar to CurrentClientTimestamp but this is accumulated with the calculated DeltaTime for each move on the server. */
        public double ServerAccumulatedClientTimeStamp;

        /** Last time server updated client with a move correction */
        public float LastUpdateTime;

        /** Server clock time when last server move was received from client (does NOT include forced moves on server) */
        public float ServerTimeStampLastServerMove;
        
        /** 
         * Max delta time for a given move, in real seconds
         * Based off of AGameNetworkManager::MaxMoveDeltaTime config setting, but can be modified per actor
         * if needed.
         * Note: This was previously named MaxResponseTime, but has been renamed to reflect what it does more accurately
         */
        public float MaxMoveDeltaTime;

        /** Force client update on the next ServerMoveHandleClientError() call. */
        public bool bForceClientUpdate = true;

        /** Accumulated timestamp difference between autonomous client and server for tracking long-term trends */
        public float LifetimeRawTimeDiscrepancy;

        /** 
         * Current time discrepancy between client-reported moves and time passed
         * on the server. Time discrepancy resolution's goal is to keep this near zero.
         */
        public float TimeDiscrepancy;

        /** True if currently in the process of resolving time discrepancy */
        public bool bResolvingTimeDiscrepancy;

        /** 
         * When bResolvingTimeDiscrepancy is true, we are in time discrepancy resolution mode whose output is
         * this value (to be used as the DeltaTime for current ServerMove)
         */
        public float TimeDiscrepancyResolutionMoveDeltaOverride;

        /** 
         * When bResolvingTimeDiscrepancy is true, we are in time discrepancy resolution mode where we bound
         * move deltas by Server Deltas. In cases where there are multiple ServerMove RPCs handled within one
         * server frame tick, we need to accumulate the client deltas of the "no tick" Moves so that the next
         * Move processed that the server server has ticked for takes into account those previous deltas. 
         * If we did not use this, the higher the framerate of a client vs the server results in more 
         * punishment/payback time.
         */
        public float TimeDiscrepancyAccumulatedClientDeltasSinceLastServerTick;

        /** Creation time of this prediction data, used to contextualize LifetimeRawTimeDiscrepancy */
        public float WorldCreationTime;
    }
}