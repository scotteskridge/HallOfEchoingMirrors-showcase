using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using HallOfEchoingMirrors.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace HallOfEchoingMirrors.Tests
{
    /// <summary>
    /// The game text file: how it's read, and that every key the code asks for is really in it
    /// (so a typo is caught here, not on screen).
    /// </summary>
    public class GameTextTests
    {
        const string TextFile = "Assets/Text/game_text.txt";

        [TearDown]
        public void Unload() => GameText.Load("");

        [Test]
        public void Lines_AreReadBySection_WithCommentsAndBlanksSkipped()
        {
            var problems = GameText.Load("# notes\n\n## results\n# the card\ntime: Time\ntitle: Loop {loop}.\n## menu\nslot: Slot {slot}\n");

            Assert.That(problems, Is.Empty);
            Assert.That(GameText.Get("results.time"), Is.EqualTo("Time"));
            Assert.That(GameText.Get("results.title", ("loop", 4)), Is.EqualTo("Loop 4."));
            Assert.That(GameText.Get("menu.slot", ("slot", 2)), Is.EqualTo("Slot 2"));
        }

        [Test]
        public void BackslashN_StartsANewLine_AndColonsInTheTextAreKept()
        {
            GameText.Load("## a\nline: One: two\\nthree\n");

            Assert.That(GameText.Get("a.line"), Is.EqualTo("One: two\nthree"));
        }

        [Test]
        public void AMissingKey_ShowsItselfOnScreen_RatherThanBreaking()
        {
            GameText.Load("## a\nb: c\n");
            // The Console warning is part of the behaviour: it's how a writer finds the typo.
            LogAssert.Expect(LogType.Warning, "Game text: no line called \"a.nope\" in the text file.");

            Assert.That(GameText.Get("a.nope"), Is.EqualTo("[a.nope?]"));
        }

        [Test]
        public void ANumber_CanBeWrittenAsAnOrdinal()
        {
            GameText.Load("## ordinals\none: {n}st\ntwo: {n}nd\nthree: {n}rd\nother: {n}th\n## planning\nbegin: Entering the hall, {loop:ordinal}\n");

            Assert.That(GameText.Get("planning.begin", ("loop", 1)), Is.EqualTo("Entering the hall, 1st"));
            Assert.That(GameText.Get("planning.begin", ("loop", 3)), Is.EqualTo("Entering the hall, 3rd"));
            string[] expected = { "1st", "2nd", "3rd", "4th", "11th", "12th", "13th", "21st", "22nd", "101st", "111th" };
            long[] numbers = { 1, 2, 3, 4, 11, 12, 13, 21, 22, 101, 111 };
            for (int i = 0; i < numbers.Length; i++)
                Assert.That(GameText.Ordinal(numbers[i]), Is.EqualTo(expected[i]));
        }

        [Test]
        public void ALineWithNoKey_IsReported()
        {
            var problems = GameText.Load("## a\nno colon here\n");

            Assert.That(problems.Count, Is.EqualTo(1));
        }

        [Test]
        public void TheRealFile_ReadsCleanly()
        {
            var problems = GameText.Load(File.ReadAllText(TextFile));

            Assert.That(problems, Is.Empty, string.Join("\n", problems));
        }

        [Test]
        public void EveryTextKeyInTheScene_IsInTheFile()
        {
            GameText.Load(File.ReadAllText(TextFile));
            var missing = new List<string>();

            // Text Key components are saved in the scene as "_key: <key>".
            var textKey = new Regex(@"^\s*_key:\s*(\S+)\s*$");
            foreach (var path in Directory.GetFiles("Assets/Scenes", "*.unity", SearchOption.AllDirectories))
                foreach (var line in File.ReadAllLines(path))
                {
                    var match = textKey.Match(line);
                    if (match.Success && !GameText.Has(match.Groups[1].Value))
                        missing.Add($"{match.Groups[1].Value} ({Path.GetFileName(path)})");
                }

            Assert.That(missing, Is.Empty, "Text Keys with no line in " + TextFile + ":\n" + string.Join("\n", missing));
        }

        [Test]
        public void EveryKeyTheCodeAsksFor_IsInTheFile()
        {
            GameText.Load(File.ReadAllText(TextFile));
            var missing = new List<string>();

            // Every "section.key" string in the code whose section is one of the file's sections
            // (so keys on the line after a GameText.Get( are caught too).
            var sections = new HashSet<string>();
            foreach (var k in GameText.Keys)
                if (k.IndexOf('.') > 0)
                    sections.Add(k.Substring(0, k.IndexOf('.')));
            var key = new Regex("\"([a-z_]+)((?:\\.[a-z_]+)+)\"");
            foreach (var path in Directory.GetFiles("Assets/Scripts", "*.cs", SearchOption.AllDirectories))
                foreach (var line in File.ReadAllLines(path))
                    foreach (Match match in key.Matches(line))
                    {
                        string full = match.Groups[1].Value + match.Groups[2].Value;
                        if (sections.Contains(match.Groups[1].Value) && !GameText.Has(full))
                            missing.Add($"{full} ({Path.GetFileName(path)})");
                    }

            foreach (ClaraAttribute attribute in Enum.GetValues(typeof(ClaraAttribute)))
                if (attribute != ClaraAttribute.None && !GameText.Has("attributes." + attribute.ToString().ToLowerInvariant()))
                    missing.Add($"attributes.{attribute.ToString().ToLowerInvariant()}");

            foreach (Hue hue in Enum.GetValues(typeof(Hue)))
                if (hue != Hue.None && !GameText.Has("hues." + hue.ToString().ToLowerInvariant()))
                    missing.Add($"hues.{hue.ToString().ToLowerInvariant()}");

            Assert.That(missing, Is.Empty, "Missing from " + TextFile + ":\n" + string.Join("\n", missing));
        }

        [Test]
        public void HueNames_ComeFromTheTextFile_ForBothEnums()
        {
            GameText.Load("## hues\nruby: Crimson\namber: Honey\n");

            Assert.That(GameText.HueName(Hue.Ruby), Is.EqualTo("Crimson"));
            Assert.That(GameText.HueName(CostSource.Ruby), Is.EqualTo("Crimson"));
            Assert.That(GameText.HueName(CostSource.Amber), Is.EqualTo("Honey"), "by name, not by position");
        }

        [Test]
        public void HueName_ThrowsForWhatIsNotAHue()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => GameText.HueName(Hue.None));
            Assert.Throws<ArgumentOutOfRangeException>(() => GameText.HueName(CostSource.Vitality));
            Assert.Throws<ArgumentOutOfRangeException>(() => GameText.HueName(CostSource.AllPools));
        }

        [Test]
        public void RoomTitles_MidSentence_LowerOnlyALeadingArticle()
        {
            Assert.That(GameText.TitleInSentence("The Dark Corridor"), Is.EqualTo("the Dark Corridor"));
            Assert.That(GameText.TitleInSentence("A Dark Hall"), Is.EqualTo("a Dark Hall"));
            Assert.That(GameText.TitleInSentence("Dark Hall"), Is.EqualTo("Dark Hall"));
            Assert.That(GameText.TitleInSentence("Theatre of Glass"), Is.EqualTo("Theatre of Glass"), "not an article");
        }
    }
}
