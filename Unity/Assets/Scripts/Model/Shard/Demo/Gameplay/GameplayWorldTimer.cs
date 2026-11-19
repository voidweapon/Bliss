#nullable enable
using System;
using System.Collections.Generic;
using ET;

namespace Bliss
{
    /// <summary>
    /// Schedules callbacks and waits against the server time of one GameplayWorld.
    /// Due callbacks run during world updates in due-time and registration order.
    /// </summary>
    public sealed class GameplayWorldTimer
    {
        private const long INVALID_TIMER_ID = 0;
        private const long NO_REPEAT_INTERVAL = 0;
        private const long NO_DELAY = 0;
        // Wait entries need a callback for scheduling, but Update completes their task through Completion instead.
        private static readonly Action waitCallback = completeWait;

        private readonly GameplayWorld gameplayWorld;
        private readonly GameplayWorldTimerSchedule dueTimers = new GameplayWorldTimerSchedule();
        /// <summary>
        /// key: Timer identifier; value: active Timer state.
        /// </summary>
        private readonly Dictionary<long, GameplayWorldTimerEntry> timerEntries =
            new Dictionary<long, GameplayWorldTimerEntry>();
        private readonly List<GameplayWorldTimerEntry> ownedTimerEntries =
            new List<GameplayWorldTimerEntry>();
        private long nextTimerId;
        private long nextRegistrationOrder;
        private bool isDisposed;

        internal GameplayWorldTimer(GameplayWorld gameplayWorld)
        {
            this.gameplayWorld = gameplayWorld;
        }

        /// <summary>
        /// Schedules a callback to run after a delay in milliseconds.
        /// </summary>
        /// <param name="delayMilliseconds">Non-negative delay from the current server time.</param>
        /// <param name="callback">Callback to invoke when the timer becomes due.</param>
        /// <param name="owner">Optional entity in this GameplayWorld; the timer is skipped if it becomes invalid.</param>
        /// <returns>The identifier used to cancel this timer.</returns>
        /// <exception cref="ArgumentOutOfRangeException">The delay is negative.</exception>
        /// <exception cref="ArgumentNullException">The callback is null.</exception>
        /// <exception cref="InvalidOperationException">The World is faulted or the owner does not belong to it.</exception>
        /// <exception cref="ObjectDisposedException">A future wait is requested after this timer or its GameplayWorld is disposed.</exception>
        public long ScheduleOnce(long delayMilliseconds, Action callback, Entity? owner = null)
        {
            if (delayMilliseconds < NO_DELAY)
            {
                throw new ArgumentOutOfRangeException(nameof(delayMilliseconds));
            }

            return this.schedule(
                TimeInfo.Instance.ServerFrameTime() + delayMilliseconds,
                NO_REPEAT_INTERVAL,
                callback,
                owner);
        }

        /// <summary>
        /// Schedules a callback to run once at an absolute server timestamp in milliseconds.
        /// </summary>
        /// <param name="dueTimestampMilliseconds">Absolute server timestamp at which the timer becomes due.</param>
        /// <param name="callback">Callback to invoke when the timer becomes due.</param>
        /// <param name="owner">Optional entity in this GameplayWorld; the timer is skipped if it becomes invalid.</param>
        /// <returns>The identifier used to cancel this timer.</returns>
        /// <exception cref="ArgumentNullException">The callback is null.</exception>
        /// <exception cref="InvalidOperationException">The World is faulted or the owner does not belong to it.</exception>
        /// <exception cref="ObjectDisposedException">A future wait is requested after this timer or its GameplayWorld is disposed.</exception>
        public long ScheduleOnceAt(long dueTimestampMilliseconds, Action callback, Entity? owner = null)
        {
            return this.schedule(
                dueTimestampMilliseconds,
                NO_REPEAT_INTERVAL,
                callback,
                owner);
        }

        /// <summary>
        /// Schedules a callback to repeat at the given interval in milliseconds.
        /// A delayed update invokes the callback once, then schedules the next run from the current server time.
        /// </summary>
        /// <param name="intervalMilliseconds">Positive interval between callback runs.</param>
        /// <param name="callback">Callback to invoke each time the timer becomes due.</param>
        /// <param name="owner">Optional entity in this GameplayWorld; the timer is cancelled if it becomes invalid.</param>
        /// <returns>The identifier used to cancel this timer.</returns>
        /// <exception cref="ArgumentOutOfRangeException">The interval is not positive.</exception>
        /// <exception cref="ArgumentNullException">The callback is null.</exception>
        /// <exception cref="InvalidOperationException">The World is faulted or the owner does not belong to it.</exception>
        /// <exception cref="ObjectDisposedException">A future wait is requested after this timer or its GameplayWorld is disposed.</exception>
        public long ScheduleRepeated(long intervalMilliseconds, Action callback, Entity? owner = null)
        {
            if (intervalMilliseconds <= NO_DELAY)
            {
                throw new ArgumentOutOfRangeException(nameof(intervalMilliseconds));
            }

            long currentTime = TimeInfo.Instance.ServerFrameTime();
            return this.schedule(
                currentTime + intervalMilliseconds,
                intervalMilliseconds,
                callback,
                owner);
        }

        /// <summary>
        /// Cancels an active timer and clears the caller's identifier. A pending wait completes with <see langword="false"/>.
        /// </summary>
        /// <param name="timerId">Identifier returned when the timer was scheduled; cleared even if no active timer exists.</param>
        /// <returns><see langword="true"/> if an active timer was cancelled; otherwise, <see langword="false"/>.</returns>
        public bool Cancel(ref long timerId)
        {
            long cancelledTimerId = timerId;
            // Cancellation can resume a wait synchronously and replace the caller's identifier.
            timerId = INVALID_TIMER_ID;
            return this.cancel(cancelledTimerId);
        }

        private bool cancel(long timerId)
        {
            if (timerId == INVALID_TIMER_ID ||
                !this.timerEntries.TryGetValue(timerId, out GameplayWorldTimerEntry? timerEntry) ||
                timerEntry is null)
            {
                return false;
            }

            this.timerEntries.Remove(timerId);
            if (timerEntry.HasOwner)
            {
                this.ownedTimerEntries.Remove(timerEntry);
            }

            this.dueTimers.remove(timerEntry);
            this.unregisterCancellation(timerEntry);
            timerEntry.Completion?.SetResult(false);
            return true;
        }

        /// <summary>
        /// Waits for a duration measured in milliseconds of server time.
        /// </summary>
        /// <param name="durationMilliseconds">Duration to wait; zero or a negative value completes immediately.</param>
        /// <param name="cancellationToken">Optional token that cancels the wait.</param>
        /// <param name="owner">Optional entity in this GameplayWorld; disposing it cancels the wait.</param>
        /// <returns><see langword="true"/> when the duration elapses; otherwise, <see langword="false"/> if cancelled.</returns>
        /// <exception cref="InvalidOperationException">A future wait is requested on a faulted World or the owner is invalid.</exception>
        /// <exception cref="ObjectDisposedException">This timer or its GameplayWorld is disposed.</exception>
        public async ETTask<bool> WaitDurationAsync(
            long durationMilliseconds,
            ETCancellationToken? cancellationToken = null,
            Entity? owner = null)
        {
            if (cancellationToken != null && cancellationToken.IsDispose())
            {
                return false;
            }

            if (durationMilliseconds <= NO_DELAY)
            {
                return true;
            }

            return await this.WaitUntilAsync(
                TimeInfo.Instance.ServerFrameTime() + durationMilliseconds,
                cancellationToken,
                owner);
        }

        /// <summary>
        /// Waits until an absolute server timestamp in milliseconds.
        /// </summary>
        /// <param name="dueTimestampMilliseconds">Absolute server timestamp at which the wait completes.</param>
        /// <param name="cancellationToken">Optional token that cancels the wait.</param>
        /// <param name="owner">Optional entity in this GameplayWorld; disposing it cancels the wait.</param>
        /// <returns><see langword="true"/> when the timestamp is reached; otherwise, <see langword="false"/> if cancelled.</returns>
        /// <exception cref="InvalidOperationException">A future wait is requested on a faulted World or the owner is invalid.</exception>
        /// <exception cref="ObjectDisposedException">This timer or its GameplayWorld is disposed.</exception>
        public async ETTask<bool> WaitUntilAsync(
            long dueTimestampMilliseconds,
            ETCancellationToken? cancellationToken = null,
            Entity? owner = null)
        {
            if (cancellationToken != null && cancellationToken.IsDispose())
            {
                return false;
            }

            if (TimeInfo.Instance.ServerFrameTime() >= dueTimestampMilliseconds)
            {
                return true;
            }

            var completion = ETTask<bool>.Create(true);
            this.scheduleWait(dueTimestampMilliseconds, completion, cancellationToken, owner);
            return await completion;
        }

        /// <summary>
        /// Waits for the next update processed by this GameplayWorld's timer.
        /// </summary>
        /// <param name="cancellationToken">Optional token that cancels the wait.</param>
        /// <param name="owner">Optional entity in this GameplayWorld; disposing it cancels the wait.</param>
        /// <returns><see langword="true"/> when the next timer update is processed; otherwise, <see langword="false"/> if cancelled.</returns>
        /// <exception cref="InvalidOperationException">A wait is requested on a faulted World or the owner is invalid.</exception>
        /// <exception cref="ObjectDisposedException">This timer or its GameplayWorld is disposed.</exception>
        public async ETTask<bool> WaitNextFrameAsync(
            ETCancellationToken? cancellationToken = null,
            Entity? owner = null)
        {
            if (cancellationToken != null && cancellationToken.IsDispose())
            {
                return false;
            }

            var completion = ETTask<bool>.Create(true);
            this.scheduleWait(
                TimeInfo.Instance.ServerFrameTime() + NO_DELAY,
                completion,
                cancellationToken,
                owner);
            return await completion;
        }

        private long schedule(long dueTime, long repeatedInterval, Action callback, Entity? owner)
        {
            if (this.isDisposed || this.gameplayWorld.IsDisposed)
            {
                throw new ObjectDisposedException(nameof(GameplayWorldTimer));
            }

            if (this.gameplayWorld.IsFaulted)
            {
                throw new InvalidOperationException("The GameplayWorld has stopped after a failure.", this.gameplayWorld.Failure);
            }

            if (callback is null)
            {
                throw new ArgumentNullException(nameof(callback));
            }

            if (owner != null && !this.belongsToWorld(owner))
            {
                throw new InvalidOperationException(
                    $"Timer owner {owner.GetType().FullName} does not belong to {nameof(GameplayWorld)} " +
                    $"'{this.gameplayWorld.Name}'.");
            }

            long timerId = ++this.nextTimerId;
            var timerEntry = new GameplayWorldTimerEntry(
                timerId,
                ++this.nextRegistrationOrder,
                dueTime,
                repeatedInterval,
                callback,
                owner);

            this.timerEntries.Add(timerId, timerEntry);
            if (timerEntry.HasOwner)
            {
                this.ownedTimerEntries.Add(timerEntry);
            }

            this.dueTimers.add(timerEntry);
            return timerId;
        }

        private void scheduleWait(
            long dueTime,
            ETTask<bool> completion,
            ETCancellationToken? cancellationToken,
            Entity? owner)
        {
            long timerId = this.schedule(dueTime, NO_REPEAT_INTERVAL, waitCallback, owner);
            GameplayWorldTimerEntry timerEntry = this.timerEntries[timerId];
            timerEntry.Completion = completion;

            if (cancellationToken != null)
            {
                timerEntry.CancellationToken = cancellationToken;
                timerEntry.CancellationAction = () => this.cancel(timerId);
                cancellationToken.Add(timerEntry.CancellationAction);
            }
        }

        private static void completeWait()
        {
        }

        internal void update()
        {
            if (this.isDisposed || this.gameplayWorld.IsFaulted || this.dueTimers.IsEmpty)
            {
                return;
            }

            this.cancelInvalidOwnerTimers();
            if (this.dueTimers.IsEmpty)
            {
                return;
            }

            long currentTime = TimeInfo.Instance.ServerFrameTime();
            List<GameplayWorldTimerEntry> dueEntries = this.dueTimers.getDueSnapshot(currentTime);

            foreach (GameplayWorldTimerEntry timerEntry in dueEntries)
            {
                if (!this.timerEntries.TryGetValue(timerEntry.TimerId, out GameplayWorldTimerEntry? currentEntry) ||
                    currentEntry is null ||
                    !ReferenceEquals(currentEntry, timerEntry))
                {
                    continue;
                }

                this.dueTimers.remove(timerEntry);

                if (timerEntry.HasOwner && !this.ownerIsValid(timerEntry))
                {
                    this.cancel(timerEntry.TimerId);
                    continue;
                }

                if (timerEntry.RepeatedInterval > NO_REPEAT_INTERVAL)
                {
                    timerEntry.DueTime = currentTime + timerEntry.RepeatedInterval;
                    this.dueTimers.add(timerEntry);
                }
                else
                {
                    this.timerEntries.Remove(timerEntry.TimerId);
                    if (timerEntry.HasOwner)
                    {
                        this.ownedTimerEntries.Remove(timerEntry);
                    }

                    this.unregisterCancellation(timerEntry);
                }

                if (timerEntry.Completion != null)
                {
                    timerEntry.Completion.SetResult(true);
                    continue;
                }

                try
                {
                    timerEntry.Callback();
                }
                catch (Exception exception)
                {
                    throw new GameplayWorldTimerException(this.gameplayWorld, timerEntry.TimerId, exception);
                }
            }
        }

        private void cancelInvalidOwnerTimers()
        {
            int entryIndex = this.ownedTimerEntries.Count;
            while (entryIndex > 0)
            {
                entryIndex--;
                GameplayWorldTimerEntry timerEntry = this.ownedTimerEntries[entryIndex];
                if (!this.ownerIsValid(timerEntry))
                {
                    this.cancel(timerEntry.TimerId);
                }
            }
        }

        internal void dispose()
        {
            if (this.isDisposed)
            {
                return;
            }

            this.isDisposed = true;
            this.dueTimers.clear();

            var pendingEntries = new List<GameplayWorldTimerEntry>(this.timerEntries.Values);
            foreach (GameplayWorldTimerEntry timerEntry in pendingEntries)
            {
                this.cancel(timerEntry.TimerId);
            }
        }

        private void unregisterCancellation(GameplayWorldTimerEntry timerEntry)
        {
            if (timerEntry.CancellationToken != null && timerEntry.CancellationAction != null)
            {
                timerEntry.CancellationToken.Remove(timerEntry.CancellationAction);
            }
        }

        private bool ownerIsValid(GameplayWorldTimerEntry timerEntry)
        {
            if (!timerEntry.HasOwner)
            {
                return true;
            }

            if (timerEntry.Owner.Validate())
            {
                Entity? owner = (Entity)timerEntry.Owner;
                if (owner != null && this.belongsToWorld(owner))
                {
                    return true;
                }
            }

            Log.Warning(
                $"Skipping timer {timerEntry.TimerId} for invalid owner in " +
                $"{nameof(GameplayWorld)} '{this.gameplayWorld.Name}'.");
            return false;
        }

        private bool belongsToWorld(Entity entity)
        {
            if (entity.IsDisposed)
            {
                return false;
            }

            for (Entity current = entity; current != null; current = current.Parent)
            {
                if (ReferenceEquals(current, this.gameplayWorld))
                {
                    return true;
                }
            }

            return false;
        }
    }

    internal sealed class GameplayWorldTimerEntry
    {
        internal long TimerId { get; }
        internal long RegistrationOrder { get; }
        internal long DueTime { get; set; }
        internal long RepeatedInterval { get; }
        internal Action Callback { get; }
        internal EntityRef<Entity> Owner { get; }
        internal bool HasOwner { get; }
        internal ETTask<bool>? Completion { get; set; }
        internal ETCancellationToken? CancellationToken { get; set; }
        internal Action? CancellationAction { get; set; }
        internal LinkedListNode<GameplayWorldTimerEntry> ScheduleNode { get; }
        internal GameplayWorldTimerBucket? ScheduleBucket { get; set; }

        internal GameplayWorldTimerEntry(
            long timerId,
            long registrationOrder,
            long dueTime,
            long repeatedInterval,
            Action callback,
            Entity? owner)
        {
            this.TimerId = timerId;
            this.RegistrationOrder = registrationOrder;
            this.DueTime = dueTime;
            this.RepeatedInterval = repeatedInterval;
            this.Callback = callback;
            this.Owner = owner == null ? default : new EntityRef<Entity>(owner);
            this.HasOwner = owner != null;
            this.ScheduleNode = new LinkedListNode<GameplayWorldTimerEntry>(this);
        }
    }

    internal sealed class GameplayWorldTimerSchedule
    {
        /// <summary>
        /// key: due timestamp in milliseconds; value: timers due at that time, ordered by registration.
        /// </summary>
        private readonly SortedDictionary<long, GameplayWorldTimerBucket> buckets =
            new SortedDictionary<long, GameplayWorldTimerBucket>();

        internal bool IsEmpty => this.buckets.Count == 0;

        internal void add(GameplayWorldTimerEntry timerEntry)
        {
            if (!this.buckets.TryGetValue(timerEntry.DueTime, out GameplayWorldTimerBucket? bucket))
            {
                bucket = new GameplayWorldTimerBucket();
                this.buckets.Add(timerEntry.DueTime, bucket);
            }

            bucket.add(timerEntry);
            timerEntry.ScheduleBucket = bucket;
        }

        internal void remove(GameplayWorldTimerEntry timerEntry)
        {
            GameplayWorldTimerBucket? bucket = timerEntry.ScheduleBucket;
            if (bucket == null)
            {
                return;
            }

            bucket.remove(timerEntry);
            timerEntry.ScheduleBucket = null;
            if (bucket.IsEmpty)
            {
                this.buckets.Remove(timerEntry.DueTime);
            }
        }

        internal List<GameplayWorldTimerEntry> getDueSnapshot(long currentTime)
        {
            var snapshot = new List<GameplayWorldTimerEntry>();
            foreach (KeyValuePair<long, GameplayWorldTimerBucket> bucket in this.buckets)
            {
                if (bucket.Key > currentTime)
                {
                    break;
                }

                bucket.Value.copyTo(snapshot);
            }

            return snapshot;
        }

        internal void clear()
        {
            this.buckets.Clear();
        }
    }

    internal sealed class GameplayWorldTimerBucket
    {
        private readonly LinkedList<GameplayWorldTimerEntry> entries = new LinkedList<GameplayWorldTimerEntry>();

        internal bool IsEmpty => this.entries.Count == 0;

        internal void add(GameplayWorldTimerEntry timerEntry)
        {
            LinkedListNode<GameplayWorldTimerEntry>? previous = this.entries.Last;
            // Repeated timers retain their original registration order when joining an existing bucket.
            while (previous != null && previous.Value.RegistrationOrder > timerEntry.RegistrationOrder)
            {
                previous = previous.Previous;
            }

            if (previous == null)
            {
                this.entries.AddFirst(timerEntry.ScheduleNode);
            }
            else
            {
                this.entries.AddAfter(previous, timerEntry.ScheduleNode);
            }
        }

        internal void remove(GameplayWorldTimerEntry timerEntry)
        {
            if (ReferenceEquals(timerEntry.ScheduleNode.List, this.entries))
            {
                this.entries.Remove(timerEntry.ScheduleNode);
            }
        }

        internal void copyTo(List<GameplayWorldTimerEntry> destination)
        {
            foreach (GameplayWorldTimerEntry timerEntry in this.entries)
            {
                destination.Add(timerEntry);
            }
        }
    }

    /// <summary>
    /// Reports an exception thrown by a GameplayWorld timer callback.
    /// The original callback exception is available as <see cref="Exception.InnerException"/>.
    /// </summary>
    public sealed class GameplayWorldTimerException : Exception
    {
        internal GameplayWorldTimerException(GameplayWorld gameplayWorld, long timerId, Exception innerException)
            : base(
                $"Timer {timerId} callback failed in {nameof(GameplayWorld)} '{gameplayWorld.Name}'.",
                innerException)
        {
        }
    }
}
