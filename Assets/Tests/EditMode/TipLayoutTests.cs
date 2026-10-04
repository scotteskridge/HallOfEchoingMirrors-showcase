using HallOfEchoingMirrors.UI;
using NUnit.Framework;

namespace HallOfEchoingMirrors.Tests
{
    /// <summary>The tooltip's plain-data layout: meanings carry a second cue, and the "since" slot stays empty until filled.</summary>
    public class TipLayoutTests
    {
        [Test]
        public void Meaning_MapsToRoleAndCue()
        {
            Assert.That(TipMeanings.RoleOf(TipMeaning.Met), Is.EqualTo(ColourRole.TipMet));
            Assert.That(TipMeanings.CueOf(TipMeaning.Met), Does.Contain("✓"));
            Assert.That(TipMeanings.RoleOf(TipMeaning.Unmet), Is.EqualTo(ColourRole.TipUnmet));
            Assert.That(TipMeanings.CueOf(TipMeaning.Unmet), Does.Contain("○"));
            Assert.That(TipMeanings.RoleOf(TipMeaning.Time), Is.EqualTo(ColourRole.TipTime));
            Assert.That(TipMeanings.RoleOf(TipMeaning.Vitality), Is.EqualTo(ColourRole.TipVitality));
            Assert.That(TipMeanings.RoleOf(TipMeaning.Plain), Is.EqualTo(ColourRole.TextMain));
        }

        [Test]
        public void Meaning_OnlyMetAndUnmetHaveCues()
        {
            foreach (var meaning in new[] { TipMeaning.Plain, TipMeaning.Time, TipMeaning.Vitality })
                Assert.That(TipMeanings.CueOf(meaning), Is.Empty, $"{meaning} is shown by its unit word, not a mark");
        }

        [Test]
        public void Since_EmptyUnlessSet()
        {
            var row = new TipLayout.Row { Label = "Takes", Value = "1.1s" };

            Assert.That(row.HasSince, Is.False, "nothing is shown by default, never \"−0.0s\"");

            row.Since = "−0.3s since last run";
            Assert.That(row.HasSince, Is.True);
        }

        [Test]
        public void Key_ChangesWhenAFactChanges()
        {
            var a = new TipLayout { Title = "Search" };
            a.Rows.Add(new TipLayout.Row { Label = "Takes", Value = "1.1s" });
            var b = new TipLayout { Title = "Search" };
            b.Rows.Add(new TipLayout.Row { Label = "Takes", Value = "1.1s" });
            Assume.That(a.Key(), Is.EqualTo(b.Key()));

            b.Rows[0].Value = "1.0s";

            Assert.That(a.Key(), Is.Not.EqualTo(b.Key()));
        }
    }
}
