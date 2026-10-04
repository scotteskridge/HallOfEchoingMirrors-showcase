using System.Collections.Generic;
using HallOfEchoingMirrors.Core;

namespace HallOfEchoingMirrors.Tests.Balance
{
    /// <summary>
    /// Act I's route for the balance probe: the chase, the hall's searches, the corridor's candles (lit
    /// again every run), the Dark Corridor's search, the lab's talk (earn insight, then push) and the gem,
    /// then the walk out. Reads gates and counts from the assets, so retuning them needs no change here.
    /// </summary>
    public class ActIRoute : RouteBot
    {
        private readonly NodeDefinition _mirror, _hall, _left, _right, _dark, _lab;
        private readonly TaskDefinition _explore, _chase, _wisp, _flint, _candle, _lightHall, _lightCorridor, _satchelTask,
            _pouchTask, _fillPhial, _feed, _takeRing, _watch, _study, _attend, _talk, _earringsTask, _denseTask,
            _practise, _craft, _exit;
        private readonly ResourceDefinition _wispItem, _flintItem, _candleItem, _hallLight, _corridorLight, _satchel, _pouch,
            _phial, _dense, _ring, _insight, _gem, _quickened;
        private readonly SwitchDefinition _alight, _rolandTakesTheRing, _home;
        private readonly int _corridorCandles, _craftGate;
        private readonly SkillDefinition _crafting;
        private int _labVisits, _talkTries, _lastFeedTry = -99, _run;

        private static readonly ProbePolicy[] AllPolicies =
        {
            new ProbePolicy { Name = "A: wisps only" },
            new ProbePolicy { Name = "B: 15 hall candles, no phials", LightTheDarkHall = true, HallCandles = 15 },
            new ProbePolicy { Name = "C: 15 hall candles, 10 phials (tidy)", LightTheDarkHall = true, HallCandles = 15, FillPhials = true, Phials = 10 },
            new ProbePolicy { Name = "D: 8 hall candles, 5 phials (loose)", LightTheDarkHall = true, HallCandles = 8, FillPhials = true, Phials = 5 },
        };

        public ActIRoute()
        {
            NodeDefinition Node(string n) => Load<NodeDefinition>($"Assets/Data/Places/{n}.asset");
            TaskDefinition Task(string n) => Load<TaskDefinition>($"Assets/Data/Tasks/{n}.asset");
            ResourceDefinition Item(string n) => Load<ResourceDefinition>($"Assets/Data/Items/{n}.asset");
            var content = Load<GameContent>("Assets/Data/GameContent.asset");

            _mirror = Node("TheSmokyMirror"); _hall = Node("Junction"); _left = Node("LeftCorridor");
            _right = Node("RightCorridor"); _dark = Node("HangingMirrors"); _lab = Node("OtherLaboratory");
            _explore = content.exploreVerb;
            _chase = Task("Tutorial/ChaseRoland"); _wisp = Task("Hall/GatherAWisp"); _flint = Task("Hall/InstantiateFlintAndSteel");
            _candle = Task("Hall/InstantiateACandle"); _lightHall = Task("Hall/LightTheCandles");
            _lightCorridor = Task("Hall/LightACandleInTheCorridor"); _satchelTask = Task("Hall/InstantiateASatchel");
            _pouchTask = Task("Hall/InstantiateAnEmptyPhial"); _fillPhial = Task("Hall/FillABottledWell");
            _feed = Task("Hall/FeedYourHoursToTheFlames"); _takeRing = Task("Hall/TakeTheRing");
            _exit = Task("Hall/ExitThroughTheGlowingMirror");
            _watch = Task("Lab/WatchHim"); _study = Task("Lab/StudyTheTome"); _attend = Task("Lab/Attend");
            _talk = Task("Lab/TalkToRoland"); _earringsTask = Task("Lab/TakeTheEarrings");
            _denseTask = Task("Lab/DrawADenseWisp"); _practise = Task("Lab/PractiseTheCut"); _craft = Task("Lab/CraftTheGem");
            _wispItem = Item("Wisp"); _flintItem = Item("Flintandsteel"); _candleItem = Item("Hangingcandle");
            _hallLight = Item("Candlelight"); _corridorLight = Item("CorridorCandlelight"); _satchel = Item("Satchel");
            _pouch = Item("Emptyphial"); _phial = Item("Bottledwell"); _dense = Item("Densewisp"); _ring = Item("Rolandsring");
            _insight = Item("Whathedidthatnight"); _gem = Item("Thegem"); _quickened = Item("QuickenedHours");
            _alight = Load<SwitchDefinition>("Assets/Data/Switches/AllTwentyFiveAlight.asset");
            _rolandTakesTheRing = Load<SwitchDefinition>("Assets/Data/Switches/RolandTakesTheRing.asset");
            _home = Load<SwitchDefinition>("Assets/Data/Switches/Home.asset");
            _crafting = Load<SkillDefinition>("Assets/Data/Skills/Crafting.asset");

            // Gates read from the assets: one source of truth, so a retune needs no edit here.
            var way = _left.ways.Find(w => w != null && w.to == _dark);
            var need = way?.needs.Find(n => n.resource == _corridorLight);
            _corridorCandles = need != null ? need.amount : _alight.triggerAmount;
            var gate = _craft.requiresSkills.Find(r => r.skill == _crafting);
            _craftGate = gate != null ? gate.level : 0;
        }

        public override string Act => "Act I";
        public override IReadOnlyList<ProbePolicy> Policies => AllPolicies;
        public override bool IsFinished => Sim.IsFlipped(_home);

        public override IReadOnlyList<(string column, string value)> Counters => new[]
        {
            ("lab_visits", _labVisits.ToString()),
            ("talk_attempts", _talkTries.ToString()),
        };

        protected override void ResetCounters()
        {
            _labVisits = 0;
            _talkTries = 0;
            _lastFeedTry = -99;
            _run = 0;
        }

        protected override void AfterRun()
        {
            _run++;
            bool talked = Notes.Exists(n => n.StartsWith("talk at"));
            if (talked)
                _talkTries++;
            if (talked || Sim.Loop.HasReached(_lab) && !Sim.IsFlipped(_rolandTakesTheRing))
                _labVisits++;
        }

        public override string Frontier()
        {
            if (Has(_gem)) return "gem made";
            if (Sim.IsFlipped(_rolandTakesTheRing)) return "talked to Roland";
            if (Sim.IsFullyExplored(_dark)) return $"lab (insight {Sim.AmountOf(_insight)})";
            if (Sim.IsFlipped(_alight)) return $"dark corridor {Sim.ExploredFraction(_dark):P0}";
            if (Sim.IsFullyExplored(_left)) return "corridors searched";
            return $"searching (mirror {Sim.ExploredFraction(_mirror):P0} hall {Sim.ExploredFraction(_hall):P0} " +
                   $"right {Sim.ExploredFraction(_right):P0} left {Sim.ExploredFraction(_left):P0})";
        }

        protected override string Decide()
        {
            if (Offered(_chase))
                return Do(_chase);
            // Walk out as soon as the gem exists (it's kept, so a later run walks straight out).
            if (Has(_gem))
                return Work(_mirror, _exit);

            // Restoratives first wherever they're to hand (they're used automatically).
            if (Offered(_wisp) && Sim.RoomFor(_wispItem) > PocketsToKeepFree())
                return Do(_wisp);
            if (Offered(_earringsTask))
                return Do(_earringsTask);
            if (Offered(_denseTask) && Sim.RoomFor(_dense) > 0)
                return Do(_denseTask);
            if (Policy.LightTheDarkHall && Here == _hall && Has(_flintItem) && Sim.AmountOf(_hallLight) < Policy.HallCandles && Offered(_lightHall))
                return Has(_candleItem) ? Do(_lightHall) : Do(_candle);
            if (Policy.FillPhials && Here != _lab && Has(_pouch) && Offered(_fillPhial) && Sim.IsFlipped(_alight)
                && Sim.RoomFor(_phial) > 0 && Sim.AmountOf(_phial) < Policy.Phials)
                return Do(_fillPhial);

            // The story's way through, in order.
            if (!Sim.IsFullyExplored(_mirror)) return Work(_mirror, _explore);
            if (!Sim.IsFullyExplored(_hall)) return Work(_hall, _explore);
            if (!Sim.IsFullyExplored(_right)) return Work(_right, _explore);
            if (!Sim.IsFullyExplored(_left)) return Work(_left, _explore);
            if (Policy.LightTheDarkHall && !Has(_flintItem)) return Work(_hall, _flint);
            // The pouch first (it takes no pocket), then the corridor, lit again every run before going deeper.
            if (Policy.FillPhials && Sim.IsFlipped(_alight) && !Has(_pouch) && !Sim.IsDoneForThisRun(_pouchTask))
                return Work(_right, _pouchTask);
            if (Sim.AmountOf(_corridorLight) < _corridorCandles) return LightTheCorridor();
            // Feed needs the corridor lit this run. A player who failed it waits a few runs before trying again.
            if (!Has(_quickened) && _run - _lastFeedTry >= 3)
            {
                if (Here == _left)
                    _lastFeedTry = _run;
                return Work(_left, _feed);
            }
            if (!Sim.IsFullyExplored(_dark)) return Work(_dark, _explore);
            if (!Sim.IsFlipped(_rolandTakesTheRing)) return TheTalk();
            return TheGem();
        }

        private int PocketsToKeepFree()
        {
            if (Sim.AmountOf(_corridorLight) >= _corridorCandles)
                return 0;
            if (!Has(_satchel) || !Has(_flintItem))
                return 2; // room for the flint and the satchel
            return System.Math.Max(0, _corridorCandles - Sim.AmountOf(_corridorLight) - Sim.AmountOf(_candleItem));
        }

        private string LightTheCorridor()
        {
            if (!Has(_satchel)) return Work(_left, _satchelTask);
            if (!Has(_flintItem)) return Work(_hall, _flint);
            int need = _corridorCandles - Sim.AmountOf(_corridorLight) - Sim.AmountOf(_candleItem);
            if (need > 0 && Sim.RoomFor(_candleItem) > 0) return Work(_hall, _candle);
            if (Has(_candleItem)) return Work(_left, _lightCorridor);
            return "no pocket room for the corridor's candles";
        }

        private string TheTalk()
        {
            if (Here != _lab)
                return Has(_ring) ? Work(_lab, _explore) : Work(_right, _takeRing);
            if (!Sim.IsFullyExplored(_lab)) return Do(_explore);
            // Earn whatever insight is on offer this run (kept), then push the talk.
            if (Offered(_watch)) return Do(_watch);
            if (Offered(_study)) return Do(_study);
            if (Offered(_attend)) return Do(_attend);
            Notes.Add($"talk at insight {Sim.AmountOf(_insight)}, {Sim.Loop.Vitality.Current:0} vitality, {Seconds}s");
            return Do(_talk); // a refusal (a missing need) shows as the run's stop reason
        }

        private string TheGem()
        {
            if (Here != _lab) return Work(_lab, _explore);
            if (!Offered(_craft) && !Sim.IsFullyExplored(_lab)) return Do(_explore);
            if (Sim.StrengthOf(_crafting) < _craftGate && Offered(_practise)) return Do(_practise);
            if (!Offered(_craft))
                return "craft not offered";
            Notes.Add($"craft at {Sim.Loop.Vitality.Current:0} vitality, {Seconds}s");
            return Do(_craft);
        }
    }
}
