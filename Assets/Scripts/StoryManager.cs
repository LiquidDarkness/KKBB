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
    public int currentLevelNumber;
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
        ShowEndingButton(false);
        Level.OnLevelCompleted += Progress;
        if (!boink.activeInHierarchy)
        {
            boink.SetActive(true);
        }
    }

    public void Start()
    {
        DisplayStoryContent();
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
        currentLevelNumber = currentLvl.IntValue;
        currentLevelNumber += 1;
        DisplayStoryContent();
        PlayerPrefs.SetInt(currentLvl.PrefsKey, currentLevelNumber);
        boink.SetActive(true);
    }

    [ContextMenu("test story")]
    public void DisplayStoryContent()
    {
        currentStory = mainManager.chosenScenario.stories[currentLevelNumber];
        isStoryActive = true;
        if (textContainer != null)
        {
            textContainer.SetActive(true);
        }

        storyText.key = mainManager.chosenScenario.name + currentLevelNumber;
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
    }

    public void Provide(CoreReferences coreReferences)
    {
        throw new System.NotImplementedException();
    }
}
