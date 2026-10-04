using System.Collections.Generic;

namespace HallOfEchoingMirrors.Core
{
    /// <summary>
    /// Reads a blurbs text file. "##" opens a bucket, "#" is a comment (the first comment under a
    /// bucket becomes its description), every other non-blank line is one blurb.
    /// </summary>
    public static class BlurbFile
    {
        public class Section
        {
            public string Id;
            public string Description;
            public readonly List<string> Lines = new List<string>();
        }

        public static List<Section> Parse(string text)
        {
            var sections = new List<Section>();
            Section current = null;
            foreach (var raw in (text ?? "").Replace("\r\n", "\n").Split('\n'))
            {
                string line = raw.Trim();
                if (line.Length == 0)
                    continue;

                if (line.StartsWith("##"))
                {
                    current = new Section { Id = line.Substring(2).Trim() };
                    sections.Add(current);
                }
                else if (line.StartsWith("#"))
                {
                    if (current != null && current.Description == null)
                        current.Description = line.Substring(1).Trim();
                }
                else if (current != null)
                {
                    current.Lines.Add(line);
                }
                // Lines before the first bucket are the file's own notes: ignored.
            }
            return sections;
        }
    }
}
