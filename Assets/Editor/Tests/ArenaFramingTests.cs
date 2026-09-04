using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// The arena is wider than a 16:9 picture at the size the camera rests on - by a quarter of a unit,
// which is why nobody noticed until a build went out at 960x600 and cut three units off each side.
// These hold the framing to the arena the scene actually has, rather than to a number typed once.
public class ArenaFramingTests
{
    private const float Resting = 15f;
    private const float HalfWidth = 26.911f;

    [Test]
    public void AWideEnoughWindowIsLeftAlone()
    {
        Assert.AreEqual(Resting, ArenaFraming.SizeFor(Resting, HalfWidth, 2.0f), 0.001f);
        Assert.AreEqual(Resting, ArenaFraming.SizeFor(Resting, HalfWidth, 1.794f), 0.01f);
    }

    [Test]
    public void ANarrowWindowIsOpenedUpUntilTheArenaFits()
    {
        foreach (float aspect in new[] { 1.333f, 1.5f, 1.6f, 1.778f })
        {
            float size = ArenaFraming.SizeFor(Resting, HalfWidth, aspect);
            Assert.GreaterOrEqual(size * aspect, HalfWidth - 0.001f,
                $"at aspect {aspect} the camera would still cut the arena off");
            Assert.GreaterOrEqual(size, Resting, $"at aspect {aspect} the camera was zoomed in, not out");
        }
    }

    [Test]
    public void AnAspectOfNothingIsNotDividedBy()
    {
        Assert.AreEqual(Resting, ArenaFraming.SizeFor(Resting, HalfWidth, 0f), 0.001f);
        Assert.AreEqual(Resting, ArenaFraming.SizeFor(Resting, HalfWidth, -1f), 0.001f);
    }

    [Test]
    public void TheGameplayCameraIsFramedForTheArenaItActuallyHas()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Gameplay.unity", OpenSceneMode.Single);

        float barriers = 0f;
        foreach (Collider2D c in Object.FindObjectsOfType<Collider2D>(true))
        {
            if (c.name.ToLower().Contains("barrier") && c.bounds.size.y > c.bounds.size.x)
            {
                barriers = Mathf.Max(barriers, Mathf.Abs(c.bounds.min.x), Mathf.Abs(c.bounds.max.x));
            }
        }

        ArenaFraming framing = SceneManager.GetActiveScene()
            .GetRootGameObjects()
            .SelectMany(r => r.GetComponentsInChildren<ArenaFraming>(true))
            .FirstOrDefault();

        Assert.NotNull(framing, "the gameplay camera has no ArenaFraming on it");
        Assert.Greater(barriers, 0f, "no side barriers found to measure against");
        Assert.AreEqual(barriers, framing.arenaHalfWidth, 0.01f,
            "the framing is set to a different arena than the one in the scene");
    }
}
