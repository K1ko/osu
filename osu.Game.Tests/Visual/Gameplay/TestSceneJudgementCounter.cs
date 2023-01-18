// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Diagnostics;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Testing;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Judgements;
using osu.Game.Rulesets.Mania;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Osu.Judgements;
using osu.Game.Rulesets.Scoring;
using osu.Game.Screens.Play.HUD.JudgementCounter;

namespace osu.Game.Tests.Visual.Gameplay
{
    public partial class TestSceneJudgementCounter : OsuTestScene
    {
        private ScoreProcessor scoreProcessor = null!;
        private JudgementTally judgementTally = null!;
        private TestJudgementCounterDisplay counterDisplay = null!;

        private readonly Bindable<JudgementResult> lastJudgementResult = new Bindable<JudgementResult>();

        private int iteration;

        [SetUpSteps]
        public void SetupSteps() => AddStep("Create components", () =>
        {
            var ruleset = CreateRuleset();

            Debug.Assert(ruleset != null);

            scoreProcessor = new ScoreProcessor(ruleset);
            Child = new DependencyProvidingContainer
            {
                RelativeSizeAxes = Axes.Both,
                CachedDependencies = new (Type, object)[] { (typeof(ScoreProcessor), scoreProcessor), (typeof(Ruleset), ruleset) },
                Children = new Drawable[]
                {
                    judgementTally = new JudgementTally(),
                    new DependencyProvidingContainer
                    {
                        RelativeSizeAxes = Axes.Both,
                        CachedDependencies = new (Type, object)[] { (typeof(JudgementTally), judgementTally) },
                        Child = counterDisplay = new TestJudgementCounterDisplay
                        {
                            Margin = new MarginPadding { Top = 100 },
                            Anchor = Anchor.TopCentre,
                            Origin = Anchor.TopCentre
                        }
                    }
                },
            };
        });

        protected override Ruleset CreateRuleset() => new ManiaRuleset();

        private void applyOneJudgement(HitResult result)
        {
            lastJudgementResult.Value = new OsuJudgementResult(new HitObject
            {
                StartTime = iteration * 10000
            }, new OsuJudgement())
            {
                Type = result,
            };
            scoreProcessor.ApplyResult(lastJudgementResult.Value);

            iteration++;
        }

        [Test]
        public void TestAddJudgementsToCounters()
        {
            AddRepeatStep("Add judgement", () => applyOneJudgement(HitResult.Great), 2);
            AddRepeatStep("Add judgement", () => applyOneJudgement(HitResult.Miss), 2);
            AddRepeatStep("Add judgement", () => applyOneJudgement(HitResult.Meh), 2);
        }

        [Test]
        public void TestAddWhilstHidden()
        {
            AddRepeatStep("Add judgement", () => applyOneJudgement(HitResult.LargeTickHit), 2);
            AddAssert("Check value added whilst hidden", () => hiddenCount() == 2);
            AddStep("Show all judgements", () => counterDisplay.Mode.Value = JudgementCounterDisplay.DisplayMode.All);
        }

        [Test]
        public void TestChangeFlowDirection()
        {
            AddStep("Set direction vertical", () => counterDisplay.FlowDirection.Value = Direction.Vertical);
            AddStep("Set direction horizontal", () => counterDisplay.FlowDirection.Value = Direction.Horizontal);
        }

        [Test]
        public void TestToggleJudgementNames()
        {
            AddStep("Hide judgement names", () => counterDisplay.ShowJudgementNames.Value = false);
            AddWaitStep("wait some", 2);
            AddAssert("Assert hidden", () => counterDisplay.CounterFlow.Children.First().ResultName.Alpha == 0);
            AddStep("Hide judgement names", () => counterDisplay.ShowJudgementNames.Value = true);
            AddWaitStep("wait some", 2);
            AddAssert("Assert shown", () => counterDisplay.CounterFlow.Children.First().ResultName.Alpha == 1);
        }

        [Test]
        public void TestHideMaxValue()
        {
            AddStep("Hide max judgement", () => counterDisplay.ShowMaxJudgement.Value = false);
            AddWaitStep("wait some", 2);
            AddAssert("Check max hidden", () => counterDisplay.CounterFlow.ChildrenOfType<JudgementCounter>().First().Alpha == 0);
            AddStep("Show max judgement", () => counterDisplay.ShowMaxJudgement.Value = true);
        }

        [Test]
        public void TestCycleDisplayModes()
        {
            AddStep("Show basic judgements", () => counterDisplay.Mode.Value = JudgementCounterDisplay.DisplayMode.Simple);
            AddWaitStep("wait some", 2);
            AddAssert("Check only basic", () => counterDisplay.CounterFlow.ChildrenOfType<JudgementCounter>().Last().Alpha == 0);
            AddStep("Show normal judgements", () => counterDisplay.Mode.Value = JudgementCounterDisplay.DisplayMode.Normal);
            AddStep("Show all judgements", () => counterDisplay.Mode.Value = JudgementCounterDisplay.DisplayMode.All);
            AddWaitStep("wait some", 2);
            AddAssert("Check all visible", () => counterDisplay.CounterFlow.ChildrenOfType<JudgementCounter>().Last().Alpha == 1);
        }

        private int hiddenCount()
        {
            var num = counterDisplay.CounterFlow.Children.First(child => child.Result.Type == HitResult.LargeTickHit);
            return num.Result.ResultCount.Value;
        }

        private partial class TestJudgementCounterDisplay : JudgementCounterDisplay
        {
            public new FillFlowContainer<JudgementCounter> CounterFlow => base.CounterFlow;
        }
    }
}
