using System;
using TMPro;
using UnityEngine;

public class StoryManager : MonoBehaviour, ICoreReferencer
{
    [Header("UI")]
    public SpriteRenderer background;
    public Sprite defaultBackground;
    public GameObject textContainer;

    [Header("Variables")]
    public TranslationMediator storyText;
    public StoryContainer storyContainer;
    public TranslationJSONDeserializer jSONDeserializer;
    public TypeDistinguisher currentLvl;
    public TypeDistinguisher chosenScenario;
    public Story currentStory;
    internal static bool isStoryActive;

    // The farewell entry is the end of the scenario - it is the one story beat with no level to
    // play, so reaching it is the only reliable "the player finished this one". Static, like every
    // other signal here, and this component belongs to the Gameplay scene: subscribers must drop
    // it again or a destroyed one throws and stops the delegates queued behind it.
    public static event Action OnScenarioFinished;

    // Raised whenever a beat is put on screen, carrying the beat number. It is what endless is
    // built on: the wave card, the speed ramp and the lives handed back all hang off it, and for a
    // scenario with no story to progress through there is no other moment that means "a new wave
    // starts here". Static like the rest of the signals in this class, and with the same duty on
    // whoever listens - drop it in OnDisable, or a destroyed subscriber throws and takes the
    // delegates queued behind it down with it.
    public static event Action<int> OnBeatShown;

    [Header("Blocks")]
    public GameObject ending;
    public GameObject boink;

    [Header("Managers")]
    public MainManager mainManager;
    public ScenarioManager scenarioManager;
    public CoreReferences coreReferences;

    public void Awake()
    {
        MainManager.OnStoryLoaded += DisplayStoryContent;

        ShowEndingButton(false);
        Level.OnLevelCompleted += Progress;
        if (!boink.activeInHierarchy)
        {
            boink.SetActive(true);
        }
    }

    // Both events are static while this component belongs to the Gameplay scene, so
    // without this every return to the menu left a destroyed StoryManager subscribed.
    // The stale handler throws the moment it touches anything of its dead object, and an
    // exception part-way through a multicast invocation stops the delegates queued behind
    // it - so the live StoryManager would never get told the level was finished.
    public void OnDestroy()
    {
        MainManager.OnStoryLoaded -= DisplayStoryContent;
        Level.OnLevelCompleted -= Progress;
    }

    // Story shortcuts for working on the game rather than playing it: G skips the beat on
    // screen, E jumps straight into the level. Behind a define because this reads the bare
    // keyboard every frame, with nothing to say it is meant for development - in a shipped
    // build a player who rested a hand on G walked out of the story.
#if AUTOPLAY
    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.G))
        {
            Progress();
        }        
        if (Input.GetKeyDown(KeyCode.E))
        {
            ProgressToLevel();
        }
    }
#endif

    // Whether the beat on screen is the farewell one: the last entry, the one paired with a
    // placeholder level that is never played. Asked of the beat itself rather than of the step
    // that got us here, because a scenario can be walked straight into on it - Continue from the
    // main menu lands there - and the ending has to be on screen without anyone having pressed on.
    // A scenario can also have no farewell at all - endless is exactly that - and then there is no
    // last entry to land on, however many beats have been played.
    private bool IsOnEndingBeat => mainManager.HasFarewellBeat && mainManager.BeatCount > 0 && currentLvl.IntValue == mainManager.BeatCount - 1;

    [ContextMenu("test progress")]
    public void Progress()
    {
        Debug.Log("Storymanager progress from level: " + currentLvl.IntValue);

        int beatCount = mainManager.BeatCount;

        if (beatCount <= 0)
        {
            // Modulo by zero throws, and a scenario with no beats in it is a wiring mistake worth
            // hearing about rather than an exception in the middle of finishing a level.
            Debug.LogError($"[{nameof(StoryManager)}] {mainManager.chosenScenario.name} has no beats to progress through.");
            return;
        }

        int currentLevelNumber = currentLvl.IntValue + 1;
        PlayerPrefs.SetInt(currentLvl.PrefsKey, currentLevelNumber % beatCount);
        DisplayStoryContent();

        Debug.Log("Storymanager progress to level: " + currentLvl.IntValue);
    }

    [ContextMenu("test story")]
    public void DisplayStoryContent()
    {
        Debug.Log("Story displayed for level: " + currentLvl.IntValue);
        currentStory = mainManager.CurrentStory;
        isStoryActive = true;
        if (textContainer != null)
        {
            textContainer.SetActive(true);
        }

        // A scenario whose beats carry no text of their own puts up its own card instead: there is
        // nothing the translation files could hold about wave 37, so endless writes that one itself.
        if (mainManager.chosenScenario.HasBeatText)
        {
            storyText.key = mainManager.chosenScenario.name + currentLvl.IntValue;
            //LiquidDarkness1
            storyText.UpdateTranslation();
        }

        // Reaching the farewell beat by any road ends the scenario, and that includes walking
        // straight into it: Continue from the main menu lands on that beat without ever going
        // through Progress, which used to leave the block that carries on standing there - and
        // boinking it took the player into the placeholder level behind the ending, which has no
        // content to play. Settled before the music, since ShowEndingButton asks for the finale
        // track itself when the answer is yes.
        ShowEndingButton(IsOnEndingBeat);

        // The story beat is shown before its level is loaded, so leaving the music to
        // LevelLoader meant a scenario opened on silence and its track only started once
        // the player had read the text and pressed on. The ending is no exception: its
        // placeholder level carries the finale track and the call above asks for that very
        // same one, which MusicSwitcher recognises and does not restart.
        PlayMusicFor(currentStory?.level);

        // Last of all, so that everyone listening finds the beat settled: the ending decided, the
        // music asked for and currentStory holding the beat that is now on screen.
        OnBeatShown?.Invoke(currentLvl.IntValue);
    }

    public void HideStoryText()
    {
        textContainer.SetActive(false);
        isStoryActive = false;
    }

    public void ProgressToLevel()
    {
        coreReferences.loadingScreen.FadeToBlack(() =>
        {
            //Dzia³a jak event Action, ale nie ma potrzeby subskrybowania siê i odsubrybowania,
            //wydarzy siê jednorazowo, ale bêdzie dzia³a³o za ka¿dym wywo³anie LoadLevel z odpowiedni¹ zawartoœci¹.
            HideStoryText();
            mainManager.LoadLevel(currentStory.level);

            coreReferences.loadingScreen.FadeToClear();
        });
    }

    public void ShowEndingButton(bool shouldShow)
    {
        ending.SetActive(shouldShow);
        boink.SetActive(!shouldShow);

        if (shouldShow)
        {
            PlayEndingMusic();
            OnScenarioFinished?.Invoke();
        }
    }

    // The ending entry pairs its story with a content-less placeholder level that is never
    // loaded (boink is hidden here), so LevelLoader never gets to start its music - it has to
    // be started from here instead.
    private void PlayEndingMusic()
    {
        PlayMusicFor(currentStory?.level);
    }

    // Deliberately silent about levels with no track: keeps whatever is already playing
    // rather than cutting to nothing, which still matters for the scenarios whose ending
    // has no music of its own yet.
    private void PlayMusicFor(LevelData levelData)
    {
        // The switcher is injected at runtime and is missing in the editor outside play mode. It
        // is checked here rather than left to throw because this is called from inside
        // ShowEndingButton, one line ahead of the signal that a scenario has been finished - and
        // an exception there would take the achievements and the score summary down with it.
        if (levelData == null || levelData.loop == null || coreReferences == null || coreReferences.musicSwitcher == null)
        {
            return;
        }

        if (levelData.intro != null)
        {
            coreReferences.musicSwitcher.SwitchToSequence(levelData.intro, levelData.loop);
        }
        else
        {
            coreReferences.musicSwitcher.SwitchAudio(levelData.loop);
        }
    }

    public void Provide(CoreReferences coreReferences)
    {
        this.coreReferences = coreReferences;
    }
}
