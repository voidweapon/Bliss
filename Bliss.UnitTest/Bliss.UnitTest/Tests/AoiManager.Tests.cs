using Unity.Mathematics;

namespace Bliss.Tests;

public class AoiManager_Tests
{
    [Test]
    public void RegisterReturnsIdentifierAndLifecycleUsesIdentifier()
    {
        var aoiManager = new AoiManagerComponent();

        long observerIdentifier = aoiManager.Register(new AoiRegisterInfo(AOITestEntity.Create(1001), 1, float3.zero, EAoiShapeType.Circle, 5f, 5f, float3.zero, float3.zero));

        long targetIdentifier = aoiManager.Register(new AoiRegisterInfo(AOITestEntity.Create(2001), 2, new float3(1f, 0f, 1f), EAoiShapeType.Circle, 1f, 1f, float3.zero, float3.zero));

        Assert.AreNotEqual(observerIdentifier, targetIdentifier);
        CollectionAssert.Contains(aoiManager.QueryByTag(1), observerIdentifier);
        CollectionAssert.Contains(aoiManager.QueryByTag(2), targetIdentifier);
        List<long> results = new List<long>();
        aoiManager.GetVisibleTargets(observerIdentifier, results); CollectionAssert.Contains(results, targetIdentifier);
        aoiManager.GetVisibleObservers(targetIdentifier, results); CollectionAssert.Contains(results, observerIdentifier);

        aoiManager.UpdatePosition(targetIdentifier, new float3(100f, 0f, 100f));
        aoiManager.GetVisibleTargets(observerIdentifier, results); CollectionAssert.DoesNotContain(results, targetIdentifier);

        aoiManager.Unregister(targetIdentifier);
        CollectionAssert.IsEmpty(aoiManager.QueryByTag(2));
    }

    [Test]
    public void VisibilityQuerySupportsTagFilter()
    {
        var aoiManager = new AoiManagerComponent();

        long observerIdentifier = aoiManager.Register(new AoiRegisterInfo(AOITestEntity.Create(3001), 9, float3.zero, EAoiShapeType.Circle, 10f, 10f, float3.zero, float3.zero));
        long targetTag10Identifier = aoiManager.Register(new AoiRegisterInfo(AOITestEntity.Create(3002), 10, new float3(1f, 0f, 1f), EAoiShapeType.Circle, 1f, 1f, float3.zero, float3.zero));
        long targetTag20Identifier = aoiManager.Register(new AoiRegisterInfo(AOITestEntity.Create(3003), 20, new float3(2f, 0f, 2f), EAoiShapeType.Circle, 1f, 1f, float3.zero, float3.zero));

        List<long> results = new List<long>();
        aoiManager.GetVisibleTargets(observerIdentifier, results);
        CollectionAssert.AreEquivalent(
            new[] { targetTag10Identifier, targetTag20Identifier },
            results);
        
        aoiManager.GetVisibleTargets(observerIdentifier, results, 20);
        CollectionAssert.AreEquivalent(
            new[] { targetTag20Identifier },
            results);
        
        aoiManager.GetVisibleObservers(targetTag10Identifier, results, 9);
        CollectionAssert.AreEquivalent(
            new[] { observerIdentifier },
            results);
    }

    [Test]
    public void AabbShapeChecksBoundsCorrectly()
    {
        var aoiManager = new AoiManagerComponent();

        long observerIdentifier = aoiManager.Register(new AoiRegisterInfo(AOITestEntity.Create(4001), 1, float3.zero, EAoiShapeType.Aabb, 0f, 0f, new float3(2f, 2f, 2f), new float3(2f, 2f, 2f)));
        long insideIdentifier = aoiManager.Register(new AoiRegisterInfo(AOITestEntity.Create(4002), 2, new float3(1f, 1f, 1f), EAoiShapeType.Circle, 1f, 1f, float3.zero, float3.zero));
        long outsideIdentifier = aoiManager.Register(new AoiRegisterInfo(AOITestEntity.Create(4003), 2, new float3(1f, 3f, 1f), EAoiShapeType.Circle, 1f, 1f, float3.zero, float3.zero));
        
        List<long> results = new List<long>();
        aoiManager.GetVisibleTargets(observerIdentifier, results);
        CollectionAssert.Contains(results, insideIdentifier);
        CollectionAssert.DoesNotContain(results, outsideIdentifier);
    }

    [Test]
    public void CallbacksEmitEnterAndLeaveWithIdentifiers()
    {
        var aoiManager = new AoiManagerComponent();
        int enterCount = 0;
        int leaveCount = 0;
        long enterObserver = 0;
        long enterTarget = 0;
        int enterTag = 0;
        long leaveObserver = 0;
        long leaveTarget = 0;
        int leaveTag = 0;

        aoiManager.SetVisibilityCallbacks(
            (observerIdentifier, targetIdentifier, targetTag) =>
            {
                enterCount++;
                enterObserver = observerIdentifier;
                enterTarget = targetIdentifier;
                enterTag = targetTag;
            },
            (observerIdentifier, targetIdentifier, targetTag) =>
            {
                leaveCount++;
                leaveObserver = observerIdentifier;
                leaveTarget = targetIdentifier;
                leaveTag = targetTag;
            });

        long observerIdentifier = aoiManager.Register(new AoiRegisterInfo(AOITestEntity.Create(5001), 1, float3.zero, EAoiShapeType.Circle, 5f, 5f, float3.zero, float3.zero));
        long targetIdentifier = aoiManager.Register(new AoiRegisterInfo(AOITestEntity.Create(5002), 2, new float3(1f, 0f, 0f), EAoiShapeType.Aabb, 0f, 0f, new float3(0.1f, 0.1f, 0.1f), new float3(0.1f, 0.1f, 0.1f)));

        Assert.AreEqual(1, enterCount);
        Assert.AreEqual(observerIdentifier, enterObserver);
        Assert.AreEqual(targetIdentifier, enterTarget);
        Assert.AreEqual(2, enterTag);

        aoiManager.UpdatePosition(targetIdentifier, new float3(50f, 0f, 50f));

        Assert.AreEqual(1, leaveCount);
        Assert.AreEqual(observerIdentifier, leaveObserver);
        Assert.AreEqual(targetIdentifier, leaveTarget);
        Assert.AreEqual(2, leaveTag);
    }

    [Test]
    public void FailedRegistrationRemovesIndexesAndRelationsEvenWhenCleanupCallbackFails()
    {
        var aoiManager = new AoiManagerComponent();
        long observerIdentifier = aoiManager.Register(new AoiRegisterInfo(
            AOITestEntity.Create(9001),
            1,
            float3.zero,
            EAoiShapeType.Circle,
            5f,
            5f,
            float3.zero,
            float3.zero));

        aoiManager.SetVisibilityCallbacks(
            (_, _, _) => throw new InvalidOperationException("enter callback failed"),
            (_, _, _) => throw new InvalidOperationException("leave callback failed"));

        AggregateException exception = Assert.Throws<AggregateException>(() =>
            aoiManager.Register(new AoiRegisterInfo(
                AOITestEntity.Create(9002),
                2,
                new float3(1f, 0f, 1f),
                EAoiShapeType.Circle,
                1f,
                1f,
                float3.zero,
                float3.zero)));

        long failedIdentifier = aoiManager.IdentifierGenerator;
        List<long> results = new List<long>();
        aoiManager.GetVisibleTargets(observerIdentifier, results);

        Assert.That(exception.InnerExceptions, Has.Count.EqualTo(2));
        Assert.That(aoiManager.Entries, Has.Count.EqualTo(1));
        Assert.That(aoiManager.QueryByTag(2), Is.Empty);
        Assert.That(results, Is.Empty);
        Assert.That(aoiManager.VisibleObserversByTarget.ContainsKey(failedIdentifier), Is.False);
    }

    /// <summary>Verifies callback failures do not prevent AOI unregister cleanup.</summary>
    [Test]
    public void FailedLeaveCallbackDoesNotPreventUnregisterCleanup()
    {
        var aoiManager = new AoiManagerComponent();
        long observerIdentifier = aoiManager.Register(new AoiRegisterInfo(
            AOITestEntity.Create(9101),
            1,
            float3.zero,
            EAoiShapeType.Circle,
            5f,
            5f,
            float3.zero,
            float3.zero));
        long targetIdentifier = aoiManager.Register(new AoiRegisterInfo(
            AOITestEntity.Create(9102),
            2,
            new float3(1f, 0f, 1f),
            EAoiShapeType.Circle,
            1f,
            1f,
            float3.zero,
            float3.zero));
        aoiManager.SetVisibilityCallbacks(
            (_, _, _) => { },
            (_, _, _) => throw new InvalidOperationException("leave callback failed"));

        Assert.Throws<InvalidOperationException>(() => aoiManager.Unregister(targetIdentifier));

        List<long> results = new List<long>();
        aoiManager.GetVisibleTargets(observerIdentifier, results);
        Assert.That(aoiManager.Entries, Has.Count.EqualTo(1));
        Assert.That(aoiManager.QueryByTag(2), Is.Empty);
        Assert.That(results, Is.Empty);
        Assert.That(aoiManager.VisibleObserversByTarget.ContainsKey(targetIdentifier), Is.False);
    }

    [Test]
    public void CircleShapeUsesThreeDimensionalDistance()
    {
        var aoiManager = new AoiManagerComponent();

        long observerIdentifier = aoiManager.Register(new AoiRegisterInfo( AOITestEntity.Create(6001), 1, float3.zero, EAoiShapeType.Circle, 5f, 5f, float3.zero, float3.zero));
        long insideIdentifier = aoiManager.Register(new AoiRegisterInfo( AOITestEntity.Create(6002), 2, new float3(0f, 4f, 0f), EAoiShapeType.Circle, 1f, 1f, float3.zero, float3.zero));
        long outsideIdentifier = aoiManager.Register(new AoiRegisterInfo(AOITestEntity.Create(6003), 2, new float3(0f, 6f, 0f), EAoiShapeType.Circle, 1f, 1f, float3.zero, float3.zero));
        
        List<long> results = new List<long>();
        aoiManager.GetVisibleTargets(observerIdentifier, results);
        CollectionAssert.Contains(results, insideIdentifier);
        CollectionAssert.DoesNotContain(results, outsideIdentifier);
    }

    [Test]
    public void CircleShapeKeepsVisibilityWithinHysteresisBand()
    {
        var aoiManager = new AoiManagerComponent();
        int enterCount = 0;
        int leaveCount = 0;

        aoiManager.SetVisibilityCallbacks(
            (_, _, _) => enterCount++,
            (_, _, _) => leaveCount++);

        long observerIdentifier = aoiManager.Register(new AoiRegisterInfo( AOITestEntity.Create(7001), 1, float3.zero, EAoiShapeType.Circle, 5f, 6f, float3.zero, float3.zero));
        long targetIdentifier = aoiManager.Register(new AoiRegisterInfo( AOITestEntity.Create(7002), 2, new float3(4.5f, 0f, 0f), EAoiShapeType.Circle, 1f, 1f, float3.zero, float3.zero));
        List<long> results = new List<long>();

        Assert.AreEqual(1, enterCount);
        aoiManager.GetVisibleTargets(observerIdentifier, results);
        CollectionAssert.Contains(results, targetIdentifier);

        aoiManager.UpdatePosition(targetIdentifier, new float3(5.5f, 0f, 0f));
        Assert.AreEqual(1, enterCount);
        Assert.AreEqual(0, leaveCount);
        aoiManager.GetVisibleTargets(observerIdentifier, results);
        CollectionAssert.Contains(results, targetIdentifier);

        aoiManager.UpdatePosition(targetIdentifier, new float3(4.8f, 0f, 0f));
        Assert.AreEqual(1, enterCount);
        Assert.AreEqual(0, leaveCount);
        aoiManager.GetVisibleTargets(observerIdentifier, results);
        CollectionAssert.Contains(results, targetIdentifier);

        aoiManager.UpdatePosition(targetIdentifier, new float3(6.1f, 0f, 0f));
        Assert.AreEqual(1, leaveCount);
        aoiManager.GetVisibleTargets(observerIdentifier, results);
        CollectionAssert.DoesNotContain(results, targetIdentifier);
    }

    [Test]
    public void AabbShapeKeepsVisibilityWithinHysteresisBand()
    {
        var aoiManager = new AoiManagerComponent();
        int enterCount = 0;
        int leaveCount = 0;

        aoiManager.SetVisibilityCallbacks(
            (_, _, _) => enterCount++,
            (_, _, _) => leaveCount++);

        long observerIdentifier = aoiManager.Register(new AoiRegisterInfo(AOITestEntity.Create(8001), 1, float3.zero, EAoiShapeType.Aabb, 0f, 0f, new float3(2f, 2f, 2f), new float3(3f, 3f, 3f)));
        long targetIdentifier = aoiManager.Register(new AoiRegisterInfo(AOITestEntity.Create(8002), 2, new float3(1f, 1f, 1f), EAoiShapeType.Circle, 1f, 1f, float3.zero, float3.zero));
        List<long> results = new List<long>();

        Assert.AreEqual(1, enterCount);
        aoiManager.GetVisibleTargets(observerIdentifier, results);
        CollectionAssert.Contains(results, targetIdentifier);

        aoiManager.UpdatePosition(targetIdentifier, new float3(2.5f, 1f, 1f));
        Assert.AreEqual(1, enterCount);
        Assert.AreEqual(0, leaveCount);
        aoiManager.GetVisibleTargets(observerIdentifier, results);
        CollectionAssert.Contains(results, targetIdentifier);

        aoiManager.UpdatePosition(targetIdentifier, new float3(3.5f, 1f, 1f));
        Assert.AreEqual(1, leaveCount);
        aoiManager.GetVisibleTargets(observerIdentifier, results);
        CollectionAssert.DoesNotContain(results, targetIdentifier);
    }
    
    public class AOITestEntity : Entity
    {
        public static AOITestEntity Create(long entityId)
        {
            return new AOITestEntity()
            {
                Id = entityId,
                InstanceId = entityId,
            };
        }
    }
    
}
