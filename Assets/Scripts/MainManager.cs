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

    public TypeDistinguisher currentLevel;
    public TypeDistinguisher scenarioIndex;

    // A saved level index belongs to the scenario it was saved in, and switching
    // scenarios does not reset it - only New Game does. Picking a shorter scenario and
    // continuing therefore indexed straight past the end of its story list and threw.
    // Falls back to the opening beat rather than the last one, which is the ending
    // placeholder that is never meant to be played, and writes the correction back so
    // Progress() does not carry on counting from the stale number.
    public Story CurrentStory
    {
        get
        {
            int index = currentLevel.IntValue;
            if (index < 0 || index >= chosenScenario.stories.Length)
            {
                Debug.LogWarning($"[{nameof(MainManager)}] Saved level index {index} is outside {chosenScenario.name} (0-{chosenScenario.stories.Length - 1}). Starting that scenario over.");
                index = 0;
                currentLevel.SetIntValue(index);
            }

            return chosenScenario.stories[index];
        }
    }

    public static event Action OnLevelLoaded;
    public static event Action OnStoryLoaded;

    public void Start()
    {
        SeekReference();

        TryLoadScenario();
    }

    private void TryLoadScenario()
    {
        levels.Clear();
        chosenScenario = scenarioManager.CurrentScenarioSettings;
        Debug.Log($"[{nameof(MainManager)}] chosenScenario: {chosenScenario.name}");
        foreach (var item in chosenScenario.stories)
        {
            levels.Add(item.level);
        }

        scenarioManager.chosenScenario.LogValue();

        OnStoryLoaded?.Invoke();
    }

    public void SeekReference()
    {
        // Pobieramy wszystkie komponenty typu MonoBehaviour z DontDestroyOnLoaded
        foreach (var item in DontDestroyOnLoaded.GetAll<MonoBehaviour>())
        {
            TryProvideForReferencer(item);
        }

        DontDestroyOnLoaded.OnPromoted += TryProvideForReferencer;
    }

    // This was a local function, which left no way to unsubscribe. MainManager lives in
    // the Gameplay scene and dies with it, while OnPromoted is static and went on holding
    // the dead instance in its invocation list - one more stale handler on every visit to
    // the scene, each of them reaching into a destroyed object.
    private void TryProvideForReferencer(MonoBehaviour loadedObject)
    {
        if (loadedObject is ICoreReferencer referencer)
        {
            referencer.Provide(coreReferences);
        }
    }

    private void OnDestroy()
    {
        DontDestroyOnLoaded.OnPromoted -= TryProvideForReferencer;
    }

    public void LoadNextLevel()
    {
        Debug.Log("MainManager progress");

        if (levels.Count == 0)
        {
            // Modulo by zero throws outright, so say what is wrong instead of dying on
            // a scenario that was left without a single story entry.
            Debug.LogError($"[{nameof(MainManager)}] {chosenScenario.name} has no stories - nothing to load.");
            return;
        }

        int currentLevelIndex = (currentLevel.IntValue + 1) % levels.Count;
        if (contentContainer.childCount != 0)
        {
            Destroy(contentContainer.GetChild(0).gameObject);
        }

        LevelData levelData = levels[currentLevelIndex];
        currentLevel.SetIntValue(currentLevelIndex);
        LoadLevel(levelData);
    }

    public void LoadLevel(LevelData levelData)
    {
        backgroundSprite.sprite = levelData.background;
        levelLoader.LoadLevel(levelData);
        OnLevelLoaded?.Invoke();
    }
}