using UnityEngine;

namespace HallOfEchoingMirrors.Core
{
    /// <summary>
    /// One passage of story, shown as a pop-up and kept in Clara's journal.
    /// The words live in a .txt file (written in any text editor): the first line is the title,
    /// everything after it is the body.
    /// </summary>
    [CreateAssetMenu(fileName = "NewStoryBeat", menuName = "Hall of Echoing Mirrors/Story Beat")]
    public class StoryBeat : ContentAsset
    {
        [Tooltip("A .txt file. First line = title, the rest = the passage.")]
        public TextAsset text;
        [Tooltip("Optional: fills {cost per second} in the passage with this resource's Carry Cost Per Second, " +
                 "so the number never has to be hard-coded into the prose. E.g. Roland's ring.")]
        public ResourceDefinition costSource;

        public string Title => Split().title;
        public string Body => Substitute(Split().body);

        private (string title, string body) Split()
        {
            if (text == null)
                return (name, "");

            string all = text.text.Replace("\r\n", "\n").Trim();
            int newline = all.IndexOf('\n');
            return newline < 0
                ? (all, "")
                : (all.Substring(0, newline).Trim(), all.Substring(newline + 1).Trim());
        }

        private string Substitute(string body)
        {
            if (costSource == null)
                return body;
            return body.Replace("{cost per second}", costSource.carryCostPerSecond.ToString());
        }
    }
}
