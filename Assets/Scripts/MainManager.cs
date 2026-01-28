using System;
using System.Collections.Generic;
using UnityEngine;

public class MainManager : MonoBehaviour
{
    //TODO: think of a new, more fitting name for the class. ContentManager? Dependency Injection > Singleton
    //This class should know which scenario has been chosen and which level should be loaded now.
    //It should delegate loading stories and levels when it is their time to be loaded to the correct classes.
    //LevelLoader - loads levels (coroutine for random blocks of the level's content appearing),
    //StoryLoader - loads story (coroutine for story root slowly showing up, along with the text),
    //MusicSwitcher - fades music in and out for whatever is being shown to the player,
    //Works for narration in the story pieces, as well, once I have them.
    //SceneLoader - loads scenes between Gameplay and MainMenu,
    //LoadingScreen - fades out and in from scene to scene,
    //[![](https://mermaid.ink/img/pako:eNp9kEFvwjAMhf9KZAlpk-i497DLOMKF7ATZwUrcNlrrIDcZQoj_vhTKRAVaDpHfe59lJyewwRGUULXhYBuUqD6XhlU-a_S8RsaaZLeiH2pHsbgLvq7obLYK6DzX2goR77QlpsHK9CQZ-bv8ajzYqngr3pUNQg_LqGKS6Bjk-LzpsvXzaKhePvK1oYqE2FL_Ok5Kvbf64KNtbtsN8GXqf9nknTCHjqRD7_LXngbSQGyoIwNlLh3KtwHD58xhikEf2UIZJdEcJKS6gbLCts8q7R1GWnqsBbs_d4-8DeGmz7_z45Y0?type=png)](https://mermaid.live/edit#pako:eNp9kEFvwjAMhf9KZAlpk-i497DLOMKF7ATZwUrcNlrrIDcZQoj_vhTKRAVaDpHfe59lJyewwRGUULXhYBuUqD6XhlU-a_S8RsaaZLeiH2pHsbgLvq7obLYK6DzX2goR77QlpsHK9CQZ-bv8ajzYqngr3pUNQg_LqGKS6Bjk-LzpsvXzaKhePvK1oYqE2FL_Ok5Kvbf64KNtbtsN8GXqf9nknTCHjqRD7_LXngbSQGyoIwNlLh3KtwHD58xhikEf2UIZJdEcJKS6gbLCts8q7R1GWnqsBbs_d4-8DeGmz7_z45Y0)

    public StoryContainer chosenScenario;
    public CoreReferences coreReferences;

    public List<LevelData> levels;
    public Transform contentContainer;
    public SpriteRenderer backgroundSprite;

    public LevelLoader levelLoader;
    public ScenarioManager scenarioManager;

    public static int currentLevelIndex;
    public TypeDistinguisher currentLevel;
    public TypeDistinguisher scenarioIndex;
    public static event Action OnLevelLoaded;

    //private StoryManager storyManager;

    public void Awake()
    {
        //chosenScenario = null;
        //chosenScenario = scenarioManager.scenarios[scenarioIndex.IntValue];
        Level.OnLevelCompleted += LoadNextLevel;
        //Debug.Log(scenarioIndex.IntValue);
    }

    public void Start()
    {
        SeekReference();

        TryLoadScenario();

        //LoadLevel(chosenScenario.stories[currentLevel.IntValue].level);
    }

    private void TryLoadScenario()
    {
        levels.Clear();
        chosenScenario = scenarioManager.CurrentScenarioSettings;

        foreach (var item in chosenScenario.stories)
        {
            levels.Add(item.level);
        }

        /*
        if (chosenScenario != scenarioManager.scenarios[scenarioIndex.IntValue])
        {
            chosenScenario = scenarioManager.scenarios[scenarioIndex.IntValue];
        }

        if (chosenScenario != null)
        {
            foreach (var item in chosenScenario.stories)
            {
                levels.Add(item.level);
            }
        }
        else
        {
            chosenScenario = scenarioManager.scenarios[scenarioManager.chosenScenario.IntValue];
            Debug.Log("Chosen scenario is null");
        }

        Debug.Log($"PlayerPrefs scenario: {PlayerPrefs.GetInt(scenarioManager.chosenScenario.PrefsKey)}");
        Debug.Log($"TypeDistinguisher scenario: {scenarioManager.scenarios[scenarioIndex.IntValue]}");
        Debug.Log("Chosen scenario: " + chosenScenario.name);
        Debug.Log($"Reading from key: {scenarioManager.chosenScenario.PrefsKey}");
        */
        scenarioManager.chosenScenario.LogValue();
    }

    public void SeekReference()
    {
        // Pobieramy wszystkie komponenty typu MonoBehaviour z DontDestroyOnLoaded
        foreach (var item in DontDestroyOnLoaded.GetAll<MonoBehaviour>())
        {
            TryProvideForReferencer(item);
        }

        // Subskrybujemy siê na przysz³e promocje
        DontDestroyOnLoaded.OnPromoted += TryProvideForReferencer;

        // Lokalna metoda do podawania referencji coreReferences
        void TryProvideForReferencer(MonoBehaviour loadedObject)
        {
            if (loadedObject is ICoreReferencer referencer)
            {
                referencer.Provide(coreReferences);
            }
        }
    }

    //[ContextMenu("test loading")]
    //public void TestLoading()
    //{
    //    levelLoader.LoadLevel(levels[currentLevelIndex]);
    //}

    //[ContextMenu("Advance")]
    public void LoadNextLevel()
    {
        currentLevelIndex = (currentLevelIndex + 1) % levels.Count;
        if (contentContainer.childCount != 0)
        {
            Destroy(contentContainer.GetChild(0).gameObject);
        }

        LevelData levelData = levels[currentLevelIndex];
        //SaveManager.Save();
        LoadLevel(levelData);
    }

    public void LoadLevel(LevelData levelData)
    {
        backgroundSprite.sprite = levelData.background;
        levelLoader.LoadLevel(levelData);
        //Instantiate(levelData.content, contentContainer);
        //coreferences.musicSwitcher.SwitchAudio(levelData.ambience);
        //TODO: ³adowaæ obiekty z resourców. (scenariusz.nazwa) ale po to mam przecie¿ CustomStoryContainer?
        OnLevelLoaded?.Invoke();
    }
}