using System.Collections.Generic;
using System.Text;
using HallOfEchoingMirrors.Core;
using UnityEditor;
using UnityEngine;

namespace HallOfEchoingMirrors.EditorTools
{
    // The Balance Sheet's blurbs (a pure move out of BalanceSheetWindow.cs).
    public partial class BalanceSheetWindow
    {
        // ---------- Blurbs ----------

        /// <summary>The story feed's timing, then a row per bucket for its picking and taper rules. Lines come from Import Blurbs.</summary>
        private void DrawBlurbs()
        {
            if (!Section(ref _showBlurbs, "Blurbs (story feed timing and bucket rules; lines are edited via Import Blurbs)") ||
                _content == null || _content.blurbs == null)
                return;

            var library = _content.blurbs;
            var librarySo = new SerializedObject(library);
            librarySo.Update();
            EditorGUILayout.BeginHorizontal();
            Label("Timer (s)", 70, "How often the timer offers a blurb, min to max, in real seconds (not game time).");
            Field(librarySo, "minSeconds", 50);
            Field(librarySo, "maxSeconds", 50);
            Label("Min real s apart", 110);
            Field(librarySo, "minRealSecondsApart", 50);
            Label("Default cooldown (real s)", 150);
            Field(librarySo, "defaultCooldownSeconds", 50);
            EditorGUILayout.EndHorizontal();
            librarySo.ApplyModifiedProperties();

            Header(("Bucket", 150), ("Priority", 55), ("Chance", 55), ("Cooldown, real s (0 = default)", 140),
                ("Taper from", 70), ("Silent from", 70), ("From loop", 70), ("To loop (0 = no end)", 120), ("Vitality below", 90), ("", 50));
            foreach (var bucket in library.buckets)
            {
                if (bucket == null)
                    continue;
                var so = new SerializedObject(bucket);
                so.Update();
                EditorGUILayout.BeginHorizontal();
                Label(bucket.bucketId, 150);
                Field(so, "priority", 55);
                Field(so, "chance", 55);
                Field(so, "cooldownSeconds", 140);
                Field(so, "taperFromLoop", 70);
                Field(so, "silentFromLoop", 70);
                Field(so, "fromLoop", 70);
                Field(so, "toLoop", 120);
                Field(so, "vitalityBelow", 90);
                SelectButton(bucket);
                EditorGUILayout.EndHorizontal();
                so.ApplyModifiedProperties();
            }
        }
    }
}
