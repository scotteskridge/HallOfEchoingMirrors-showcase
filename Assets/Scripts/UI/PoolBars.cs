using System.Text;
using HallOfEchoingMirrors.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HallOfEchoingMirrors.UI
{
    /// <summary>
    /// Clara's gem: one bar for all her pathos pools. The whole bar is the gem full; each pool
    /// fills a slice in its hue's colour, as wide as what it holds. Seven equal pools share it in
    /// sevenths, and one pool half full of the gem fills half the bar. Pools appear as she learns
    /// hues, so nothing here assumes how many there are. Hover a slice for that pool, or the rest
    /// of the bar for the whole gem.
    /// "The gem full" is a placeholder rule in Core (Simulation.GemCapacity).
    /// </summary>
    public class PoolBars : MonoBehaviour
    {
        [SerializeField] private GameController _game;
        [Tooltip("The bar's background, the size of the whole gem. It shows the empty part.")]
        [SerializeField] private RectTransform _track;
        [Tooltip("Copied once per pool: a coloured slice inside the track.")]
        [SerializeField] private Image _sliceTemplate;
        [Tooltip("Optional: the total over the bar, e.g. \"Pathos 80/100\".")]
        [SerializeField] private TMP_Text _label;

        private TemplateList<Image> _slices;
        private readonly StringBuilder _tip = new StringBuilder();
        private readonly System.Collections.Generic.HashSet<Hue> _warnedHues = new System.Collections.Generic.HashSet<Hue>();

        private Simulation Sim => _game.Simulation;

        private void Start()
        {
            if (_track == null || _sliceTemplate == null)
            {
                Debug.LogWarning("Pool Bars needs its Track and Slice Template set (the Gem in the top bar). Switched off.", this);
                enabled = false;
                return;
            }
            _slices = new TemplateList<Image>(_sliceTemplate, (slice, index) => ToolTip.On(slice, () => PoolTip(index)));
            ToolTip.On(_track, GemTip);
        }

        private void Update()
        {
            var sim = Sim;
            if (sim == null)
                return;

            var pools = sim.Loop.Pools;
            // No pools yet (vitality only): no gem to show.
            bool any = pools.Count > 0;
            UiText.SetActive(_track, any);
            UiText.SetActive(_label, any);
            _slices.Show(pools.Count);

            float capacity = sim.GemCapacity, from = 0f, held = 0f;
            for (int i = 0; i < pools.Count; i++)
            {
                var pool = pools[i];
                from += ShowSlice(_slices[i], pool, capacity, from);
                held += pool.Current;
            }

            if (_label != null)
            {
                string text = GameText.Get("main.gem", ("current", UiText.Whole(held)), ("max", UiText.Whole(capacity)));
                UiText.Set(_label, text);
            }
        }

        /// <summary>One coloured slice of the bar; returns how wide it is (0 to 1 of the bar).</summary>
        private float ShowSlice(Image slice, Pool pool, float capacity, float from)
        {
            float width = capacity > 0f ? pool.Current / capacity : 0f;
            var rect = slice.rectTransform;
            rect.anchorMin = new Vector2(from, 0f);
            rect.anchorMax = new Vector2(from + width, 1f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;

            if (!UiStyle.TryGetHueColour(pool.Hue, out var colour))
            {
                if (pool.Hue != Hue.None && _warnedHues.Add(pool.Hue)) // once per hue, not every frame
                    Debug.LogError($"PoolBars: no colour for the hue {pool.Hue}, so its slice keeps its old colour. Add one in UiStyle.", this);
            }
            else if (slice.color != colour)
                slice.color = colour;
            return width;
        }

        private string PoolTip(int index)
        {
            var sim = Sim;
            if (sim == null || index >= sim.Loop.Pools.Count)
                return null;
            var pool = sim.Loop.Pools[index];
            string tip = GameText.Get("tips.pool", ("name", pool.Name),
                ("current", UiText.Number(pool.Current)), ("max", UiText.Whole(pool.Max)));
            return AddCostsNote(sim, tip);
        }

        /// <summary>The whole gem: every pool and what it holds.</summary>
        private string GemTip()
        {
            var sim = Sim;
            if (sim == null)
                return null;
            _tip.Clear();
            _tip.Append(GameText.Get("tips.gem"));
            foreach (var pool in sim.Loop.Pools)
                _tip.Append('\n').Append(GameText.Get("tips.gem_pool", ("name", pool.Name),
                    ("current", UiText.Whole(pool.Current)), ("max", UiText.Whole(pool.Max))));
            return AddCostsNote(sim, _tip.ToString());
        }

        private static string AddCostsNote(Simulation sim, string tip) =>
            sim.Settings.chargeActionCosts
                ? tip
                : tip + "\n" + UiStyle.Aside(GameText.Get("tips.pool_costs_off"));
    }
}
