using System.Collections.Generic;
using UnityEngine;

namespace HallOfEchoingMirrors.Core
{
    /// <summary>
    /// Looks content up by its permanent ID, for loading saves. Built by following every reference
    /// out from Game Content (tasks, switches, story, resources), so anything the game can reach is
    /// findable without keeping a separate list.
    /// </summary>
    public class ContentIndex
    {
        private readonly Dictionary<string, ContentAsset> _byId = new Dictionary<string, ContentAsset>();

        public ContentIndex(GameContent content)
        {
            if (content == null)
            {
                // An empty index would make every save look as if all its content were gone.
                Debug.LogError("ContentIndex: no Game Content was given, so saved content can't be found.");
                return;
            }

            Add(content.openingStory);
            AddTask(content.travelVerb);
            AddTask(content.exploreVerb);
            AddTask(content.pickUpVerb);
            AddTask(content.putDownVerb);
            foreach (var task in content.tasks)
                AddTask(task);
            foreach (var @switch in content.switches)
                AddSwitch(@switch);
            foreach (var skill in content.skills)
                Add(skill);
            AddNode(content.startNode);
            foreach (var node in content.PlayableNodes)
                AddNode(node);
        }

        /// <summary>The content with this ID, or null if it no longer exists (or is a different type).</summary>
        public T Find<T>(string id) where T : ContentAsset =>
            id != null && _byId.TryGetValue(id, out var asset) ? asset as T : null;

        private bool Add(ContentAsset asset)
        {
            if (asset == null)
                return false;
            if (_byId.TryGetValue(asset.Id, out var existing))
            {
                if (existing != asset)
                    Debug.LogWarning($"ContentIndex: '{existing.name}' and '{asset.name}' share the Id {asset.Id}; saves can only find the first.");
                return false;
            }
            _byId[asset.Id] = asset;
            // A container reaches the items it holds.
            if (asset is ResourceDefinition resource)
                foreach (var held in resource.holds)
                    Add(held);
            return true;
        }

        private void AddTask(TaskDefinition task)
        {
            if (!Add(task))
                return;
            foreach (var need in task.needs)
                Add(need.resource);
            foreach (var give in task.gives)
                Add(give.resource);
            foreach (var take in task.takes)
                Add(take.resource);
            foreach (var easier in task.easierWith)
                Add(easier?.whileHolding);
            Add(task.skill);
            foreach (var requirement in task.requiresSkills)
                Add(requirement?.skill);
        }

        private void AddNode(NodeDefinition node)
        {
            // A planned room is not part of the game, so a save can't name it, however it is reached (plan ui-053).
            if (node != null && node.planned)
                return;
            if (!Add(node))
                return;
            foreach (var task in node.tasks)
                AddTask(task);
            foreach (var find in node.foundBySearching)
            {
                AddTask(find?.task);
                AddSwitch(find?.afterSwitch);
            }
            foreach (var way in node.ways)
            {
                if (way == null)
                    continue;
                AddNode(way.to);
                Add(way.story);
                foreach (var need in way.needs)
                    Add(need.resource);
            }
            foreach (var give in node.eachExploreGives)
                Add(give.resource);
            Add(node.firstEntry);
        }

        private void AddSwitch(SwitchDefinition @switch)
        {
            if (!Add(@switch))
                return;
            AddTask(@switch.triggerTask);
            foreach (var task in @switch.requiredTasks) AddTask(task);
            foreach (var task in @switch.unlocksTasks) AddTask(task);
            foreach (var task in @switch.locksTasks) AddTask(task);
            Add(@switch.resourceToHold);
            AddNode(@switch.roomToExplore);
            foreach (var room in @switch.reopensSearch) AddNode(room);
            foreach (var way in @switch.opensWays) { AddNode(way?.from); AddNode(way?.to); }
            foreach (var way in @switch.closesWays) { AddNode(way?.from); AddNode(way?.to); }
            Add(@switch.story);
        }
    }
}
