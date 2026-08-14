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
        }
    }

    // The ending entry pairs its story with a content-less placeholder level that is never
    // loaded (boink is hidden here), so LevelLoader never gets to start its music - it has to
    // be started from here instead.
    private void PlayEndingMusic()
    {
        LevelData endingLevel = currentStory?.level;
        if (endingLevel == null || endingLevel.loop == null)
        {
            // No ending track for this scenario yet - keep whatever is already playing rather
            // than cutting the finale to silence.
            return;
        }

        if (endingLevel.intro != null)
        {
            coreReferences.musicSwitcher.SwitchToSequence(endingLevel.intro, endingLevel.loop);
        }
        else
        {
            coreReferences.musicSwitcher.SwitchAudio(endingLevel.loop);
        }
    }

    public void Provide(CoreReferences coreReferences)
    {
        this.coreReferences = coreReferences;
    }
}
