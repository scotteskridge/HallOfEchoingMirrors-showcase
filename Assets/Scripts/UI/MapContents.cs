using System.Collections.Generic;
using HallOfEchoingMirrors.Core;

namespace HallOfEchoingMirrors.UI
{
    /// <summary>
    /// Which rooms and ways the map shows (split out of MapView): gathered from the simulation a few times
    /// a second into a scratch set, compared with what is drawn, and only committed (so MapView lays out
    /// again) when they differ. Also which rooms are next stops and which have a way in. It never holds the
    /// Simulation: MapView passes it in each time, because loading replaces it.
    /// </summary>
    public class MapContents
    {
        public struct Line
        {
            public NodeDefinition A, B;
            public bool BothWays;
            public bool Shut; // found but shut, shown dim (Way.showWhileShut)
        }

        /// <summary>The rooms and ways drawn now, as of the last Commit.</summary>
        public List<NodeDefinition> Nodes { get; } = new List<NodeDefinition>();
        public List<Line> Lines { get; } = new List<Line>();

        private readonly List<NodeDefinition> _scratchNodes = new List<NodeDefinition>();
        private readonly List<Line> _scratchLines = new List<Line>();
        private readonly HashSet<NodeDefinition> _knownSet = new HashSet<NodeDefinition>();
        private readonly HashSet<NodeDefinition> _scratchNodeSet = new HashSet<NodeDefinition>();

        // Trips are scheduled from where the queue ends, so those rooms are the clickable ones; and which
        // rooms any known way leads into.
        private readonly HashSet<NodeDefinition> _nextStops = new HashSet<NodeDefinition>();
        private readonly HashSet<NodeDefinition> _hasWayIn = new HashSet<NodeDefinition>();

        public bool IsNextStop(NodeDefinition node) => _nextStops.Contains(node);
        public bool HasWayIn(NodeDefinition node) => _hasWayIn.Contains(node);

        public void GatherKnownRooms(Simulation sim)
        {
            // In the content's order, so the map doesn't reshuffle as rooms are found.
            _knownSet.Clear();
            foreach (var room in sim.RoomsOnMap())
                _knownSet.Add(room);
            _scratchNodes.Clear();
            _scratchNodeSet.Clear();
            foreach (var node in sim.AllNodes)
                if (node != null && _knownSet.Contains(node))
                {
                    _scratchNodes.Add(node);
                    _scratchNodeSet.Add(node);
                }

            // One line per open way between two known rooms.
            _scratchLines.Clear();
            foreach (var from in _scratchNodes)
                foreach (var way in from.ways)
                {
                    if (way == null || !_scratchNodeSet.Contains(way.to))
                        continue;
                    bool shut = !sim.IsOpen(way, from);
                    if ((!shut || way.showWhileShut) && sim.IsFound(way, from) && !HasLineBetween(from, way.to))
                        _scratchLines.Add(new Line { A = from, B = way.to, BothWays = way.bothWays, Shut = shut });
                }
        }

        private bool HasLineBetween(NodeDefinition a, NodeDefinition b)
        {
            foreach (var line in _scratchLines)
                if ((line.A == a && line.B == b) || (line.A == b && line.B == a))
                    return true;
            return false;
        }

        /// <summary>Whether what was just gathered is what is drawn already.</summary>
        public bool SameAsShown()
        {
            if (_scratchNodes.Count != Nodes.Count || _scratchLines.Count != Lines.Count)
                return false;
            for (int i = 0; i < Nodes.Count; i++)
                if (_scratchNodes[i] != Nodes[i])
                    return false;
            for (int i = 0; i < Lines.Count; i++)
                if (_scratchLines[i].A != Lines[i].A || _scratchLines[i].B != Lines[i].B || _scratchLines[i].BothWays != Lines[i].BothWays || _scratchLines[i].Shut != Lines[i].Shut)
                    return false;
            return true;
        }

        /// <summary>What was gathered becomes what is drawn.</summary>
        public void Commit()
        {
            Nodes.Clear();
            Nodes.AddRange(_scratchNodes);
            Lines.Clear();
            Lines.AddRange(_scratchLines);
        }

        public void FindNextStops(Simulation sim)
        {
            _nextStops.Clear();
            foreach (var room in sim.NextStops())
                _nextStops.Add(room);
            _hasWayIn.Clear();
            _hasWayIn.Add(sim.StartNode);
            foreach (var line in Lines)
            {
                _hasWayIn.Add(line.B);
                if (line.BothWays)
                    _hasWayIn.Add(line.A);
            }
        }
    }
}
