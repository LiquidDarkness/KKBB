using System.Globalization;
using TMPro;
using UnityEngine;

// What stands in for a story beat when there is no story: the card between two waves, saying which
// wave is next and what a point is worth on it.
//
// It writes into the very same text a written scenario tells its story in, so the card looks like
// every other beat and needs nothing of its own in the scene. While an endless run is on, the
// TranslationMediator that normally drives that text is switched off - it has no key that could
// say "wave 37", and left running it would write the last scenario's beat back over the card the
// moment the player changed language.
public class EndlessBeatCard : MonoBehaviour
{
    [Header("What it writes into")]
    [Tooltip("The story text itself.")]
    public TMP_Text text;

    [Tooltip("The mediator that drives that text for a written scenario. Switched off for as long as an endless run is being played, and switched back on when it is not.")]
    public TranslationMediator mediator;

    [Header("What it reads")]
    public ScenarioManager scenarioManager;

    public TypeDistinguisher currentLvl;

    [Header("Translation keys")]
    [Tooltip("The line naming the wave. {0} is the wave number.")]
    public string waveKey = "Wave {0}";

    [Tooltip("The line saying what points are worth. {0} is the multiplier. Shown only once it is worth more than it was collected at.")]
    public string multiplierKey = "Points are worth x{0}";

    private void OnEnable()
    {
        StoryManager.OnBeatShown += HandleBeatShown;
        // The card is written in whatever language is on, and a language changed from the options
        // while the card is up has to reach it - the mediator that would normally do that is off.
        TranslationJSONDeserializer.OnTransaltionUpdated += Redraw;

        Apply();
    }

    private void OnDisable()
    {
        StoryManager.OnBeatShown -= HandleBeatShown;
        TranslationJSONDeserializer.OnTransaltionUpdated -= Redraw;

        // Handed back as it was found. Both this and the mediator belong to the Gameplay scene and
        // are built afresh with it, so this cannot leave a story scenario mute.
        if (mediator != null)
        {
            mediator.enabled = true;
        }
    }

    private EndlessScenario Endless =>
        scenarioManager != null ? scenarioManager.CurrentScenarioSettings as EndlessScenario : null;

    private void HandleBeatShown(int _)
    {
        Apply();
    }

    private void Apply()
    {
        bool isEndless = Endless != null;

        if (mediator != null)
        {
            mediator.enabled = !isEndless;
        }

        if (isEndless)
        {
            Redraw();
        }
    }

    private void Redraw()
    {
        EndlessScenario endless = Endless;

        if (endless == null || text == null)
        {
            return;
        }

        int wave = EndlessScenario.WaveOf(currentLvl != null ? currentLvl.IntValue : 0);
        float multiplier = endless.ScoreMultiplierForWave(wave);

        string card = string.Format(Translate(waveKey), wave.ToString("N0", CultureInfo.CurrentCulture));

        // Left off entirely on the first wave, where it would only say that nothing has changed
        // yet. From the second on it is the reason to keep going.
        if (multiplier > 1f)
        {
            card += "\n" + string.Format(Translate(multiplierKey), multiplier.ToString("0.##", CultureInfo.CurrentCulture));
        }

        text.text = card;
    }

    // The same fallback the mediator and the score summary use: a key with no translation behind it
    // shows as itself, which reads as English and says plainly what is missing from the JSON.
    private static string Translate(string key)
    {
        if (TranslationJSONDeserializer.dataDictionary != null
            && TranslationJSONDeserializer.dataDictionary.TryGetValue(key, out string translated)
            && !string.IsNullOrEmpty(translated))
        {
            return translated;
        }

        return key;
    }
}
