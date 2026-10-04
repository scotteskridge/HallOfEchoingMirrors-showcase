using System.Collections.Generic;
using HallOfEchoingMirrors.Core;
using NUnit.Framework;
using UnityEditor;

namespace HallOfEchoingMirrors.Tests.Balance
{
    /// <summary>
    /// The balance tools (docs/balance-tools.md). The probe itself is Explicit (category Balance): it runs
    /// only when asked, since it reads the balance rather than checking a rule. The other two run with
    /// the suite: what-ifs always put the numbers back, and scenarios.txt names only real assets and fields.
    /// </summary>
    public class BalanceProbeTests
    {
        [Test, Explicit("Balance probe: run by hand (Test Runner, category Balance) during a balance pass"), Category("Balance")]
        public void Probe_ActI()
        {
            new BalanceProbe(new ActIRoute()).Run(Scenarios());
            UnityEngine.Debug.Log($"Balance probe written to {BalanceProbe.ReportFolder}/");
        }

        [Test]
        public void AWhatIf_PutsEveryValueBack()
        {
            var settings = AssetDatabase.LoadAssetAtPath<LoopSettings>("Assets/Data/LoopSettings.asset");
            float before = settings.drainGrowthPerMinute;
            bool wasDirty = EditorUtility.IsDirty(settings);
            var scenario = new WhatIf("test");
            scenario.Changes.Add(new WhatIf.Change { AssetPath = "Assets/Data/LoopSettings.asset", FieldPath = "drainGrowthPerMinute", Value = "0.123" });

            try
            {
                scenario.Apply();
                Assert.That(settings.drainGrowthPerMinute, Is.EqualTo(0.123f), "tried in memory");
            }
            finally
            {
                scenario.Undo();
            }

            Assert.That(settings.drainGrowthPerMinute, Is.EqualTo(before), "and put back exactly");
            if (!wasDirty)
                Assert.That(EditorUtility.IsDirty(settings), Is.False, "and not left looking unsaved");
        }

        [Test]
        public void TheScenariosFile_NamesOnlyRealAssetsAndFields()
        {
            foreach (var scenario in Scenarios())
            {
                try
                {
                    scenario.Apply(); // fails loudly on a missing asset, field or a value of the wrong kind
                }
                finally
                {
                    scenario.Undo();
                }
            }
        }

        private static List<WhatIf> Scenarios() =>
            System.IO.File.Exists(BalanceProbe.ScenariosFile)
                ? WhatIf.ReadFile(BalanceProbe.ScenariosFile)
                : new List<WhatIf> { new WhatIf("shipped") };
    }
}
