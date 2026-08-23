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

    [ContextMenu("test progress")]
    public void Progress()
    {
        Debug.Log("Storymanager progress from level: " + currentLvl.IntValue);

        int currentLevelNumber = currentLvl.IntValue + 1;
        PlayerPrefs.SetInt(currentLvl.PrefsKey, currentLevelNumber % mainManager.levels.Count);
        DisplayStoryContent();
        ShowEndingButton(currentLevelNumber == mainManager.levels.Count - 1);

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

        storyText.key = mainManager.chosenScenario.name + currentLvl.IntValue;
        //LiquidDarkness1
        storyText.UpdateTranslation();

        // The story beat is shown before its level is loaded, so leaving the music to
        // LevelLoader meant a scenario opened on silence and its track only started once
        // the player had read the text and pressed on. The ending is no exception: its
        // placeholder level carries the finale track and ShowEndingButton then asks for
        // that very same one, which MusicSwitcher recognises and does not restart.
        PlayMusicFor(currentStory?.level);
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
        if (levelData == null || levelData.loop == null || coreReferences == null)
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
