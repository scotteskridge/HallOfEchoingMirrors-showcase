using System.Collections.Generic;
using System.Text;
using HallOfEchoingMirrors.Core;
using UnityEditor;

namespace HallOfEchoingMirrors.Tests.Balance
{
    /// <summary>
    /// Plays a route from a fresh save under every way of playing it knows, for each what-if scenario,
    /// and writes the results to docs/balance/latest/: probe_summary.csv (when each way of playing walks
    /// out), probe_runs.csv (every run), summary.md (the same, readable) and balance_snapshot.csv (the
    /// shipped numbers). How to use it: docs/balance-tools.md.
    /// </summary>
    public class BalanceProbe
    {
        /// <summary>Where the reports go (in git, so passes can be compared).</summary>
        public const string ReportFolder = "docs/balance/latest";
        /// <summary>The what-if scenarios file.</summary>
        public const string ScenariosFile = "docs/balance/scenarios.txt";

        private readonly RouteBot _route;
        private readonly int _runLimit;
        private readonly Csv _runs = new Csv("scenario", "policy", "run", "seconds", "ending", "reached", "died_in", "doing",
            "vitality_lost", "travel_paid", "stopped_because", "notes", "skills");
        private Csv _summary;
        private readonly StringBuilder _readable = new StringBuilder();

        public BalanceProbe(RouteBot route, int runLimit = 30)
        {
            _route = route;
            _runLimit = runLimit;
        }

        /// <summary>Runs every scenario (each undone afterwards, even if a run fails) and saves the reports.</summary>
        public void Run(IReadOnlyList<WhatIf> scenarios)
        {
            GameText.Load(System.IO.File.ReadAllText("Assets/Text/game_text.txt"));
            var settings = AssetDatabase.LoadAssetAtPath<LoopSettings>("Assets/Data/LoopSettings.asset");
            var content = AssetDatabase.LoadAssetAtPath<GameContent>("Assets/Data/GameContent.asset");

            _readable.AppendLine($"# Balance probe: {_route.Act}, {System.DateTime.Now:yyyy-MM-dd HH:mm}");
            _readable.AppendLine();
            _readable.AppendLine("| Scenario | Way of playing | Walks out on run | Counters |");
            _readable.AppendLine("| --- | --- | --- | --- |");
            foreach (var scenario in scenarios)
            {
                try
                {
                    scenario.Apply();
                    foreach (var policy in _route.Policies)
                        Play(scenario, policy, settings, content);
                }
                finally
                {
                    scenario.Undo();
                }
            }
            _summary.Save($"{ReportFolder}/probe_summary.csv");
            _runs.Save($"{ReportFolder}/probe_runs.csv");
            BalanceSnapshot.Take().Save($"{ReportFolder}/balance_snapshot.csv");
            _readable.AppendLine();
            _readable.AppendLine("Scenarios and their changes:");
            foreach (var scenario in scenarios)
                _readable.AppendLine($"- **{scenario.Name}**: {(scenario.Changes.Count == 0 ? "shipped numbers" : string.Join("; ", scenario.Changes))}");
            System.IO.File.WriteAllText($"{ReportFolder}/summary.md", _readable.ToString());
        }

        private void Play(WhatIf scenario, ProbePolicy policy, LoopSettings settings, GameContent content)
        {
            _route.NewGame(settings, content, policy);
            int exitRun = 0;
            for (int run = 1; run <= _runLimit && exitRun == 0; run++)
            {
                string stopped = _route.PlayRun();
                var loop = _route.Simulation.Loop;
                _runs.Row(scenario.Name, policy.Name, run, loop.TicksElapsed / (float)TickEngine.TicksPerSecond, loop.EndReason,
                    _route.Frontier(), loop.CurrentNode != null ? loop.CurrentNode.name : "", loop.CurrentTask != null ? loop.CurrentTask.name : "",
                    loop.VitalityLostThisRun, loop.MoveVitalityPaid, stopped ?? "", string.Join("; ", _route.RunNotes), _route.Skills());
                if (_route.IsFinished)
                    exitRun = run;
            }

            var counters = _route.Counters;
            if (_summary == null)
            {
                var header = new List<string> { "scenario", "policy", "walks_out_on_run" };
                foreach (var (column, _) in counters)
                    header.Add(column);
                _summary = new Csv(header.ToArray());
            }
            var row = new List<object> { scenario.Name, policy.Name, exitRun > 0 ? exitRun.ToString() : $"not in {_runLimit}" };
            var readable = new List<string>();
            foreach (var (column, value) in counters)
            {
                row.Add(value);
                readable.Add($"{column} {value}");
            }
            _summary.Row(row.ToArray());
            _readable.AppendLine($"| {scenario.Name} | {policy.Name} | {(exitRun > 0 ? exitRun.ToString() : "—")} | {string.Join(", ", readable)} |");
        }
    }
}
