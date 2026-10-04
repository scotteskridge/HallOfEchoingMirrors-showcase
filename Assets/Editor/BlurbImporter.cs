using System.Collections.Generic;
using System.IO;
using HallOfEchoingMirrors.Core;
using UnityEditor;
using UnityEngine;

namespace HallOfEchoingMirrors.EditorTools
{
    /// <summary>
    /// Menu: Hall of Echoing Mirrors → Story → Import Blurbs. Reads every .txt in Assets/Story/Blurbs
    /// and makes or updates one Blurb Bucket asset per "## name". Only the lines and description
    /// are replaced: rules set on a bucket (priority, rooms, loops...) are kept. Run it again
    /// after editing the text.
    /// </summary>
    public static class BlurbImporter
    {
        const string SourceFolder = "Assets/Story/Blurbs";
        const string BucketFolder = "Assets/Data/Blurbs";
        // Content the starting rules point at. If one moves, its rule is simply left for the Inspector.
        const string ChasePath = "Assets/Data/Tasks/Tutorial/ChaseRoland.asset";
        const string RingPath = "Assets/Data/Items/Rolandsring.asset";
        const string DarkRoomPath = "Assets/Data/Places/HangingMirrors.asset";
        const string MirrorsInTheDarkPath = "Assets/Data/Switches/MirrorsInTheDark.asset";
        const string RolandTakesTheRingPath = "Assets/Data/Switches/RolandTakesTheRing.asset";
        const string TheGemIsMadePath = "Assets/Data/Switches/TheGemIsMade.asset";

        [MenuItem("Hall of Echoing Mirrors/Story/Import Blurbs", false, 40)]
        public static void Import()
        {
            if (!AssetDatabase.IsValidFolder(SourceFolder))
            {
                EditorUtility.DisplayDialog("Import Blurbs", $"Put the blurb text files in {SourceFolder} first.", "OK");
                return;
            }

            var library = EditorUiFactory.GetOrCreateAsset<BlurbLibrary>(BucketFolder, "BlurbLibrary", _ => { });
            Undo.RecordObject(library, "Import Blurbs");
            var imported = new HashSet<string>();
            int buckets = 0, lines = 0;

            foreach (string guid in AssetDatabase.FindAssets("t:TextAsset", new[] { SourceFolder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith(".txt"))
                    continue;

                foreach (var section in BlurbFile.Parse(File.ReadAllText(path)))
                {
                    if (!imported.Add(section.Id))
                    {
                        Debug.LogWarning($"Import Blurbs: \"## {section.Id}\" appears more than once; only the first was used ({path}).");
                        continue;
                    }

                    bool isNew = false;
                    var bucket = EditorUiFactory.GetOrCreateAsset<BlurbBucket>(BucketFolder, section.Id, b =>
                    {
                        b.bucketId = section.Id;
                        StartingRules(b);
                        isNew = true;
                    });
                    Undo.RecordObject(bucket, "Import Blurbs");
                    bucket.bucketId = section.Id;
                    bucket.description = section.Description ?? "";
                    bucket.lines = new List<string>(section.Lines);
                    EditorUtility.SetDirty(bucket);
                    if (!library.buckets.Contains(bucket))
                        library.buckets.Add(bucket);

                    buckets++;
                    lines += section.Lines.Count;
                    if (isNew)
                        Debug.Log($"Import Blurbs: new bucket {section.Id} ({section.Lines.Count} lines). " +
                                  (bucket.enabled ? "Check its rules in the Inspector." : "Switched off until its system exists."), bucket);
                }
            }

            foreach (var bucket in library.buckets)
                if (bucket != null && !imported.Contains(bucket.bucketId))
                    Debug.LogWarning($"Import Blurbs: {bucket.bucketId} is no longer in any text file. Its old lines are kept; " +
                                     "delete the asset if it's gone for good.", bucket);

            var content = EditorUiFactory.LoadContent();
            if (content != null && content.blurbs == null)
            {
                Undo.RecordObject(content, "Import Blurbs");
                content.blurbs = library;
                EditorUtility.SetDirty(content);
            }
            if (content != null)
                foreach (var bucket in library.buckets)
                    if (bucket != null && bucket.enabled)
                        WarnIfCanNeverFire(bucket, content);

            EditorUtility.SetDirty(library);
            AssetDatabase.SaveAssets();
            Debug.Log($"Import Blurbs: {buckets} buckets, {lines} lines. Library: {BucketFolder}/BlurbLibrary.", library);
        }

        /// <summary>
        /// One-off fixes for bucket rules that don't match their own file comment (found by audit,
        /// not by StartingRules, which only ever runs once per bucket). Safe to run twice: each fix
        /// only applies while the field is still at the value it's guarding against, so a value the
        /// user has since tuned by hand is left alone. Add to this list as more turn up, or once a
        /// fix's content (a task, switch or room) is finally built.
        /// </summary>
        [MenuItem("Hall of Echoing Mirrors/Story/Fix Known Blurb Rules", false, 41)]
        public static void FixKnownRules()
        {
            var library = AssetDatabase.LoadAssetAtPath<BlurbLibrary>($"{BucketFolder}/BlurbLibrary.asset");
            if (library == null)
            {
                EditorUtility.DisplayDialog("Fix Known Blurb Rules", "No Blurb Library found. Run Import Blurbs first.", "OK");
                return;
            }

            int applied = 0;
            applied += FixRule(library, "hall_corridor", "before the dark is found",
                b => b.endsWhenFlipped.Count == 0,
                b =>
                {
                    var dark = LoadForRule<SwitchDefinition>(MirrorsInTheDarkPath, "hall_corridor");
                    if (dark != null)
                        b.endsWhenFlipped.Add(dark);
                });
            applied += FixRule(library, "ring_refused", "fires occasionally in the corridor",
                b => b.roomKinds.Count == 0,
                b => b.roomKinds.Add(NodeKind.Hall));
            applied += FixRule(library, "start_chase", "the chase in loop 1",
                b => b.toLoop == 0,
                b => b.toLoop = 1);
            applied += FixRule(library, "lab_sealed", "loop 4 onward",
                b => b.fromLoop == 0,
                b => b.fromLoop = 4);

            // Once Roland has the ring she never needs or carries it again (plan 027a), so the lines
            // about lacking it, leaving it, or the lab refusing her without it would read wrong.
            var takesTheRing = LoadForRule<SwitchDefinition>(RolandTakesTheRingPath, "ring_present, ring_refused and lab_sealed");
            foreach (string bucketId in new[] { "ring_present", "ring_refused", "lab_sealed" })
                applied += FixRule(library, bucketId, "ends when Roland takes the ring",
                    b => !b.endsWhenFlipped.Contains(takesTheRing),
                    b =>
                    {
                        if (takesTheRing != null)
                            b.endsWhenFlipped.Add(takesTheRing);
                    });

            // Each bucket's own hand-curated Tasks list is the evidence for its topic: whatever was
            // listed under "gathering_wisps" is, by construction, a Gathering task. So the fix tags
            // those exact tasks (only if a task doesn't already carry some other topic by hand) and
            // then switches the bucket to matching by topic instead of by task.
            foreach (string bucketId in new[] { "gathering_wisps", "start_gather_wisp" })
                applied += FixRule(library, bucketId, "blurb topic Gathering, not a hardcoded task list",
                    b => b.topics.Count == 0 && b.tasks.Count > 0,
                    b => MigrateTasksToTopic(b, BlurbTopic.Gathering));
            foreach (string bucketId in new[] { "instantiating", "start_instantiate" })
                applied += FixRule(library, bucketId, "blurb topic Instantiating, not a hardcoded task list",
                    b => b.topics.Count == 0 && b.tasks.Count > 0,
                    b => MigrateTasksToTopic(b, BlurbTopic.Instantiating));

            // reaching is "cut off once the mirror gem is crafted" (plan 027c).
            var gemIsMade = AssetDatabase.LoadAssetAtPath<SwitchDefinition>(TheGemIsMadePath);
            if (gemIsMade == null)
                Debug.LogError($"Fix Known Blurb Rules: {TheGemIsMadePath} is missing (run setup step 101), so reaching can't end at it.");
            else
                applied += FixRule(library, "reaching", "ends when the gem is made",
                    b => !b.endsWhenFlipped.Contains(gemIsMade),
                    b => b.endsWhenFlipped.Add(gemIsMade));

            // Placeholder rule: Cut the Stone and Fill a Phial of Memory share TaskKind.Instantiate/
            // Gather with the tasks above, but were never in these buckets' Tasks lists, so they stay
            // untagged (BlurbTopic.None) rather than being guessed into a topic here (PROJECT_NOTES.md).

            AssetDatabase.SaveAssets();
            Debug.Log($"Fix Known Blurb Rules: applied {applied} fix(es).");
        }

        /// <summary>
        /// Tags every task still in the bucket's Tasks list with the topic (unless a task already
        /// carries a different one, set by hand), then clears the list: the bucket now matches any
        /// task with that topic instead of only these exact ones.
        /// </summary>
        private static void MigrateTasksToTopic(BlurbBucket b, BlurbTopic topic)
        {
            foreach (var task in b.tasks)
                if (task != null && task.blurbTopic == BlurbTopic.None)
                {
                    Undo.RecordObject(task, "Fix Known Blurb Rules");
                    task.blurbTopic = topic;
                    EditorUtility.SetDirty(task);
                }
            b.topics.Add(topic);
            b.tasks.Clear();
        }

        /// <summary>
        /// Applies <paramref name="fix"/> to the named bucket only while <paramref name="stillNeedsIt"/>
        /// is true, so a value the user has already tuned by hand is never overwritten. Checks
        /// <paramref name="stillNeedsIt"/> again afterwards: if the fix didn't actually change anything
        /// (e.g. the switch or task it points at failed to load), that's logged as a failure, not
        /// silently counted as applied.
        /// </summary>
        private static int FixRule(BlurbLibrary library, string bucketId, string note, System.Func<BlurbBucket, bool> stillNeedsIt, System.Action<BlurbBucket> fix)
        {
            var bucket = library.buckets.Find(b => b != null && b.bucketId == bucketId);
            if (bucket == null)
            {
                Debug.LogWarning($"Fix Known Blurb Rules: no bucket called {bucketId} (\"{note}\").");
                return 0;
            }
            if (!stillNeedsIt(bucket))
                return 0; // already tuned by hand, or already fixed

            Undo.RecordObject(bucket, "Fix Known Blurb Rules");
            fix(bucket);
            if (stillNeedsIt(bucket))
            {
                Debug.LogError($"Fix Known Blurb Rules: {bucketId} (\"{note}\") still needs its fix after trying — " +
                                "check the content it points at still exists where expected.");
                return 0;
            }
            EditorUtility.SetDirty(bucket);
            return 1;
        }

        /// <summary>
        /// Rules for a bucket the first time it's imported, following the notes in
        /// exploration_blurbs_act1.txt.
        /// Placeholder rule: these starting numbers (priorities, chances, loop ranges) are first guesses; tune them on the asset.
        /// Priorities: low vitality → ring carried → reaching → the action → the place.
        /// </summary>
        private static void StartingRules(BlurbBucket b)
        {
            // "start_…" buckets speak the moment an action begins.
            if (b.bucketId.StartsWith("start_"))
                b.moment = BlurbMoment.Starting;

            switch (b.bucketId)
            {
                case "start_chase":
                    b.priority = 30;
                    var chase = LoadForRule<TaskDefinition>(ChasePath, b.bucketId);
                    if (chase != null)
                        b.tasks.Add(chase);
                    break;
                case "start_travel":
                    b.activities = BlurbActivity.Travelling;
                    break;
                case "start_explore":
                    b.activities = BlurbActivity.Exploring;
                    break;
                case "start_search":
                    b.activities = BlurbActivity.Searching;
                    break;
                case "low_vitality":
                    b.priority = 50;
                    b.chance = 0.6f;
                    b.vitalityBelow = 0.3f;
                    break;
                case "ring_carried":
                    b.priority = 40;
                    var ring = LoadForRule<ResourceDefinition>(RingPath, b.bucketId);
                    if (ring != null)
                        b.needsHeld.Add(new ResourceAmount { resource = ring, amount = 1 });
                    else
                        b.enabled = false; // no ring to listen for (warned above): set Needs Held on the asset
                    break;
                case "reaching":
                    // Early and often in loops 2–3, mostly gone by loop 4, silent from 5. The document
                    // also says to cut it once the mirror gem is crafted: Fix Known Blurb Rules adds The Gem Is Made.
                    b.priority = 30;
                    b.chance = 0.5f;
                    b.fromLoop = 2;
                    b.taperFromLoop = 3;
                    b.silentFromLoop = 5;
                    break;
                case "searching":
                    b.priority = 25;
                    b.activities = BlurbActivity.Searching;
                    break;
                case "exploring":
                    b.priority = 25;
                    b.activities = BlurbActivity.Exploring;
                    break;
                case "ring_present":
                    b.priority = 25;
                    b.enabled = false; // needs the ring's room (set Rooms on the asset)
                    break;
                case "lab_sealed":
                    b.priority = 25;
                    b.enabled = false; // needs the lab mirror's room
                    break;
                case "hall_dark":
                    b.priority = 20;
                    var dark = LoadForRule<NodeDefinition>(DarkRoomPath, b.bucketId);
                    if (dark != null)
                        b.rooms.Add(dark);
                    break;
                case "hall_corridor":
                    // The corridors, from loop 2. It also speaks while exploring until there's an
                    // "exploring" bucket (the document lists traverse and search).
                    b.priority = 20;
                    b.roomKinds.Add(NodeKind.Hall);
                    b.fromLoop = 2;
                    b.activities = BlurbActivity.Travelling | BlurbActivity.Searching | BlurbActivity.Exploring;
                    break;
                case "ring_refused":
                    b.priority = 15;
                    b.chance = 0.3f;
                    b.enabled = false; // placeholder: when does she refuse the ring? Set its rules on the asset
                    break;
                case "start_gather_wisp":
                case "gathering_wisps":
                    b.priority = b.moment == BlurbMoment.Starting ? 25 : 24;
                    b.topics.Add(BlurbTopic.Gathering);
                    break;
                case "start_instantiate":
                case "instantiating":
                    b.priority = b.moment == BlurbMoment.Starting ? 25 : 24;
                    b.topics.Add(BlurbTopic.Instantiating);
                    break;
                case "candlelight":
                    b.priority = 22;
                    b.chance = 0.5f;
                    AddHeld(b, "Candles lit");
                    break;
                case "satchel":
                    b.priority = 21;
                    b.chance = 0.3f;
                    AddHeld(b, "Satchel");
                    break;
                case "hall_shifted":
                    // Searching in the runs after the hall first shifts; fades once it's familiar.
                    b.priority = 23;
                    b.chance = 0.5f;
                    b.activities = BlurbActivity.Exploring | BlurbActivity.Searching;
                    b.fromLoop = 3;
                    b.taperFromLoop = 5;
                    b.silentFromLoop = 8;
                    break;
                default:
                    Debug.LogWarning($"Import Blurbs: {b.bucketId} is new and has no starting rules, so it can speak anywhere. " +
                                     "Set its rules on the asset.");
                    break;
            }
        }

        /// <summary>
        /// Logs a bucket whose task/room restrictions match nothing in the live content, e.g. a
        /// topics entry for a topic no task has, or a tasks/rooms reference to an asset that isn't
        /// in GameContent. Doesn't check needsFlipped/needsHeld etc: those can legitimately wait for
        /// content that's still being built.
        /// </summary>
        private static void WarnIfCanNeverFire(BlurbBucket bucket, GameContent content)
        {
            var allTasks = new List<TaskDefinition>(content.tasks);
            foreach (var verb in new[] { content.travelVerb, content.exploreVerb, content.pickUpVerb, content.putDownVerb })
                if (verb != null)
                    allTasks.Add(verb);

            if (bucket.tasks.Count > 0 && !bucket.tasks.Exists(t => t != null && allTasks.Contains(t)))
            {
                Debug.LogWarning($"Import Blurbs: {bucket.bucketId} can never fire: none of its Tasks are in GameContent.", bucket);
                return;
            }
            if (bucket.topics.Count > 0 && !allTasks.Exists(t => t != null && bucket.topics.Contains(t.blurbTopic)))
            {
                Debug.LogWarning($"Import Blurbs: {bucket.bucketId} can never fire: no task in GameContent has one of its Topics.", bucket);
                return;
            }
            if (bucket.rooms.Count > 0 && !bucket.rooms.Exists(r => r != null && content.nodes.Contains(r)))
            {
                Debug.LogWarning($"Import Blurbs: {bucket.bucketId} can never fire: none of its Rooms are in GameContent.", bucket);
                return;
            }
            if (bucket.roomKinds.Count > 0 && !content.nodes.Exists(n => n != null && bucket.roomKinds.Contains(n.kind)))
                Debug.LogWarning($"Import Blurbs: {bucket.bucketId} can never fire: no room in GameContent has one of its Room Kinds.", bucket);
        }

        /// <summary>A starting rule's content, found by its path: a miss is warned about, since the rule is then left unset.</summary>
        private static T LoadForRule<T>(string path, string forWhat) where T : Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
                Debug.LogWarning($"Import Blurbs: {path} isn't there, so the rule for {forWhat} that points at it is left for the Inspector.");
            return asset;
        }

        private static void AddHeld(BlurbBucket b, string itemName)
        {
            var item = EditorUiFactory.FindResource(itemName);
            if (item != null)
                b.needsHeld.Add(new ResourceAmount { resource = item, amount = 1 });
            else
            {
                Debug.LogWarning($"Import Blurbs: {b.bucketId} listens for an item called \"{itemName}\" (found by display name), but none exists. " +
                                 "The bucket is switched off: set Needs Held on the asset.", b);
                b.enabled = false;
            } // nothing to listen for: set Needs Held on the asset
        }
    }
}
