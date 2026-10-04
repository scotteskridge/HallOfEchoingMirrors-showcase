using System.Collections.Generic;
using HallOfEchoingMirrors.Core;
using NUnit.Framework;
using UnityEditor;

namespace HallOfEchoingMirrors.Tests.Balance
{
    /// <summary>
    /// A "sensible player" for the balance probe: plays a fresh save run after run, deciding one action
    /// at a time whenever the queue is empty, on the real assets. Shared moves live here; each act's
    /// route (e.g. <see cref="ActIRoute"/>) says what to do next. Test-side only: never shipped.
    /// </summary>
    public abstract class RouteBot
    {
        protected Simulation Sim { get; private set; }
        protected ProbePolicy Policy { get; private set; }
        /// <summary>Things worth a column or a note this run (switches flipped, a talk tried); cleared each run.</summary>
        protected readonly List<string> Notes = new List<string>();
        private string _lastRefusal;

        /// <summary>The act this route plays, for reports.</summary>
        public abstract string Act { get; }
        /// <summary>The ways of playing this route knows (tidy to loose).</summary>
        public abstract IReadOnlyList<ProbePolicy> Policies { get; }
        /// <summary>The run it's played to the end (e.g. the first walk out).</summary>
        public abstract bool IsFinished { get; }
        /// <summary>How far the game has got, in a few words, after a run.</summary>
        public abstract string Frontier();
        /// <summary>One decision with an empty queue: queue something and return null, or return why she stops.</summary>
        protected abstract string Decide();
        /// <summary>Called after each run so a route can count act-specific things (lab visits, talks).</summary>
        protected virtual void AfterRun() { }
        /// <summary>The route's own counters, as summary columns (same names every time).</summary>
        public virtual IReadOnlyList<(string column, string value)> Counters => new (string, string)[0];
        /// <summary>Called when a new save starts, to reset the counters.</summary>
        protected virtual void ResetCounters() { }

        /// <summary>Starts a fresh save on the shipped content with this way of playing.</summary>
        public void NewGame(LoopSettings settings, GameContent content, ProbePolicy policy)
        {
            Sim = new Simulation(settings, TickEngine.TicksPerSecond, content);
            Policy = policy;
            Sim.ActionRefused += (task, to, why) => _lastRefusal = why;
            Sim.SwitchFlipped += flipped => Notes.Add($"{flipped.name}@{Seconds}s");
            ResetCounters();
        }

        public Simulation Simulation => Sim;
        public IReadOnlyList<string> RunNotes => Notes;
        protected int Seconds => (int)(Sim.Loop.TicksElapsed / TickEngine.TicksPerSecond);

        /// <summary>Plays one run to its end; returns why the bot gave up early (ending the run), or null.</summary>
        public string PlayRun()
        {
            Notes.Clear();
            Sim.BeginLoop();
            string stall = null;
            int guard = 0;
            while (!Sim.Loop.IsOver)
            {
                if (Sim.Loop.CurrentTask == null && Sim.Queue.Count == 0)
                {
                    stall = Decide();
                    if (stall == null && Sim.Queue.Count == 0)
                        stall = "refused: " + _lastRefusal;
                    if (stall != null)
                    {
                        Sim.EndRunEarly();
                        break;
                    }
                }
                Sim.Tick();
                if (++guard > 3600 * TickEngine.TicksPerSecond)
                    Assert.Fail("a probe run took more than an hour of game time: the route is stuck in a loop");
            }
            AfterRun();
            return stall;
        }

        // ---------- Moves every route uses ----------

        protected static T Load<T>(string path) where T : UnityEngine.Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            Assert.That(asset, Is.Not.Null, $"balance probe: missing {path}");
            return asset;
        }

        protected NodeDefinition Here => Sim.Loop.CurrentNode;

        /// <summary>Offered where she is now, unlocked, and not used up for this run.</summary>
        protected bool Offered(TaskDefinition task) =>
            Sim.IsUnlocked(task) && Sim.IsAvailableAt(task, Here) && !Sim.IsDoneForThisRun(task);

        protected bool Has(ResourceDefinition item) => Sim.AmountOf(item) > 0;

        /// <summary>Queues one go of a task.</summary>
        protected string Do(TaskDefinition task)
        {
            Sim.Schedule(task, 1);
            return null;
        }

        /// <summary>Goes to the room (the shortest walk), or does the task there once she's in it.</summary>
        protected string Work(NodeDefinition room, TaskDefinition task)
        {
            if (Here != room)
            {
                Sim.ScheduleTrip(room);
                return null;
            }
            return Offered(task) ? Do(task) : $"{task.name} not offered in {room.name}";
        }

        /// <summary>A short "Skill level+mastery" list for the report.</summary>
        public string Skills()
        {
            var parts = new List<string>();
            foreach (var skill in Sim.KnownSkills())
                parts.Add($"{skill.name} {Sim.LevelOf(skill)}+{Sim.MasteryOf(skill)}");
            return string.Join(" ", parts);
        }
    }
}
