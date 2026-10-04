using UnityEngine;
using UnityEngine.UI;

namespace HallOfEchoingMirrors.UI
{
    /// <summary>
    /// One candle on the planning screen: its flame flickers a little, and its warm light (drawn by the
    /// CandleLightSurface over the map) dips, sways and swells with layered noise. Purely visual: it never
    /// reads the simulation, so it runs on the real clock, not the game's ticks. The numbers under
    /// "Candle light" are pushed every frame, so they can be tuned while playing (Play-mode edits are
    /// lost on stopping: note the values and type them in again).
    /// </summary>
    public class CandleFlicker : MonoBehaviour
    {
        [Header("Parts")]
        [Tooltip("Where the light is drawn.")]
        [SerializeField] private CandleLightSurface _surface;
        [Tooltip("Which of the light's candle slots this is (0 to 3). Each candle needs its own.")]
        [SerializeField, Range(0, CandleLightSurface.MaxCandles - 1)] private int _slot;
        [Tooltip("An empty object at the tip of the flame: the light is centred here.")]
        [SerializeField] private RectTransform _flamePoint;
        [Tooltip("The candle's picture: its brightness flickers a little with the flame.")]
        [SerializeField] private Image _candle;

        [Header("Candle light")]
        [Tooltip("How fast the flame wavers (higher is more restless).")]
        [SerializeField, Range(0.1f, 10f)] private float _flickerSpeed = 1.6f;
        [Tooltip("How far the light dips when the flame is low: 0 is steady, 1 goes almost out.")]
        [SerializeField, Range(0f, 1f)] private float _flickerStrength = 0.35f;
        [Tooltip("How far the light reaches, in heights of the map.")]
        [SerializeField, Range(0.05f, 1.5f)] private float _lightRadius = 0.5f;
        [Tooltip("How far the centre of the light wobbles, in heights of the map.")]
        [SerializeField, Range(0f, 0.1f)] private float _sway = 0.02f;
        [Tooltip("The colour of the light.")]
        [SerializeField] private Color _warmColour = new Color(1f, 0.62f, 0.28f);
        [Tooltip("How bright the light is at its brightest.")]
        [SerializeField, Range(0f, 3f)] private float _brightness = 0.55f;
        [Tooltip("How dark the map gets far from the candles: 0 is not at all, 1 is black.")]
        [SerializeField, Range(0f, 1f)] private float _darkening = 0.45f;
        [Tooltip("How much the candle's own picture dims when the flame dips.")]
        [SerializeField, Range(0f, 0.5f)] private float _flameFlicker = 0.12f;

        private float _seed;
        private Color _candleColour = Color.white;

        private void Awake()
        {
            // Each candle drifts on its own stretch of the noise, so two never flicker together.
            _seed = _slot * 37.31f + 11.7f;
            if (_candle != null)
                _candleColour = _candle.color;
        }

        private void Update()
        {
            float t = Time.unscaledTime * _flickerSpeed;
            // Two layers: a slow waver and a faster quiver on top.
            float waver = 0.65f * Mathf.PerlinNoise(_seed, t) + 0.35f * Mathf.PerlinNoise(_seed + 17f, t * 3.1f);
            float flick = Mathf.Lerp(1f - _flickerStrength, 1f, Mathf.Clamp01(waver * 1.4f - 0.2f));

            var sway = new Vector2(
                Mathf.PerlinNoise(_seed + 40f, t * 0.7f) - 0.5f,
                Mathf.PerlinNoise(_seed + 80f, t * 0.7f) - 0.5f) * (2f * _sway);

            var rect = _surface.Rect.rect;
            Vector3 local = _surface.Rect.InverseTransformPoint(_flamePoint.position);
            var uv = new Vector2((local.x - rect.xMin) / Mathf.Max(rect.width, 1f), (local.y - rect.yMin) / Mathf.Max(rect.height, 1f));

            _surface.Set(_slot, uv + sway, _lightRadius * (0.92f + 0.08f * flick), _brightness * flick, _warmColour, _darkening);

            if (_candle != null)
            {
                float dim = 1f - _flameFlicker * (1f - flick) / Mathf.Max(_flickerStrength, 0.01f);
                _candle.color = new Color(_candleColour.r * dim, _candleColour.g * dim, _candleColour.b * dim, _candleColour.a);
            }
        }
    }
}
