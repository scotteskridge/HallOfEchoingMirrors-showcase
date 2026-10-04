using System;
using System.Collections.Generic;
using UnityEngine;

namespace HallOfEchoingMirrors.Core
{
    /// <summary>When a bucket speaks.</summary>
    public enum BlurbMoment
    {
        /// <summary>Every few seconds while she's busy (the timer).</summary>
        During = 0,
        /// <summary>The moment an action begins: "I begin chasing, but..."</summary>
        Starting = 1,
    }

    /// <summary>What Clara is doing when a blurb fires. Combine them (e.g. Travelling | Searching); none means any.</summary>
    [Flags]
    public enum BlurbActivity
    {
        Any = 0,
        Travelling = 1,
        Exploring = 2,
        Searching = 4,
        /// <summary>Any other task.</summary>
        Working = 8,
        /// <summary>Between tasks, e.g. paused with nothing queued.</summary>
        Idle = 16,
    }

    /// <summary>
    /// One bucket of short story lines in Clara's voice, and the rules for when it may speak.
    /// The lines come from a text file (Import Blurbs); the rules are set here and survive
    /// re-importing. See BlurbPicker for how a line is chosen.
    /// </summary>
    [CreateAssetMenu(fileName = "NewBlurbBucket", menuName = "Hall of Echoing Mirrors/Blurb Bucket")]
    public class BlurbBucket : ContentAsset
    {
        [Tooltip("Matches the ## name in the blurbs text file, e.g. hall_corridor.")]
        public string bucketId = "new_bucket";
        [Tooltip("Off: never fires (e.g. waiting for a system that isn't built yet).")]
        public bool enabled = true;
        [Tooltip("The comment under the ## line in the text file. Just a note for the writer.")]
        [TextArea(1, 3)] public string description;

        [Header("Which bucket speaks")]
        [Tooltip("When several buckets fit the moment, the highest priority speaks. Equal priorities take turns at random.")]
        public int priority = 20;
        [Tooltip("Chance this bucket speaks when it's chosen; if it stays quiet, the next one down gets a turn.")]
        [Range(0f, 1f)] public float chance = 1f;
        [Tooltip("How long this bucket steps aside for others after it speaks, in real seconds " +
                 "(0 = use the library's default). A bucket that should keep interrupting, e.g. low vitality, gets its own low value.")]
        [Min(0f)] public float cooldownSeconds;

        [Header("When it fits (all must be true; empty means no limit)")]
        [Tooltip("During: every few seconds while she's busy. Starting: the moment an action begins.")]
        public BlurbMoment moment = BlurbMoment.During;
        public BlurbActivity activities = BlurbActivity.Any;
        [Tooltip("Only during these particular tasks, e.g. Chase Roland.")]
        public List<TaskDefinition> tasks = new List<TaskDefinition>();
        [Tooltip("Only during a task tagged with any of these blurb topics, e.g. Gathering; empty = no restriction. " +
                 "Fires for any task with that topic, present or future, with no per-task edit here.")]
        public List<BlurbTopic> topics = new List<BlurbTopic>();
        [Tooltip("Only in these rooms.")]
        public List<NodeDefinition> rooms = new List<NodeDefinition>();
        [Tooltip("Only in rooms of these kinds.")]
        public List<NodeKind> roomKinds = new List<NodeKind>();
        [Tooltip("First loop it can fire in (0 = from the start).")]
        [Min(0)] public int fromLoop;
        [Tooltip("Last loop it can fire in (0 = no end).")]
        [Min(0)] public int toLoop;
        [Tooltip("Every one of these must have flipped, e.g. the dark has been found.")]
        public List<SwitchDefinition> needsFlipped = new List<SwitchDefinition>();
        [Tooltip("None of these may have flipped, e.g. 'the mirror gem is crafted' ends 'reaching'.")]
        public List<SwitchDefinition> endsWhenFlipped = new List<SwitchDefinition>();
        [Tooltip("She must be holding these, e.g. 1 Roland's ring.")]
        public List<ResourceAmount> needsHeld = new List<ResourceAmount>();
        [Tooltip("She must be holding none of these.")]
        public List<ResourceDefinition> needsNotHeld = new List<ResourceDefinition>();
        [Tooltip("Only when vitality is below this share of its maximum (0 = any vitality). E.g. 0.3 for 'low vitality'.")]
        [Range(0f, 1f)] public float vitalityBelow;

        [Header("Tapering off")]
        [Tooltip("From this loop on, the chance falls away... (0 = never tapers)")]
        [Min(0)] public int taperFromLoop;
        [Tooltip("...until it's silent at this loop.")]
        [Min(0)] public int silentFromLoop;

        [Header("Lines (from the text file: re-import rather than editing here)")]
        [TextArea(1, 4)] public List<string> lines = new List<string>();
    }
}
