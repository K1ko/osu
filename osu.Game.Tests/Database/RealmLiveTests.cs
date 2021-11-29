// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using osu.Game.Database;
using osu.Game.Models;
using Realms;

#nullable enable

namespace osu.Game.Tests.Database
{
    public class RealmLiveTests : RealmTest
    {
        [Test]
        public void TestLiveEquality()
        {
            RunTestWithRealm((realmFactory, _) =>
            {
                ILive<RealmBeatmap> beatmap = realmFactory.CreateContext().Write(r => r.Add(new RealmBeatmap(CreateRuleset(), new RealmBeatmapDifficulty(), new RealmBeatmapMetadata()))).ToLive();

                ILive<RealmBeatmap> beatmap2 = realmFactory.CreateContext().All<RealmBeatmap>().First().ToLive();

                Assert.AreEqual(beatmap, beatmap2);
            });
        }

        [Test]
        public void TestAccessAfterAttach()
        {
            RunTestWithRealm((realmFactory, _) =>
            {
                var beatmap = new RealmBeatmap(CreateRuleset(), new RealmBeatmapDifficulty(), new RealmBeatmapMetadata());

                var liveBeatmap = beatmap.ToLive();

                using (var context = realmFactory.CreateContext())
                    context.Write(r => r.Add(beatmap));

                Assert.IsFalse(liveBeatmap.PerformRead(l => l.Hidden));
            });
        }

        [Test]
        public void TestAccessNonManaged()
        {
            var beatmap = new RealmBeatmap(CreateRuleset(), new RealmBeatmapDifficulty(), new RealmBeatmapMetadata());
            var liveBeatmap = beatmap.ToLive();

            Assert.IsFalse(beatmap.Hidden);
            Assert.IsFalse(liveBeatmap.Value.Hidden);
            Assert.IsFalse(liveBeatmap.PerformRead(l => l.Hidden));

            Assert.Throws<InvalidOperationException>(() => liveBeatmap.PerformWrite(l => l.Hidden = true));

            Assert.IsFalse(beatmap.Hidden);
            Assert.IsFalse(liveBeatmap.Value.Hidden);
            Assert.IsFalse(liveBeatmap.PerformRead(l => l.Hidden));
        }

        [Test]
        public void TestValueAccessWithOpenContext()
        {
            RunTestWithRealm((realmFactory, _) =>
            {
                ILive<RealmBeatmap>? liveBeatmap = null;
                Task.Factory.StartNew(() =>
                {
                    using (var threadContext = realmFactory.CreateContext())
                    {
                        var beatmap = threadContext.Write(r => r.Add(new RealmBeatmap(CreateRuleset(), new RealmBeatmapDifficulty(), new RealmBeatmapMetadata())));

                        liveBeatmap = beatmap.ToLive();
                    }
                }, TaskCreationOptions.LongRunning | TaskCreationOptions.HideScheduler).Wait();

                Debug.Assert(liveBeatmap != null);

                Task.Factory.StartNew(() =>
                {
                    // TODO: The commented code is the behaviour we hope to obtain, but is temporarily disabled.
                    // See https://github.com/ppy/osu/pull/15851
                    using (realmFactory.CreateContext())
                    {
                        Assert.Throws<InvalidOperationException>(() =>
                        {
                            var __ = liveBeatmap.Value;
                        });
                    }

                    // Assert.DoesNotThrow(() =>
                    // {
                    //     using (realmFactory.CreateContext())
                    //     {
                    //         var resolved = liveBeatmap.Value;
                    //
                    //         Assert.IsTrue(resolved.Realm.IsClosed);
                    //         Assert.IsTrue(resolved.IsValid);
                    //
                    //         // can access properties without a crash.
                    //         Assert.IsFalse(resolved.Hidden);
                    //     }
                    // });
                }, TaskCreationOptions.LongRunning | TaskCreationOptions.HideScheduler).Wait();
            });
        }

        [Test]
        public void TestScopedReadWithoutContext()
        {
            RunTestWithRealm((realmFactory, _) =>
            {
                ILive<RealmBeatmap>? liveBeatmap = null;
                Task.Factory.StartNew(() =>
                {
                    using (var threadContext = realmFactory.CreateContext())
                    {
                        var beatmap = threadContext.Write(r => r.Add(new RealmBeatmap(CreateRuleset(), new RealmBeatmapDifficulty(), new RealmBeatmapMetadata())));

                        liveBeatmap = beatmap.ToLive();
                    }
                }, TaskCreationOptions.LongRunning | TaskCreationOptions.HideScheduler).Wait();

                Debug.Assert(liveBeatmap != null);

                Task.Factory.StartNew(() =>
                {
                    liveBeatmap.PerformRead(beatmap =>
                    {
                        Assert.IsTrue(beatmap.IsValid);
                        Assert.IsFalse(beatmap.Hidden);
                    });
                }, TaskCreationOptions.LongRunning | TaskCreationOptions.HideScheduler).Wait();
            });
        }

        [Test]
        public void TestScopedWriteWithoutContext()
        {
            RunTestWithRealm((realmFactory, _) =>
            {
                ILive<RealmBeatmap>? liveBeatmap = null;
                Task.Factory.StartNew(() =>
                {
                    using (var threadContext = realmFactory.CreateContext())
                    {
                        var beatmap = threadContext.Write(r => r.Add(new RealmBeatmap(CreateRuleset(), new RealmBeatmapDifficulty(), new RealmBeatmapMetadata())));

                        liveBeatmap = beatmap.ToLive();
                    }
                }, TaskCreationOptions.LongRunning | TaskCreationOptions.HideScheduler).Wait();

                Debug.Assert(liveBeatmap != null);

                Task.Factory.StartNew(() =>
                {
                    liveBeatmap.PerformWrite(beatmap => { beatmap.Hidden = true; });
                    liveBeatmap.PerformRead(beatmap => { Assert.IsTrue(beatmap.Hidden); });
                }, TaskCreationOptions.LongRunning | TaskCreationOptions.HideScheduler).Wait();
            });
        }

        [Test]
        public void TestValueAccessWithoutOpenContextFails()
        {
            RunTestWithRealm((realmFactory, _) =>
            {
                ILive<RealmBeatmap>? liveBeatmap = null;
                Task.Factory.StartNew(() =>
                {
                    using (var threadContext = realmFactory.CreateContext())
                    {
                        var beatmap = threadContext.Write(r => r.Add(new RealmBeatmap(CreateRuleset(), new RealmBeatmapDifficulty(), new RealmBeatmapMetadata())));

                        liveBeatmap = beatmap.ToLive();
                    }
                }, TaskCreationOptions.LongRunning | TaskCreationOptions.HideScheduler).Wait();

                Debug.Assert(liveBeatmap != null);

                Task.Factory.StartNew(() =>
                {
                    Assert.Throws<InvalidOperationException>(() =>
                    {
                        var unused = liveBeatmap.Value;
                    });
                }, TaskCreationOptions.LongRunning | TaskCreationOptions.HideScheduler).Wait();
            });
        }

        [Test]
        public void TestLiveAssumptions()
        {
            RunTestWithRealm((realmFactory, _) =>
            {
                int changesTriggered = 0;

                using (var updateThreadContext = realmFactory.CreateContext())
                {
                    updateThreadContext.All<RealmBeatmap>().SubscribeForNotifications(gotChange);
                    ILive<RealmBeatmap>? liveBeatmap = null;

                    Task.Factory.StartNew(() =>
                    {
                        using (var threadContext = realmFactory.CreateContext())
                        {
                            var ruleset = CreateRuleset();
                            var beatmap = threadContext.Write(r => r.Add(new RealmBeatmap(ruleset, new RealmBeatmapDifficulty(), new RealmBeatmapMetadata())));

                            // add a second beatmap to ensure that a full refresh occurs below.
                            // not just a refresh from the resolved Live.
                            threadContext.Write(r => r.Add(new RealmBeatmap(ruleset, new RealmBeatmapDifficulty(), new RealmBeatmapMetadata())));

                            liveBeatmap = beatmap.ToLive();
                        }
                    }, TaskCreationOptions.LongRunning | TaskCreationOptions.HideScheduler).Wait();

                    Debug.Assert(liveBeatmap != null);

                    // not yet seen by main context
                    Assert.AreEqual(0, updateThreadContext.All<RealmBeatmap>().Count());
                    Assert.AreEqual(0, changesTriggered);

                    // TODO: Originally the following was using `liveBeatmap.Value`. This has been temporarily disabled.
                    // See https://github.com/ppy/osu/pull/15851
                    liveBeatmap.PerformRead(resolved =>
                    {
                        // retrieval causes an implicit refresh. even changes that aren't related to the retrieval are fired at this point.
                        // ReSharper disable once AccessToDisposedClosure
                        Assert.AreEqual(2, updateThreadContext.All<RealmBeatmap>().Count());
                        Assert.AreEqual(1, changesTriggered);

                        // TODO: as above, temporarily disabled as it doesn't make sense inside a `PerformRead`.
                        // // even though the realm that this instance was resolved for was closed, it's still valid.
                        // Assert.IsTrue(resolved.Realm.IsClosed);
                        // Assert.IsTrue(resolved.IsValid);

                        // can access properties without a crash.
                        Assert.IsFalse(resolved.Hidden);

                        // ReSharper disable once AccessToDisposedClosure
                        updateThreadContext.Write(r =>
                        {
                            // can use with the main context.
                            r.Remove(resolved);
                        });
                    });
                }

                void gotChange(IRealmCollection<RealmBeatmap> sender, ChangeSet changes, Exception error)
                {
                    changesTriggered++;
                }
            });
        }
    }
}
