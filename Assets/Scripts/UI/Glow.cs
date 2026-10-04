using UnityEngine;
using UnityEngine.UI;

namespace HallOfEchoingMirrors.UI
{
    /// <summary>
    /// A brief highlight on something that just changed: it swells a little and (optionally) warms
    /// to the glow colour, then settles back. Added to an element the first time it glows, so
    /// nothing needs setting up in the editor: call Glow.Flash(label).
    /// </summary>
    public class Glow : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float _seconds = 0.9f;
        [SerializeField, Range(0f, 0.5f)] private float _swell = 0.12f;

        private Graphic _target;
        private bool _tint;
        private Color _baseColour;
        private Vector3 _baseScale;
        private float _elapsed = -1f; // below zero: not glowing

        /// <summary>Glow this element. Tint off for elements whose colour something else sets every frame.</summary>
        public static void Flash(Graphic target, bool tint = true)
        {
            if (target == null)
                return;
            var glow = target.GetComponent<Glow>();
            if (glow == null)
                glow = target.gameObject.AddComponent<Glow>();
            glow.Begin(target, tint);
        }

        private void Begin(Graphic target, bool tint)
        {
            // Mid-glow: start again from the top, but remember the true resting look.
            if (_elapsed < 0f)
            {
                _baseColour = target.color;
                _baseScale = transform.localScale;
            }
            _target = target;
            _tint = tint;
            _elapsed = 0f;
        }

        private void Update()
        {
            if (_elapsed < 0f)
                return;

            _elapsed += Time.unscaledDeltaTime;
            float strength = 1f - Mathf.Clamp01(_elapsed / _seconds);
            strength *= strength; // quick to flare, slow to settle

            transform.localScale = _baseScale * (1f + _swell * strength);
            if (_tint)
                _target.color = Color.Lerp(_baseColour, UiStyle.GlowColour, strength);

            if (_elapsed >= _seconds)
            {
                transform.localScale = _baseScale;
                if (_tint)
                    _target.color = _baseColour;
                _elapsed = -1f;
            }
        }

        private void OnDisable()
        {
            // Hidden mid-glow (e.g. the page slid away): don't leave it swollen.
            if (_elapsed >= 0f)
            {
                transform.localScale = _baseScale;
                if (_tint && _target != null)
                    _target.color = _baseColour;
                _elapsed = -1f;
            }
        }
    }
}
