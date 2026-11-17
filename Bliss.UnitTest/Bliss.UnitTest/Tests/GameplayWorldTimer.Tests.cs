using System.Reflection;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Bliss.Core;
using ET;

namespace Bliss.Tests;

[NonParallelizable]
public class GameplayWorldTimerTests
{
    private const int ROOT_ID = 101;
    private const int ROOT_ZONE = 1;
    private const long GAMEPLAY_WORLD_ID = 202;
    private const int LEVEL_SCENE_ID = 303;
    private const long GAMEPLAY_WORLD_INSTANCE_ID = 10001;
    private const long SECOND_WORLD_ID = 204;
    private const long SECOND_WORLD_INSTANCE_ID = 10004;
    private const long THIRD_WORLD_ID = 205;
    private const long THIRD_WORLD_INSTANCE_ID = 10005;
    private const long REUSED_INSTANCE_ID = 99999;
    private const long FIRST_LOGIC_TICK_TIME = 1050;
    private const long SECOND_LOGIC_TICK_TIME = 1100;
    private const long DELAYED_LOGIC_TICK_TIME = 1500;
    private const long FOLLOWING_LOGIC_TICK_TIME = 1550;
    private const float EXPECTED_LOGIC_DELTA_SECONDS = 0.05f;
    private const int TEST_LOGIC_PRIORITY = 100;
    private const int LATER_LOGIC_PRIORITY = 200;
    private const long IMMEDIATE_DELAY_MILLISECONDS = 0;
    private const long INVALID_DELAY_MILLISECONDS = -1;
    private const long INVALID_TIMER_ID = 0;
    private const int EXPECTED_CALLBACK_COUNT = 1;
    private const int CANCELLED_CALLBACK_INCREMENT = 100;
    private const int OWNER_INVALID_WARNING_COUNT = 3;
    private const long FIRST_TEST_TIME = 1000;
    private const long EARLIER_DUE_TIME = 1010;
    private const long SAME_DUE_TIME = 1020;
    private const long REPEATED_INTERVAL_MILLISECONDS = 10;
    private const long DELAYED_SERVER_TIME = 1050;
    private const long NEXT_REPEATED_DUE_TIME = 1060;
    private const long WAIT_DEADLINE = 1010;
    private const long WAIT_DURATION_MILLISECONDS = 10;
    private const long OWNER_TIMER_FUTURE_DEADLINE = 2000;
    private const int FIRST_CALLBACK_INCREMENT = 1;
    private const int DEFERRED_CALLBACK_INCREMENT = 10;
    private const int SECOND_INVOCATION_COUNT = 2;

    private TypeManager _typeManager;
    private EntitySystemManager _entitySystemManager;
    private EventSystem _eventSystem;
    private Root _root;
    private GameplayWorld _gameplayWorld;
    private ILogger _previousLogger;
    private RecordingLogger _logger;

    [SetUp]
    public void SetUp()
    {
        TimeInfo.Instance.ServerMinusClientTime = 0;
        _previousLogger = Log.Logger;
        _logger = new RecordingLogger();
        Log.Logger = _logger;

        _typeManager = new TypeManager();
        _typeManager.Init(new List<Assembly>
        {
            typeof(Entity).Assembly,
            typeof(GameplayWorldTimerTests).Assembly,
        });
        _typeManager.Register();

        _entitySystemManager = new EntitySystemManager();
        _entitySystemManager.Init();
        _entitySystemManager.Register();
        _root = new Root(ROOT_ID, ROOT_ZONE, ELogicSceneType.Main, nameof(GameplayWorldTimerTests));
        _eventSystem = new EventSystem();
        _eventSystem.Init();
        _eventSystem.Register();

        _gameplayWorld = GameplayWorldFactory.Create(
            _root.Scene,
            GAMEPLAY_WORLD_ID,
            GAMEPLAY_WORLD_INSTANCE_ID,
            nameof(GameplayWorld));
        var levelScene = LevelSceneFactory.Create(
            _gameplayWorld,
            _gameplayWorld,
            LEVEL_SCENE_ID,
            LEVEL_SCENE_ID,
            nameof(LEVEL_SCENE_ID)).GetAwaiter().GetResult();
        LevelSceneFactory.Enable(levelScene).GetAwaiter().GetResult();
    }

    [TearDown]
    public void TearDown()
    {
        _eventSystem?.Dispose();
        _gameplayWorld?.Dispose();
        _root?.Dispose();
        _entitySystemManager.Dispose();
        _typeManager.Dispose();
        TimeInfo.Instance.ServerMinusClientTime = 0;
        Log.Logger = _previousLogger;
    }

    /// <summary>Checks that all World timers precede all World logic ticks in one Root.</summary>
    [Test]
    public void RootRunsEveryWorldTimerBeforeAnyWorldLogicTick()
    {
        setServerTime(FIRST_TEST_TIME);
        GameplayWorld first = GameplayWorldFactory.Create(
            _root.Scene, SECOND_WORLD_ID, SECOND_WORLD_INSTANCE_ID, nameof(SECOND_WORLD_ID));
        GameplayWorld second = GameplayWorldFactory.Create(
            _root.Scene, THIRD_WORLD_ID, THIRD_WORLD_INSTANCE_ID, nameof(THIRD_WORLD_ID));
        TimerOwnerEntity firstParticipant = first.AddChild<TimerOwnerEntity>();
        TimerOwnerEntity secondParticipant = second.AddChild<TimerOwnerEntity>();
        var order = new List<string>();
        first.Timer.ScheduleOnceAt(FIRST_LOGIC_TICK_TIME, () => order.Add("firstTimer"));
        second.Timer.ScheduleOnceAt(FIRST_LOGIC_TICK_TIME, () => order.Add("secondTimer"));
        first.LogicUpdate.Register(firstParticipant, "logic", TEST_LOGIC_PRIORITY, _ => order.Add("firstLogic"));
        second.LogicUpdate.Register(secondParticipant, "logic", TEST_LOGIC_PRIORITY, _ => order.Add("secondLogic"));

        setServerTime(FIRST_LOGIC_TICK_TIME);
        _root.EarlyUpdate();
        _root.Update();

        Assert.That(order, Is.EqualTo(new[] { "firstTimer", "secondTimer", "firstLogic", "secondLogic" }));
    }

    /// <summary>Checks that callback failures consume the tick and delayed ticks do not catch up.</summary>
    [Test]
    public void FailedLogicTickIsConsumedAndDelayedTicksDoNotCatchUp()
    {
        setServerTime(FIRST_TEST_TIME);
        GameplayWorld world = GameplayWorldFactory.Create(
            _root.Scene,
            SECOND_WORLD_ID,
            SECOND_WORLD_INSTANCE_ID,
            nameof(SECOND_WORLD_ID));
        TimerOwnerEntity participant = world.AddChild<TimerOwnerEntity>();
        var order = new List<string>();
        bool fail = true;
        world.LogicUpdate.Register(participant, "first", TEST_LOGIC_PRIORITY, _ =>
        {
            order.Add("first");
            if (fail)
            {
                fail = false;
                throw new InvalidOperationException("expected logic failure");
            }
        });
        world.LogicUpdate.Register(participant, "second", TEST_LOGIC_PRIORITY, _ => order.Add("second"));

        setServerTime(DELAYED_LOGIC_TICK_TIME);
        _root.EarlyUpdate();
        Exception exception = Assert.Throws<Exception>(() => _root.Update());
        Assert.That(exception.InnerException, Is.TypeOf<GameplayWorldLogicUpdateException>());
        Assert.That(exception.InnerException!.Message, Does.Contain("first"));
        Assert.That(exception.InnerException.InnerException, Is.TypeOf<InvalidOperationException>());
        _root.Update();
        Assert.That(order, Is.EqualTo(new[] { "first" }));

        setServerTime(FOLLOWING_LOGIC_TICK_TIME);
        _root.Update();
        Assert.That(order, Is.EqualTo(new[] { "first", "first", "second" }));
    }

    /// <summary>Checks that foreign participants are rejected and stale references are skipped.</summary>
    [Test]
    public void LogicRegistrationRejectsForeignParticipantsAndSkipsStaleReferences()
    {
        setServerTime(FIRST_TEST_TIME);
        GameplayWorld world = GameplayWorldFactory.Create(
            _root.Scene,
            SECOND_WORLD_ID,
            SECOND_WORLD_INSTANCE_ID,
            nameof(SECOND_WORLD_ID));
        TimerOwnerEntity foreign = _root.Scene.AddChild<TimerOwnerEntity>();
        TimerOwnerEntity stale = world.AddChild<TimerOwnerEntity>();
        int callbackCount = 0;
        Assert.Throws<InvalidOperationException>(() =>
            world.LogicUpdate.Register(foreign, "foreign", TEST_LOGIC_PRIORITY, _ => callbackCount++));
        world.LogicUpdate.Register(stale, "stale", TEST_LOGIC_PRIORITY, _ => callbackCount++);
        stale.ChangeInstanceId(REUSED_INSTANCE_ID);

        setServerTime(FIRST_LOGIC_TICK_TIME);
        _root.EarlyUpdate();
        _root.Update();

        Assert.That(callbackCount, Is.Zero);
    }

    /// <summary>Checks that destroying a World from its timer clears pending ticks and waits.</summary>
    [Test]
    public async Task WorldDestroyedByTimerClearsPendingTickAndWaiters()
    {
        setServerTime(FIRST_TEST_TIME);
        GameplayWorld world = GameplayWorldFactory.Create(
            _root.Scene,
            SECOND_WORLD_ID,
            SECOND_WORLD_INSTANCE_ID,
            nameof(SECOND_WORLD_ID));
        TimerOwnerEntity participant = world.AddChild<TimerOwnerEntity>();
        int callbackCount = 0;
        world.LogicUpdate.Register(participant, "logic", TEST_LOGIC_PRIORITY, _ => callbackCount++);
        ETTask<bool> waiter = world.Timer.WaitUntilAsync(OWNER_TIMER_FUTURE_DEADLINE);
        world.Timer.ScheduleOnceAt(FIRST_LOGIC_TICK_TIME, world.Dispose);

        setServerTime(FIRST_LOGIC_TICK_TIME);
        _root.EarlyUpdate();
        _root.Update();

        Assert.That(callbackCount, Is.Zero);
        Assert.That(await waiter, Is.False);
    }

    /// <summary>Checks that disposed participants and entries after World destruction are skipped.</summary>
    [Test]
    public void LogicDispatchSkipsDisposedParticipantsAndStopsWhenWorldIsDestroyed()
    {
        setServerTime(FIRST_TEST_TIME);
        GameplayWorld world = GameplayWorldFactory.Create(
            _root.Scene,
            SECOND_WORLD_ID,
            SECOND_WORLD_INSTANCE_ID,
            nameof(SECOND_WORLD_ID));
        TimerOwnerEntity participant = world.AddChild<TimerOwnerEntity>();
        TimerOwnerEntity disposedParticipant = world.AddChild<TimerOwnerEntity>();
        var order = new List<string>();
        world.LogicUpdate.Register(disposedParticipant, "disposed", TEST_LOGIC_PRIORITY, _ => order.Add("disposed"));
        world.LogicUpdate.Register(participant, "destroy", TEST_LOGIC_PRIORITY, _ =>
        {
            order.Add("destroy");
            world.Dispose();
        });
        world.LogicUpdate.Register(participant, "remaining", TEST_LOGIC_PRIORITY, _ => order.Add("remaining"));
        disposedParticipant.Dispose();

        setServerTime(FIRST_LOGIC_TICK_TIME);
        _root.EarlyUpdate();
        _root.Update();

        Assert.That(order, Is.EqualTo(new[] { "destroy" }));
    }

    /// <summary>Checks next-tick visibility and cancellation for registrations changed during dispatch.</summary>
    [Test]
    public void LogicRegistrationChangesDuringDispatchApplyOnNextTick()
    {
        setServerTime(FIRST_TEST_TIME);
        GameplayWorld world = GameplayWorldFactory.Create(
            _root.Scene,
            SECOND_WORLD_ID,
            SECOND_WORLD_INSTANCE_ID,
            nameof(SECOND_WORLD_ID));
        TimerOwnerEntity participant = world.AddChild<TimerOwnerEntity>();
        var order = new List<string>();
        bool changed = false;
        world.LogicUpdate.Register(participant, "first", TEST_LOGIC_PRIORITY, _ =>
        {
            order.Add("first");
            if (!changed)
            {
                changed = true;
                world.LogicUpdate.Unregister(participant, "second");
                world.LogicUpdate.Register(participant, "second", TEST_LOGIC_PRIORITY, _ => order.Add("replacement"));
                world.LogicUpdate.Register(participant, "third", TEST_LOGIC_PRIORITY, _ => order.Add("third"));
            }
        });
        world.LogicUpdate.Register(participant, "second", TEST_LOGIC_PRIORITY, _ => order.Add("original"));

        setServerTime(FIRST_LOGIC_TICK_TIME);
        _root.EarlyUpdate();
        _root.Update();
        Assert.That(order, Is.EqualTo(new[] { "first" }));

        setServerTime(SECOND_LOGIC_TICK_TIME);
        _root.EarlyUpdate();
        _root.Update();
        Assert.That(order, Is.EqualTo(new[] { "first", "first", "replacement", "third" }));
    }

    /// <summary>Checks that pooled entries use the replacement owner, callback and priority.</summary>
    [Test]
    public void ReusedLogicEntryDoesNotRetainThePreviousRegistration()
    {
        setServerTime(FIRST_TEST_TIME);
        GameplayWorld world = GameplayWorldFactory.Create(
            _root.Scene, SECOND_WORLD_ID, SECOND_WORLD_INSTANCE_ID, nameof(SECOND_WORLD_ID));
        TimerOwnerEntity oldOwner = world.AddChild<TimerOwnerEntity>();
        TimerOwnerEntity newOwner = world.AddChild<TimerOwnerEntity>();
        var order = new List<string>();
        world.LogicUpdate.Register(oldOwner, "old", TEST_LOGIC_PRIORITY, _ => order.Add("old"));
        Assert.That(world.LogicUpdate.Unregister(oldOwner, "old"), Is.True);
        world.LogicUpdate.Register(newOwner, "new", LATER_LOGIC_PRIORITY, _ => order.Add("new"));
        world.LogicUpdate.Register(newOwner, "earlier", TEST_LOGIC_PRIORITY, _ => order.Add("earlier"));
        oldOwner.Dispose();

        setServerTime(FIRST_LOGIC_TICK_TIME);
        _root.EarlyUpdate();
        _root.Update();

        Assert.That(order, Is.EqualTo(new[] { "earlier", "new" }));
    }

    /// <summary>Checks deferred recycling after a dispatch aborts with a callback exception.</summary>
    [Test]
    public void RemovedEntriesRemainSkippedWhenDispatchFailsAndAreReusableOnTheNextTick()
    {
        setServerTime(FIRST_TEST_TIME);
        GameplayWorld world = GameplayWorldFactory.Create(
            _root.Scene, SECOND_WORLD_ID, SECOND_WORLD_INSTANCE_ID, nameof(SECOND_WORLD_ID));
        TimerOwnerEntity owner = world.AddChild<TimerOwnerEntity>();
        var order = new List<string>();
        bool firstTick = true;
        world.LogicUpdate.Register(owner, "first", TEST_LOGIC_PRIORITY, _ =>
        {
            order.Add("first");
            if (firstTick)
            {
                firstTick = false;
                world.LogicUpdate.Unregister(owner, "second");
                world.LogicUpdate.Register(owner, "second", TEST_LOGIC_PRIORITY, _ => order.Add("replacement"));
                throw new InvalidOperationException("expected failure");
            }
        });
        world.LogicUpdate.Register(owner, "second", TEST_LOGIC_PRIORITY, _ => order.Add("old"));
        setServerTime(FIRST_LOGIC_TICK_TIME);
        _root.EarlyUpdate();
        Assert.Throws<Exception>(() => _root.Update());
        Assert.That(order, Is.EqualTo(new[] { "first" }));

        world.LogicUpdate.Register(owner, "third", TEST_LOGIC_PRIORITY, _ => order.Add("third"));
        setServerTime(SECOND_LOGIC_TICK_TIME);
        _root.EarlyUpdate();
        _root.Update();

        Assert.That(order, Is.EqualTo(new[] { "first", "first", "replacement", "third" }));
    }

    /// <summary>Checks named entry uniqueness and priority/registration ordering.</summary>
    [Test]
    public void LogicEntriesRunByPriorityAndRegistrationOrderAndRejectDuplicates()
    {
        setServerTime(FIRST_TEST_TIME);
        GameplayWorld world = GameplayWorldFactory.Create(
            _root.Scene,
            SECOND_WORLD_ID,
            SECOND_WORLD_INSTANCE_ID,
            nameof(SECOND_WORLD_ID));
        TimerOwnerEntity participant = world.AddChild<TimerOwnerEntity>();
        var order = new List<string>();
        world.LogicUpdate.Register(participant, "later", LATER_LOGIC_PRIORITY, _ => order.Add("later"));
        world.LogicUpdate.Register(participant, "first", TEST_LOGIC_PRIORITY, _ => order.Add("first"));
        world.LogicUpdate.Register(participant, "second", TEST_LOGIC_PRIORITY, _ => order.Add("second"));

        Assert.Throws<InvalidOperationException>(() =>
            world.LogicUpdate.Register(participant, "first", TEST_LOGIC_PRIORITY, _ => { }));

        setServerTime(FIRST_LOGIC_TICK_TIME);
        _root.EarlyUpdate();
        _root.Update();

        Assert.That(order, Is.EqualTo(new[] { "first", "second", "later" }));
    }

    /// <summary>Checks fixed delta and independent Root-driven ticks without an enabled LevelScene.</summary>
    [Test]
    public void LogicTickRunsAtFixedStepThroughRootWithoutAnEnabledLevelScene()
    {
        setServerTime(FIRST_TEST_TIME);
        GameplayWorld world = GameplayWorldFactory.Create(
            _root.Scene,
            SECOND_WORLD_ID,
            SECOND_WORLD_INSTANCE_ID,
            nameof(SECOND_WORLD_ID));
        TimerOwnerEntity participant = world.AddChild<TimerOwnerEntity>();
        var deltas = new List<float>();
        world.LogicUpdate.Register(participant, "test", TEST_LOGIC_PRIORITY, deltas.Add);

        _root.EarlyUpdate();
        _root.Update();
        Assert.That(deltas, Is.Empty);

        setServerTime(FIRST_LOGIC_TICK_TIME);
        world.Update();
        Assert.That(deltas, Is.Empty);
        _root.EarlyUpdate();
        _root.Update();

        Assert.That(deltas, Is.EqualTo(new[] { EXPECTED_LOGIC_DELTA_SECONDS }));
    }

    /// <summary>Checks that a World timer is driven even when no LevelScene is enabled.</summary>
    [Test]
    public void WorldTimerRunsThroughRootWithoutAnEnabledLevelScene()
    {
        GameplayWorld world = GameplayWorldFactory.Create(
            _root.Scene,
            SECOND_WORLD_ID,
            SECOND_WORLD_INSTANCE_ID,
            nameof(SECOND_WORLD_ID));
        int callbackCount = 0;
        world.Timer.ScheduleOnce(IMMEDIATE_DELAY_MILLISECONDS, () => callbackCount++);

        _root.EarlyUpdate();
        _root.Update();

        Assert.That(callbackCount, Is.EqualTo(EXPECTED_CALLBACK_COUNT));
    }

    [Test]
    public void DueTimerRunsWhenTheGameplayWorldIsUpdated()
    {
        int callbackCount = 0;
        _root.EarlyUpdate();
        _gameplayWorld.Timer.ScheduleOnce(IMMEDIATE_DELAY_MILLISECONDS, () => callbackCount++);

        _root.Update();

        Assert.That(callbackCount, Is.EqualTo(EXPECTED_CALLBACK_COUNT));
    }

    [Test]
    public void DueTimersRunByTimestampThenRegistrationOrder()
    {
        var executionOrder = new List<string>();
        _root.EarlyUpdate();
        setServerTime(FIRST_TEST_TIME);

        _gameplayWorld.Timer.ScheduleOnceAt(
            SAME_DUE_TIME,
            () => executionOrder.Add("first"));
        _gameplayWorld.Timer.ScheduleOnceAt(
            EARLIER_DUE_TIME,
            () => executionOrder.Add("earlier"));
        _gameplayWorld.Timer.ScheduleOnceAt(
            SAME_DUE_TIME,
            () => executionOrder.Add("second"));

        setServerTime(SAME_DUE_TIME);
        _root.Update();

        Assert.That(executionOrder, Is.EqualTo(new[] { "earlier", "first", "second" }));
    }

    [Test]
    public void TimersCanBeCancelledAndInvalidIntervalsAreRejected()
    {
        int callbackCount = 0;
        _root.EarlyUpdate();
        setServerTime(FIRST_TEST_TIME);
        long timerId = _gameplayWorld.Timer.ScheduleOnceAt(
            OWNER_TIMER_FUTURE_DEADLINE,
            () => callbackCount++);

        Assert.That(_gameplayWorld.Timer.Cancel(timerId), Is.True);
        Assert.That(_gameplayWorld.Timer.Cancel(timerId), Is.False);
        Assert.Throws<ArgumentOutOfRangeException>(
            () => _gameplayWorld.Timer.ScheduleOnce(INVALID_DELAY_MILLISECONDS, () => { }));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => _gameplayWorld.Timer.ScheduleRepeated(IMMEDIATE_DELAY_MILLISECONDS, () => { }));

        setServerTime(OWNER_TIMER_FUTURE_DEADLINE);
        _root.Update();

        Assert.That(callbackCount, Is.EqualTo(0));
    }

    [Test]
    public void TimersAddedDuringCallbacksWaitUntilNextUpdateAndCancelledCallbacksAreSkipped()
    {
        int callbackCount = 0;
        _root.EarlyUpdate();
        setServerTime(FIRST_TEST_TIME);

        long cancelledTimerId = INVALID_TIMER_ID;
        _gameplayWorld.Timer.ScheduleOnce(IMMEDIATE_DELAY_MILLISECONDS, () =>
        {
            callbackCount += FIRST_CALLBACK_INCREMENT;
            _gameplayWorld.Timer.ScheduleOnce(
                IMMEDIATE_DELAY_MILLISECONDS,
                () => callbackCount += DEFERRED_CALLBACK_INCREMENT);
            _gameplayWorld.Timer.Cancel(cancelledTimerId);
        });
        cancelledTimerId = _gameplayWorld.Timer.ScheduleOnce(
            IMMEDIATE_DELAY_MILLISECONDS,
            () => callbackCount += CANCELLED_CALLBACK_INCREMENT);

        _root.Update();

        Assert.That(callbackCount, Is.EqualTo(FIRST_CALLBACK_INCREMENT));

        _root.Update();

        Assert.That(callbackCount, Is.EqualTo(FIRST_CALLBACK_INCREMENT + DEFERRED_CALLBACK_INCREMENT));
    }

    [Test]
    public void DelayedRepeatedTimerFiresOnceAndReschedulesFromCurrentTime()
    {
        int callbackCount = 0;
        _root.EarlyUpdate();
        setServerTime(FIRST_TEST_TIME);

        _gameplayWorld.Timer.ScheduleRepeated(
            REPEATED_INTERVAL_MILLISECONDS,
            () => callbackCount++);

        setServerTime(DELAYED_SERVER_TIME);
        _root.Update();

        Assert.That(callbackCount, Is.EqualTo(EXPECTED_CALLBACK_COUNT));

        _root.Update();
        Assert.That(callbackCount, Is.EqualTo(EXPECTED_CALLBACK_COUNT));

        setServerTime(NEXT_REPEATED_DUE_TIME);
        _root.Update();

        Assert.That(callbackCount, Is.EqualTo(EXPECTED_CALLBACK_COUNT + EXPECTED_CALLBACK_COUNT));
    }

    [Test]
    public void RepeatedTimerKeepsOriginalRegistrationOrderWhenRescheduledIntoExistingBucket()
    {
        var executionOrder = new List<string>();
        _root.EarlyUpdate();
        setServerTime(FIRST_TEST_TIME);

        _gameplayWorld.Timer.ScheduleRepeated(
            REPEATED_INTERVAL_MILLISECONDS,
            () => executionOrder.Add("repeated"));
        _gameplayWorld.Timer.ScheduleOnceAt(SAME_DUE_TIME, () => executionOrder.Add("once"));

        setServerTime(EARLIER_DUE_TIME);
        _root.Update();
        setServerTime(SAME_DUE_TIME);
        _root.Update();

        Assert.That(executionOrder, Is.EqualTo(new[] { "repeated", "repeated", "once" }));
    }

    [Test]
    public void CallbackFailureLeavesOtherDueTimersAvailableForNextUpdate()
    {
        var executionOrder = new List<string>();
        _root.EarlyUpdate();
        setServerTime(FIRST_TEST_TIME);

        _gameplayWorld.Timer.ScheduleOnce(IMMEDIATE_DELAY_MILLISECONDS, () =>
        {
            executionOrder.Add("failed");
            throw new InvalidOperationException("expected test failure");
        });
        _gameplayWorld.Timer.ScheduleOnce(IMMEDIATE_DELAY_MILLISECONDS, () => executionOrder.Add("remaining"));

        Assert.Throws<Exception>(() => _root.Update());
        Assert.That(executionOrder, Is.EqualTo(new[] { "failed" }));

        _root.Update();
        Assert.That(executionOrder, Is.EqualTo(new[] { "failed", "remaining" }));
    }

    [Test]
    public void DisposingWorldDuringCallbackCancelsRemainingTimersInSnapshot()
    {
        var executionOrder = new List<string>();
        _root.EarlyUpdate();
        setServerTime(FIRST_TEST_TIME);

        _gameplayWorld.Timer.ScheduleOnce(IMMEDIATE_DELAY_MILLISECONDS, () =>
        {
            executionOrder.Add("dispose");
            _gameplayWorld.Dispose();
        });
        _gameplayWorld.Timer.ScheduleOnce(IMMEDIATE_DELAY_MILLISECONDS, () => executionOrder.Add("remaining"));

        _root.Update();

        Assert.That(executionOrder, Is.EqualTo(new[] { "dispose" }));
    }

    [Test]
    public async Task WaitsCompleteWhenTheirDueTimeOrNextFrameIsProcessed()
    {
        _root.EarlyUpdate();
        setServerTime(FIRST_TEST_TIME);

        ETTask<bool> durationWait = _gameplayWorld.Timer.WaitDurationAsync(WAIT_DURATION_MILLISECONDS);
        ETTask<bool> timestampWait = _gameplayWorld.Timer.WaitUntilAsync(WAIT_DEADLINE);
        ETTask<bool> nextFrameWait = _gameplayWorld.Timer.WaitNextFrameAsync();

        setServerTime(WAIT_DEADLINE);
        _root.Update();

        Assert.That(await durationWait, Is.True);
        Assert.That(await timestampWait, Is.True);
        Assert.That(await nextFrameWait, Is.True);
    }

    [Test]
    public async Task CancellationAndWorldDestructionCompleteWaitsWithFalse()
    {
        _root.EarlyUpdate();
        setServerTime(FIRST_TEST_TIME);
        var cancellationToken = new ETCancellationToken();
        int callbackCount = 0;

        ETTask<bool> cancelledWait = _gameplayWorld.Timer.WaitDurationAsync(
            WAIT_DURATION_MILLISECONDS,
            cancellationToken);
        cancellationToken.Cancel();

        Assert.That(await cancelledWait, Is.False);
        Assert.That(
            await _gameplayWorld.Timer.WaitDurationAsync(IMMEDIATE_DELAY_MILLISECONDS, cancellationToken),
            Is.False);

        ETTask<bool> destroyedWorldWait = _gameplayWorld.Timer.WaitNextFrameAsync();
        _gameplayWorld.Timer.ScheduleOnce(IMMEDIATE_DELAY_MILLISECONDS, () => callbackCount++);
        _gameplayWorld.Dispose();

        Assert.That(await destroyedWorldWait, Is.False);

        _root.Update();

        Assert.That(callbackCount, Is.EqualTo(0));
    }

    [Test]
    public async Task OwnerMustBelongToWorldAndInvalidOwnersAreSkipped()
    {
        var foreignOwner = _root.Scene.AddChild<TimerOwnerEntity>();
        Assert.Throws<InvalidOperationException>(
            () => _gameplayWorld.Timer.ScheduleOnce(IMMEDIATE_DELAY_MILLISECONDS, () => { }, foreignOwner));

        var owner = _gameplayWorld.AddChild<TimerOwnerEntity>();
        int callbackCount = 0;
        _root.EarlyUpdate();
        setServerTime(FIRST_TEST_TIME);
        long timerId = _gameplayWorld.Timer.ScheduleOnceAt(
            OWNER_TIMER_FUTURE_DEADLINE,
            () => callbackCount++,
            owner);
        ETTask<bool> ownerWait = _gameplayWorld.Timer.WaitNextFrameAsync(owner: owner);
        long repeatedTimerId = _gameplayWorld.Timer.ScheduleRepeated(
            REPEATED_INTERVAL_MILLISECONDS,
            () => callbackCount++,
            owner);

        owner.Dispose();
        setServerTime(WAIT_DEADLINE);
        _root.Update();

        Assert.That(callbackCount, Is.EqualTo(0));
        Assert.That(_gameplayWorld.Timer.Cancel(timerId), Is.False);
        Assert.That(_gameplayWorld.Timer.Cancel(repeatedTimerId), Is.False);
        Assert.That(await ownerWait, Is.False);
        Assert.That(_logger.Warnings, Has.Count.EqualTo(OWNER_INVALID_WARNING_COUNT));
    }

    [Test]
    public void CallbackFailuresPropagateAndRepeatedTimersRemainScheduled()
    {
        int callbackCount = 0;
        _root.EarlyUpdate();
        setServerTime(FIRST_TEST_TIME);

        _gameplayWorld.Timer.ScheduleRepeated(REPEATED_INTERVAL_MILLISECONDS, () =>
        {
            callbackCount++;
            if (callbackCount == EXPECTED_CALLBACK_COUNT)
            {
                throw new InvalidOperationException("expected test failure");
            }
        });

        setServerTime(FIRST_TEST_TIME + REPEATED_INTERVAL_MILLISECONDS);
        Exception exception = Assert.Throws<Exception>(() => _root.Update());

        Assert.That(exception.InnerException, Is.TypeOf<GameplayWorldTimerException>());
        Assert.That(exception.InnerException.InnerException, Is.TypeOf<InvalidOperationException>());
        Assert.That(callbackCount, Is.EqualTo(EXPECTED_CALLBACK_COUNT));

        setServerTime(NEXT_REPEATED_DUE_TIME);
        _root.Update();

        Assert.That(callbackCount, Is.EqualTo(SECOND_INVOCATION_COUNT));
    }

    private static void setServerTime(long serverTime)
    {
        TimeInfo.Instance.ServerMinusClientTime = serverTime - TimeInfo.Instance.FrameTime;
    }

    /// <summary>Completes LevelScene preparation for timer integration tests.</summary>
    [Event(ELogicSceneType.Level)]
    public sealed class TimerLevelInitializationHandler : AEvent<LogicScene, Event_AfterCreateLevelScene>
    {
        protected override async ETTask Run(LogicScene levelScene, Event_AfterCreateLevelScene eventInfo)
        {
            await ETTask.CompletedTask;
        }
    }

    /// <summary>Completes LevelScene enabling for timer integration tests.</summary>
    [Event(ELogicSceneType.Level)]
    public sealed class TimerLevelEnableHandler : AEvent<LogicScene, Event_EnableLevelScene>
    {
        protected override async ETTask Run(LogicScene levelScene, Event_EnableLevelScene eventInfo)
        {
            await ETTask.CompletedTask;
        }
    }

    public class TimerOwnerEntity : Entity
    {
        /// <summary>Changes the instance identifier to simulate a stale pooled reference.</summary>
        /// <param name="instanceId">Replacement identifier for the test.</param>
        public void ChangeInstanceId(long instanceId)
        {
            InstanceId = instanceId;
        }
    }

    private sealed class RecordingLogger : ILogger
    {
        internal List<string> Warnings { get; } = new List<string>();

        public void Trace(string message)
        {
        }

        public void Warning(string message)
        {
            this.Warnings.Add(message);
        }

        public void Info(string message)
        {
        }

        public void Debug(string message)
        {
        }

        public void Error(string message)
        {
        }

        public void Error(Exception exception)
        {
        }

        public void Trace(ref DefaultInterpolatedStringHandler message)
        {
        }

        public void Warning(ref DefaultInterpolatedStringHandler message)
        {
        }

        public void Info(ref DefaultInterpolatedStringHandler message)
        {
        }

        public void Debug(ref DefaultInterpolatedStringHandler message)
        {
        }

        public void Error(ref DefaultInterpolatedStringHandler message)
        {
        }
    }
}
