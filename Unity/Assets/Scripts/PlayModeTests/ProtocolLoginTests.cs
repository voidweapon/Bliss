using System;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using Bliss;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>Exercises the client login protocol against local fake endpoints.</summary>
public class ProtocolLoginTests
{
    /// <summary>Releases owned resources even if the test coroutine is stopped.</summary>
    [TearDown]
    public void TearDown()
    {
        ProtocolLoginFixture.DisposeActiveRun();
    }

    /// <summary>A successful Realm and Gate handshake establishes the client session.</summary>
    [UnityTest]
    public IEnumerator LoginSucceedsThroughRealmAndGate()
    {
        yield return ProtocolLoginFixture.RunSuccessfulLogin();
    }

    /// <summary>A rejected Gate login does not establish a logged-in client.</summary>
    [UnityTest]
    public IEnumerator GateRejectionDoesNotLogIn()
    {
        yield return ProtocolLoginFixture.RunRejectedLogin();
    }

    /// <summary>An existing World is never taken over or disposed by the fixture.</summary>
    [Test]
    public void ExistingWorldIsPreserved()
    {
        FieldInfo field = typeof(World).GetField("instance", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null);
        Assert.That(field.GetValue(null), Is.Null, "The test requires an initially unowned World.");
        World existing = World.Instance;
        try
        {
            IEnumerator run = ProtocolLoginFixture.RunSuccessfulLogin();
            Assert.Throws<InvalidOperationException>(() => run.MoveNext());
            Assert.That(field.GetValue(null), Is.SameAs(existing));
        }
        finally
        {
            existing.Dispose();
        }
    }

    /// <summary>An occupied Realm port fails without disturbing its owner.</summary>
    [Test]
    public void OccupiedRealmPortFailsWithoutClosingOwner()
    {
        using Socket owner = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        owner.Bind(new IPEndPoint(IPAddress.Loopback, ProtocolLoginFixture.REALM_PORT));
        IEnumerator run = ProtocolLoginFixture.RunSuccessfulLogin();
        Assert.Throws<SocketException>(() => run.MoveNext());
        Assert.That(owner.IsBound, Is.True);
    }

    /// <summary>Releasing a suspended test also stops its World and fake endpoints.</summary>
    [Test]
    public void SuspendedRunCanBeCleanedUp()
    {
        VerifySuspendedRunCleanup(false);
    }

    /// <summary>Leaving PlayMode also releases a suspended test's resources.</summary>
    [Test]
    public void DestroyingCleanupObjectReleasesSuspendedRun()
    {
        VerifySuspendedRunCleanup(true);
    }

    private static void VerifySuspendedRunCleanup(bool destroyCleanupObject)
    {
        FieldInfo field = typeof(World).GetField("instance", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null);
        Assert.That(field.GetValue(null), Is.Null);
        IEnumerator run = ProtocolLoginFixture.RunSuccessfulLogin();
        try
        {
            Assert.That(run.MoveNext(), Is.True);
            Assert.That(field.GetValue(null), Is.Not.Null);
            if (destroyCleanupObject)
            {
                ProtocolLoginFixture.ProtocolFixtureCleanup cleanup =
                    UnityEngine.Object.FindFirstObjectByType<ProtocolLoginFixture.ProtocolFixtureCleanup>();
                Assert.That(cleanup, Is.Not.Null);
                UnityEngine.Object.DestroyImmediate(cleanup.gameObject);
            }
            else
            {
                ProtocolLoginFixture.DisposeActiveRun();
            }
            Assert.That(field.GetValue(null), Is.Null);
            using Socket probe = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            probe.Bind(new IPEndPoint(IPAddress.Loopback, ProtocolLoginFixture.REALM_PORT));
        }
        finally
        {
            (run as IDisposable)?.Dispose();
        }
    }
}
