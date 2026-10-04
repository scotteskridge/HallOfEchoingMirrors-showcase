using HallOfEchoingMirrors.EditorTools;
using NUnit.Framework;
using UnityEditor.SceneManagement;

namespace HallOfEchoingMirrors.Tests
{
    /// <summary>
    /// What Close Scenes Before Git remembers comes back the same for Reopen. Closing and reopening
    /// real scenes isn't tested here: it would close the scene open in the editor mid-run.
    /// </summary>
    public class SceneGitGuardTests
    {
        [Test]
        public void OpenScenes_ComeBackTheSame_InOrder()
        {
            var setup = new[]
            {
                new SceneSetup { path = "Assets/Scenes/A.unity", isLoaded = true, isActive = false },
                new SceneSetup { path = "Assets/Scenes/B.unity", isLoaded = false, isActive = true },
            };

            var back = SceneGitGuard.Decode(SceneGitGuard.Encode(setup));

            Assert.That(back, Has.Length.EqualTo(2));
            Assert.That(back[0].path, Is.EqualTo("Assets/Scenes/A.unity"));
            Assert.That(back[0].isLoaded, Is.True);
            Assert.That(back[0].isActive, Is.False);
            Assert.That(back[1].path, Is.EqualTo("Assets/Scenes/B.unity"));
            Assert.That(back[1].isLoaded, Is.False);
            Assert.That(back[1].isActive, Is.True);
        }

        [Test]
        public void AnUnsavedScene_IsLeftOut_BecauseItCantBeReopened()
        {
            var setup = new[]
            {
                new SceneSetup { path = "", isLoaded = true, isActive = true },
                new SceneSetup { path = "Assets/Scenes/A.unity", isLoaded = true, isActive = false },
            };

            var back = SceneGitGuard.Decode(SceneGitGuard.Encode(setup));

            Assert.That(back, Has.Length.EqualTo(1));
            Assert.That(back[0].path, Is.EqualTo("Assets/Scenes/A.unity"));
        }

        [Test]
        public void OnlyUnsavedScenes_LeaveNothingToRemember()
        {
            var setup = new[] { new SceneSetup { path = "", isLoaded = true, isActive = true } };

            Assert.That(SceneGitGuard.Encode(setup), Is.Empty);
            Assert.That(SceneGitGuard.Decode(""), Is.Empty);
        }
    }
}
