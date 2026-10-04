using System.Collections.Generic;
using HallOfEchoingMirrors.Core;
using HallOfEchoingMirrors.Saving;
using NUnit.Framework;

namespace HallOfEchoingMirrors.Tests
{
    /// <summary>
    /// Carried items: kept only by walking out, lost when the anchor pulls her back; the stash and
    /// packing; items that tax every second of action; and tasks that take things away.
    /// </summary>
    public class CarriedItemsTests : SimulationTestBase
    {
        private ResourceDefinition _ring;
        private TaskDefinition _takeRing, _dropRing, _walkOut, _wait;

        // Tasks can be done anywhere (no places). Taking the ring: 1s. Waiting: 10s, free.
        [SetUp]
        public void SetUp()
        {
            _ring = MakeResource("Roland's ring", ResourceLifetime.Carried, max: 1);

            _takeRing = MakeTask("Take the ring", 1f);
            _takeRing.gives.Add(new ResourceAmount { resource = _ring, amount = 1 });

            _dropRing = MakeTask("Put the ring down", 1f);
            _dropRing.needs.Add(new ResourceAmount { resource = _ring, amount = 1 });
            _dropRing.takes.Add(new ResourceAmount { resource = _ring, amount = 1 });

            _walkOut = MakeTask("Step out through the mirror", 1f);
            _walkOut.walksOut = true;

            _wait = MakeTask("Wait", 10f);
        }

        // Carrying trains Composure, which softens the tax: off in test settings, so the sums stay exact.
        private Simulation MakeSimulation(LoopSettings settings = null) =>
            new Simulation(settings ?? MakeLoopSettings(), TicksPerSecond);

        [Test]
        public void AnAlwaysChargedItem_CostsVitality_EvenWithCarryCostsOff()
        {
            var settings = MakeLoopSettings();
            settings.chargeCarryCosts = false;
            settings.pools.Add(HuePool(Hue.Amber, 50f)); // it still hits vitality, not the pools
            _ring.carryCostPerSecond = 1f;
            _ring.alwaysCharged = true;
            var sim = MakeSimulation(settings);
            sim.Schedule(_takeRing, 1);
            sim.Schedule(_wait, 1);
            sim.BeginLoop();

            RunSeconds(sim, 6); // 1s taking it, then 5s of waiting while carrying it

            Assert.That(sim.Loop.Vitality.Current, Is.EqualTo(95f).Within(0.01f));
            Assert.That(sim.Loop.FindPool(Hue.Amber).Current, Is.EqualTo(50f));
        }

        [Test]
        public void AnOrdinaryItem_IsFree_WithCarryCostsOff_AndTrainsNothing()
        {
            var settings = MakeLoopSettings();
            settings.chargeCarryCosts = false;
            _ring.carryCostPerSecond = 1f;
            var sim = new Simulation(settings, TicksPerSecond); // Composure training left on
            sim.Schedule(_takeRing, 1);
            sim.Schedule(_wait, 1);
            sim.BeginLoop();

            RunSeconds(sim, 6);

            Assert.That(sim.Loop.Vitality.Current, Is.EqualTo(100f));
            Assert.That(sim.Loop.XpOf(ClaraAttribute.Composure), Is.EqualTo(0f), "nothing to hold steady against");
        }

        [Test]
        public void WalkingOut_KeepsWhatSheCarries_InTheStash()
        {
            var sim = MakeSimulation();
            sim.Schedule(_takeRing, 1);
            sim.Schedule(_walkOut, 1);
            sim.BeginLoop();

            RunSeconds(sim, 2);

            Assert.That(sim.Loop.EndReason, Is.EqualTo(LoopEndReason.WalkedOut));
            Assert.That(sim.StashOf(_ring), Is.EqualTo(1));
            Assert.That(sim.LastRun.CarriedKept, Is.EqualTo(new[] { (_ring, 1) }));
            Assert.That(sim.Loop.Vitality.Current, Is.EqualTo(100f), "walking out leaves vitality unspent");
        }

        [Test]
        public void BeingYankedOut_LosesWhatSheCarries()
        {
            var sim = MakeSimulation();
            sim.Schedule(_takeRing, 1);
            sim.BeginLoop();
            RunSeconds(sim, 1);

            sim.EndRunEarly(); // ending early is the anchor too

            Assert.That(sim.StashOf(_ring), Is.EqualTo(0));
            Assert.That(sim.LastRun.CarriedLost, Is.EqualTo(new[] { (_ring, 1) }));
            Assert.That(sim.LastRun.ToolsAndStats, Is.Empty, "carried items aren't listed as this-run-only tools");
        }

        [Test]
        public void APackedItem_GoesInWithHer_AndIsAtRiskThere()
        {
            var sim = MakeSimulation();
            sim.Persistent.Stash[_ring] = 1;
            sim.SetPacked(_ring, true);

            sim.BeginLoop();
            Assert.That(sim.AmountOf(_ring), Is.EqualTo(1), "in the hall with her");
            Assert.That(sim.StashOf(_ring), Is.EqualTo(0), "no longer safe in the lab");

            sim.EndRunEarly();
            Assert.That(sim.StashOf(_ring), Is.EqualTo(0), "lost with the run");
        }

        [Test]
        public void PackedItems_OnlyFillFreePockets_TheRestWaitInTheStash()
        {
            var lantern = MakeObject("Lantern", max: 5, lasts: ResourceLifetime.Carried);
            var sim = MakeSimulation(MakeLoopSettings(pockets: 2));
            sim.Persistent.Stash[lantern] = 5;
            sim.SetPacked(lantern, true);

            sim.BeginLoop();

            Assert.That(sim.AmountOf(lantern), Is.EqualTo(2));
            Assert.That(sim.StashOf(lantern), Is.EqualTo(3));
        }

        [Test]
        public void WalkingOut_EndsTheRun_BeforeAnythingElseHappensThatTick()
        {
            var settings = MakeLoopSettings();
            settings.composureXpPerSecond = 1f; // she'd learn a little every tick
            var sim = MakeSimulation(settings);
            float xpAtTheEnd = -1f;
            sim.LoopEnded += () => xpAtTheEnd = sim.Loop.XpOf(ClaraAttribute.Composure);
            sim.Schedule(_walkOut, 1);
            sim.BeginLoop();

            RunSeconds(sim, 2f);

            Assert.That(xpAtTheEnd, Is.GreaterThanOrEqualTo(0f), "she walked out");
            Assert.That(sim.Loop.XpOf(ClaraAttribute.Composure), Is.EqualTo(xpAtTheEnd), "nothing learnt after the run ended");
        }

        [Test]
        public void AnUnpackedItem_StaysSafeInTheLab()
        {
            var sim = MakeSimulation();
            sim.Persistent.Stash[_ring] = 1;

            sim.BeginLoop();
            sim.EndRunEarly();

            Assert.That(sim.AmountOf(_ring), Is.EqualTo(0));
            Assert.That(sim.StashOf(_ring), Is.EqualTo(1));
        }

        [Test]
        public void ATaxingItem_CostsVitality_ForEverySecondOfAction()
        {
            _ring.carryCostPerSecond = 0.5f;
            var sim = MakeSimulation();
            sim.Schedule(_takeRing, 1);
            sim.Schedule(_wait, 1); // free, 10s
            sim.BeginLoop();

            RunSeconds(sim, 11);

            Assert.That(sim.Loop.Vitality.Current, Is.EqualTo(100f - 0.5f * 10f).Within(0.01f));
        }

        [Test]
        public void TheTax_IsntChargedWhileSheStandsIdle()
        {
            _ring.carryCostPerSecond = 0.5f;
            var sim = MakeSimulation();
            sim.Schedule(_takeRing, 1);
            sim.BeginLoop();

            RunSeconds(sim, 20); // the queue runs out after 1s; the rest is idle

            Assert.That(sim.Loop.Vitality.Current, Is.EqualTo(100f).Within(0.01f));
        }

        [Test]
        public void OnceSheHasPools_TheTaxDrawsOnThemFirst()
        {
            _ring.carryCostPerSecond = 1f;
            var settings = MakeLoopSettings();
            settings.pools.Add(HuePool(Hue.Amber, 50f));
            var sim = MakeSimulation(settings);
            sim.Schedule(_takeRing, 1);
            sim.Schedule(_wait, 1);
            sim.BeginLoop();

            RunSeconds(sim, 11);

            Assert.That(sim.Loop.FindPool(Hue.Amber).Current, Is.EqualTo(40f).Within(0.01f));
            Assert.That(sim.Loop.Vitality.Current, Is.EqualTo(100f).Within(0.01f));
        }

        [Test]
        public void PuttingItDown_TakesItAway()
        {
            var sim = MakeSimulation();
            var lost = new List<int>();
            sim.ResourceLost += (resource, amount) => lost.Add(amount);
            sim.Schedule(_takeRing, 1);
            sim.Schedule(_dropRing, 1);
            sim.BeginLoop();

            RunSeconds(sim, 2);

            Assert.That(sim.AmountOf(_ring), Is.EqualTo(0));
            Assert.That(lost, Is.EqualTo(new[] { 1 }));
        }

        [Test]
        public void TheStashAndPackingList_SurviveSavingAndLoading()
        {
            var content = Make<GameContent>();
            content.tasks.Add(_takeRing);
            var sim = new Simulation(MakeLoopSettings(), TicksPerSecond, content);
            sim.Persistent.Stash[_ring] = 1;
            sim.SetPacked(_ring, true);

            var warnings = new List<string>();
            var restored = SaveAndLoad(sim, content, warnings);

            Assert.That(warnings, Is.Empty);
            Assert.That(restored.Stash[_ring], Is.EqualTo(1));
            Assert.That(restored.Packed, Has.Member(_ring));
        }

        [Test]
        public void PackedItems_AreStillInTheStash_WhenARunBegunButNotYetStartedIsSaved()
        {
            var content = Make<GameContent>();
            content.tasks.Add(_takeRing);
            var sim = new Simulation(MakeLoopSettings(), TicksPerSecond, content);
            sim.Persistent.Stash[_ring] = 1;
            sim.SetPacked(_ring, true);
            sim.BeginLoop(); // she has the ring; nothing queued yet, so the run doesn't start (and isn't saved)
            RunSeconds(sim, 1f);

            var restored = SaveAndLoad(sim, content);

            Assert.That(restored.Stash[_ring], Is.EqualTo(1), "back in the lab, since the run starts again");
            Assert.That(sim.StashOf(_ring), Is.EqualTo(0), "saving doesn't change the game being played");
        }
    }
}
