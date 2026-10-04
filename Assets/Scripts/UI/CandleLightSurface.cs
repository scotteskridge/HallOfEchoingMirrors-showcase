using UnityEngine;
using UnityEngine.UI;

namespace HallOfEchoingMirrors.UI
{
    /// <summary>
    /// The Image the planning screen's candle light is drawn on (shader "Candle Light"). Each candle's
    /// CandleFlicker hands it what its light looks like this frame; it passes them all on to the
    /// shader together. A UI Image can't take a material property block, so it works on a copy of the
    /// material made at run time: the material asset on disk is never changed. Visual only.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class CandleLightSurface : MonoBehaviour
    {
        /// <summary>The most candles the shader draws (MAX_CANDLES in CandleLight.shader).</summary>
        public const int MaxCandles = 4;

        private static readonly int CandleId = Shader.PropertyToID("_Candle");
        private static readonly int ColourId = Shader.PropertyToID("_CandleColour");
        private static readonly int CountId = Shader.PropertyToID("_CandleCount");
        private static readonly int AspectId = Shader.PropertyToID("_Aspect");
        private static readonly int DarknessId = Shader.PropertyToID("_Darkness");

        private readonly Vector4[] _candles = new Vector4[MaxCandles];
        private readonly Vector4[] _colours = new Vector4[MaxCandles];
        private readonly float[] _darkness = new float[MaxCandles];
        private readonly bool[] _lit = new bool[MaxCandles];

        private Image _image;
        private RectTransform _rect;
        private Material _runtime;

        public RectTransform Rect => _rect != null ? _rect : (_rect = (RectTransform)transform);

        private void Ensure()
        {
            if (_runtime != null)
                return;
            _image = GetComponent<Image>();
            if (_image.material == null || _image.material == _image.defaultMaterial)
                throw new System.InvalidOperationException($"{name}: give the Image the Candle Light material, or there is no light to draw.");
            _runtime = new Material(_image.material) { name = _image.material.name + " (runtime)" };
            _image.material = _runtime;
        }

        private void OnDestroy()
        {
            if (_runtime != null)
                Destroy(_runtime);
        }

        /// <summary>
        /// One candle's light this frame. <paramref name="uv"/> is where it is across this image (0 to 1),
        /// <paramref name="radius"/> how far it reaches in image heights, <paramref name="darkness"/> how
        /// dark it gets far from the candles.
        /// </summary>
        public void Set(int slot, Vector2 uv, float radius, float brightness, Color colour, float darkness)
        {
            if (slot < 0 || slot >= MaxCandles)
                throw new System.ArgumentOutOfRangeException(nameof(slot), slot, $"{name}: the light holds {MaxCandles} candles.");
            _candles[slot] = new Vector4(uv.x, uv.y, radius, brightness);
            _colours[slot] = colour;
            _darkness[slot] = darkness;
            _lit[slot] = true;
        }

        private void LateUpdate()
        {
            Ensure();
            int count = 0;
            float darkness = 0f;
            int used = 0;
            for (int i = 0; i < MaxCandles; i++)
            {
                if (!_lit[i])
                {
                    _candles[i].w = 0f; // a candle that stopped reporting (switched off) goes out
                    continue;
                }
                count = i + 1;
                darkness += _darkness[i];
                used++;
                _lit[i] = false; // reports again next frame, or goes out
            }
            var rect = Rect.rect;
            _runtime.SetVectorArray(CandleId, _candles);
            _runtime.SetVectorArray(ColourId, _colours);
            _runtime.SetFloat(CountId, count);
            _runtime.SetFloat(AspectId, rect.height > 0f ? rect.width / rect.height : 1f);
            _runtime.SetFloat(DarknessId, used > 0 ? darkness / used : 0f);
        }
    }
}
