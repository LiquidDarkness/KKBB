using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Builder for the endless mode: the scenario asset it is played out of, the setting that remembers
// which shuffle a run was dealt, and the four places in the prefab and the two scenes that have to
// be told about it.
//
// Written as an editor tool rather than done by hand for the same reason as the setups next to it -
// Unity keeps every fileID and reference straight, and the whole thing can be run again after a
// change without making a second button. Every step is idempotent: it looks for what it would build
// before building it, and the numbers on the endless asset itself are only written when the asset
// is first made, since those are tuning values and re-running this must not undo an afternoon of
// them.
public static class EndlessSetup
{
    private const string ScenarioManagerPath = "Assets/New Scenario Manager.asset";
    private const string CoreReferencesPath = "Assets/Data/CoreReferences.asset";
    private const string CurrentLevelPath = "Assets/Resources/TypeDistinguishers/currentLvl.asset";
    private const string SavedScorePath = "Assets/Resources/TypeDistinguishers/scoreValue.asset";
    private const string SeedPath = "Assets/Resources/TypeDistinguishers/endlessSeed.asset";
    private const string EndlessFolder = "Assets/Stories/Endless";
    private const string EndlessPath = EndlessFolder + "/Endless.asset";

    private const string GameSessionPrefabPath = "Assets/Prefabs/GameSession.prefab";
    private const string GameplayScenePath = "Assets/Scenes/Gameplay.unity";
    private const string MenuScenePath = "Assets/Scenes/Menu.unity";

    private const string ButtonGroupPath = "Canvas/Content/ButtonGroup";
    private const string TemplateButtonName = "NewGame";
    private const string EndlessButtonName = "Endless";

    // The label, which is also its key in the translation files - the same arrangement every other
    // string a player reads is under.
    private const string EndlessButtonLabel = "Endless";

    private const string LogoPath = "Canvas/LogoImage";
    private const string ContentPath = "Canvas/Content";
    private const string EasterEggPath = ContentPath + "/EasterEgg";

    // Larger than the buttons in the group beside it, which are 90.
    private const float EndlessFontSize = 110f;
    private const string StoryWindowPath = "Canvas/Content/StoryWindow";
    private const string DifficultyWindowPath = "Canvas/Content/DifficultyWindow";

    private const string AvatarWindowName = "AvatarWindow";
    private const string AvatarWindowPath = ContentPath + "/" + AvatarWindowName;

    private const string ChosenAvatarPath = "Assets/Resources/TypeDistinguishers/ChosenAvatar.asset";
    private const string MenuAnimationPath = "Assets/Resources/TypeDistinguishers/menuAnimationActive.asset";
    private const string ReduceMotionPath = "Assets/Resources/TypeDistinguishers/reduceMotion.asset";

    [MenuItem("Debug/Endless - build the whole mode", priority = 120)]
    public static void BuildAll()
    {
        BuildAssets();
        WireGameSession();
        WireGameplay();

        // Before the button: the button is what opens the cat picker, and it refuses to be wired
        // to a window that is not there yet.
        BuildAvatarWindow();
        WireMenu();

        AssetDatabase.SaveAssets();
        Debug.Log("[EndlessSetup] Done.");
    }

    // The endless scenario itself, the seed setting behind it, and its place in the list of
    // scenarios - which is what makes every other part of the game able to play it without knowing
    // it is any different.
    [MenuItem("Debug/Endless - build the assets only", priority = 121)]
    public static void BuildAssets()
    {
        TypeDistinguisher seed = AssetDatabase.LoadAssetAtPath<TypeDistinguisher>(SeedPath);

        if (seed == null)
        {
            seed = ScriptableObject.CreateInstance<TypeDistinguisher>();
            seed.prefType = TypeDistinguisher.PlayerPrefType.INT;

            // Progress rather than a setting: it is purged with the rest of a playthrough, and it
            // has no default worth seeding - a run that has never rolled one is a run that has not
            // started.
            seed.purgable = true;
            seed.defaultValue = string.Empty;
            AssetDatabase.CreateAsset(seed, SeedPath);
            Debug.Log("[EndlessSetup] Created " + SeedPath);
        }

        if (!AssetDatabase.IsValidFolder(EndlessFolder))
        {
            AssetDatabase.CreateFolder("Assets/Stories", "Endless");
        }

        EndlessScenario endless = AssetDatabase.LoadAssetAtPath<EndlessScenario>(EndlessPath);
        bool isNew = endless == null;

        if (isNew)
        {
            endless = ScriptableObject.CreateInstance<EndlessScenario>();

            // Open in the demo, unlike a newly added story scenario: there is no story here to give
            // away, and what it deals in a demo build is only ever what the demo may already play.
            // One checkbox on the asset if that is ever the wrong call.
            endless.availableInDemo = true;

            AssetDatabase.CreateAsset(endless, EndlessPath);
            Debug.Log("[EndlessSetup] Created " + EndlessPath);
        }

        // Wiring, always: these are references, and a reference that has gone missing is a bug
        // rather than a decision anybody made.
        endless.runSeed = seed;
        endless.stories = new Story[0];

        ScenarioManager manager = AssetDatabase.LoadAssetAtPath<ScenarioManager>(ScenarioManagerPath);

        if (manager == null)
        {
            Debug.LogError("[EndlessSetup] No scenario manager at " + ScenarioManagerPath);
            return;
        }

        // Everything the game lists as playable is what endless deals from - so a scenario added to
        // the game and this list is in the pool the next time this is run, and nothing has to be
        // remembered twice.
        var sources = new List<StoryContainer>();

        foreach (StoryContainer scenario in manager.scenarios)
        {
            if (scenario != null && scenario != endless)
            {
                sources.Add(scenario);
            }
        }

        endless.levelSources = sources;

        if (!manager.scenarios.Contains(endless))
        {
            // Appended, never inserted: the chosen scenario is saved as an index into this list, and
            // putting endless anywhere but the end would move every scenario a saved game points at.
            manager.scenarios.Add(endless);
            EditorUtility.SetDirty(manager);
            Debug.Log("[EndlessSetup] Added the endless scenario to " + ScenarioManagerPath);
        }

        EditorUtility.SetDirty(seed);
        EditorUtility.SetDirty(endless);
        AssetDatabase.SaveAssets();

        Debug.Log($"[EndlessSetup] Endless deals from {sources.Count} scenarios.");
    }

    // What the pool comes to right now, and the first waves it would deal out of it. For telling a
    // question about the assets apart from a question about the run: if this says nothing is there,
    // it is levelSources or the demo gate; if this is healthy and the game still complains, it is
    // something about that session rather than about what is on disk.
    [MenuItem("Debug/Endless - list what it deals", priority = 126)]
    public static void ListWhatItDeals()
    {
        EndlessScenario endless = AssetDatabase.LoadAssetAtPath<EndlessScenario>(EndlessPath);

        if (endless == null)
        {
            Debug.LogError("[EndlessSetup] No endless scenario at " + EndlessPath);
            return;
        }

        var said = new System.Text.StringBuilder();
        said.Append($"[EndlessSetup] {endless.name}: {endless.Pool.Count} levels from {endless.levelSources.Count} sources, seed {endless.Seed}.");

        int waves = Mathf.Min(12, endless.Pool.Count);

        for (int beat = 0; beat < waves; beat++)
        {
            LevelData level = endless.LevelForBeat(beat);
            said.Append($"\n  wave {EndlessScenario.WaveOf(beat)}: {(level == null ? "<nothing>" : level.name)}");
        }

        Debug.Log(said.ToString(), endless);
    }

    // GameSession outlives every scene, which is where the two pieces that have to keep working
    // across a whole run belong: the pace of the wave, and the run itself.
    [MenuItem("Debug/Endless - wire GameSession", priority = 122)]
    public static void WireGameSession()
    {
        ScenarioManager manager = AssetDatabase.LoadAssetAtPath<ScenarioManager>(ScenarioManagerPath);
        TypeDistinguisher currentLevel = AssetDatabase.LoadAssetAtPath<TypeDistinguisher>(CurrentLevelPath);
        CoreReferences core = AssetDatabase.LoadAssetAtPath<CoreReferences>(CoreReferencesPath);

        GameObject root = PrefabUtility.LoadPrefabContents(GameSessionPrefabPath);

        try
        {
            GameSpeedManager speed = root.GetComponentInChildren<GameSpeedManager>(true);

            if (speed == null)
            {
                Debug.LogError("[EndlessSetup] No GameSpeedManager in " + GameSessionPrefabPath);
                return;
            }

            speed.scenarioManager = manager;
            speed.currentLvl = currentLevel;

            ScenarioScoreSummary summary = root.GetComponentInChildren<ScenarioScoreSummary>(true);

            if (summary == null)
            {
                Debug.LogError("[EndlessSetup] No ScenarioScoreSummary in " + GameSessionPrefabPath);
                return;
            }

            // Next to the summary it reports a finished run to, and on an object that is never
            // destroyed - the run has to survive the scene reloads a long endless game is made of.
            EndlessRunController controller = root.GetComponentInChildren<EndlessRunController>(true);

            if (controller == null)
            {
                controller = summary.gameObject.AddComponent<EndlessRunController>();
                Debug.Log("[EndlessSetup] Added EndlessRunController to " + summary.gameObject.name);
            }

            controller.scenarioManager = manager;
            controller.currentLvl = currentLevel;
            controller.coreReferences = core;

            PrefabUtility.SaveAsPrefabAsset(root, GameSessionPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    // The card between two waves is written into the story text, so it has to be in the scene that
    // holds it.
    [MenuItem("Debug/Endless - wire Gameplay", priority = 123)]
    public static void WireGameplay()
    {
        ScenarioManager manager = AssetDatabase.LoadAssetAtPath<ScenarioManager>(ScenarioManagerPath);
        TypeDistinguisher currentLevel = AssetDatabase.LoadAssetAtPath<TypeDistinguisher>(CurrentLevelPath);

        Scene scene = EditorSceneManager.OpenScene(GameplayScenePath, OpenSceneMode.Single);

        StoryManager story = Object.FindObjectOfType<StoryManager>();

        if (story == null || story.storyText == null)
        {
            Debug.LogError("[EndlessSetup] No StoryManager with a story text in " + GameplayScenePath);
            return;
        }

        GameObject textObject = story.storyText.gameObject;
        EndlessBeatCard card = textObject.GetComponent<EndlessBeatCard>();

        if (card == null)
        {
            card = textObject.AddComponent<EndlessBeatCard>();
            Debug.Log("[EndlessSetup] Added EndlessBeatCard to " + textObject.name);
        }

        card.text = textObject.GetComponent<TMP_Text>();
        card.mediator = story.storyText;
        card.scenarioManager = manager;
        card.currentLvl = currentLevel;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    // The way in: its own button on the main menu, made out of the New Game button beside it so it
    // comes out in the same art and the same font, and leading to the same difficulty screen every
    // written scenario goes through.
    [MenuItem("Debug/Endless - wire the menu button", priority = 124)]
    public static void WireMenu()
    {
        ScenarioManager manager = AssetDatabase.LoadAssetAtPath<ScenarioManager>(ScenarioManagerPath);
        EndlessScenario endless = AssetDatabase.LoadAssetAtPath<EndlessScenario>(EndlessPath);

        if (manager == null || endless == null)
        {
            Debug.LogError("[EndlessSetup] Build the assets before wiring the menu.");
            return;
        }

        Scene scene = EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);

        Transform buttons = Find(ButtonGroupPath);
        Transform template = buttons != null ? buttons.Find(TemplateButtonName) : null;
        Transform avatars = Find(AvatarWindowPath);

        if (buttons == null || template == null)
        {
            Debug.LogError("[EndlessSetup] The menu is not shaped the way this expects - the button group or New Game is missing.");
            return;
        }

        if (avatars == null)
        {
            Debug.LogError("[EndlessSetup] There is no avatar window to open - run the cat picker builder first.");
            return;
        }

        // Looked for by the component that makes it what it is, anywhere in the scene, rather than
        // by name under the button group: the button has already been dragged somewhere else once,
        // and finding it only where it was built would quietly make a second one.
        EndlessRunStarter found = FindInScene<EndlessRunStarter>();
        bool isNew = found == null;
        Transform button = isNew
            ? ((GameObject)Object.Instantiate(template.gameObject, buttons)).transform
            : found.transform;

        button.name = EndlessButtonName;

        if (isNew)
        {
            // Straight under New Game, but only the first time: where it sits afterwards is
            // somebody's decision about how the menu looks, and not this script's business.
            button.SetSiblingIndex(template.GetSiblingIndex() + 1);
        }

        // Over the easter egg art, which is what it would otherwise be hidden behind: everything in
        // Content is drawn over everything in the branches before it, so being on top of one thing
        // in there means living in there too. Straight after the art and before the buttons, so the
        // windows that open over the menu still cover it. Only ever moved into Content - where it
        // sits once it is in there is somebody's decision about how the menu looks.
        Transform easterEgg = Find(EasterEggPath);

        if (easterEgg != null && button.parent != easterEgg.parent)
        {
            ReparentInPlace((RectTransform)button, (RectTransform)easterEgg.parent);
            button.SetSiblingIndex(easterEgg.GetSiblingIndex() + 1);
        }

        TMP_Text label = button.GetComponent<TMP_Text>();

        if (label != null)
        {
            label.text = EndlessButtonLabel;

            // Bigger than the buttons in the group, because it is not in the group and has nothing
            // beside it to be read against. Written by this builder, so change it here rather than
            // in the scene: the box is widened by however much the letters grew, and doing that
            // twice would leave a button with a great deal of nothing to the right of its word.
            if (!Mathf.Approximately(label.fontSize, EndlessFontSize))
            {
                float grew = EndlessFontSize / Mathf.Max(1f, label.fontSize);
                label.fontSize = EndlessFontSize;

                var box = (RectTransform)button;
                box.sizeDelta = new Vector2(box.sizeDelta.x * grew, box.sizeDelta.y);
            }
        }

        // The halo the cat wears in gameplay, put around the word. Not under the animation switches:
        // it does not move, and Reduce motion is about movement.
        if (button.GetComponent<TextGlow>() == null)
        {
            button.gameObject.AddComponent<TextGlow>();
        }

        TranslationMediator translation = button.GetComponent<TranslationMediator>();

        if (translation != null)
        {
            translation.key = EndlessButtonLabel;
        }

        // The button carries what it starts, so there is nothing else in the scene to keep in step
        // with it.
        EndlessRunStarter starter = button.GetComponent<EndlessRunStarter>();

        if (starter == null)
        {
            starter = button.gameObject.AddComponent<EndlessRunStarter>();
        }

        starter.scenarioManager = manager;
        starter.endless = endless;
        starter.currentLvl = AssetDatabase.LoadAssetAtPath<TypeDistinguisher>(CurrentLevelPath);
        starter.savedScore = AssetDatabase.LoadAssetAtPath<TypeDistinguisher>(SavedScorePath);

        // Locked in a demo build unless the endless scenario says otherwise, the same way every
        // scenario button is.
        DemoContentGate gate = button.GetComponent<DemoContentGate>();

        if (gate == null)
        {
            gate = button.gameObject.AddComponent<DemoContentGate>();
        }

        gate.scenario = endless;

        // The letters bounce, so the button is found without anyone having to be told it is there.
        // Under the same Menu animations switch the scrolling background answers to, and under
        // Reduce motion above that, which is what the enabler beside it is for.
        TextBounceEffect bounce = button.GetComponent<TextBounceEffect>();

        if (bounce == null)
        {
            bounce = button.gameObject.AddComponent<TextBounceEffect>();
        }

        ComponentEnabler enabler = FindEnablerFor(button.gameObject, bounce);

        if (enabler == null)
        {
            enabler = button.gameObject.AddComponent<ComponentEnabler>();
        }

        enabler.target = bounce;
        enabler.typeDistinguisher = AssetDatabase.LoadAssetAtPath<TypeDistinguisher>(MenuAnimationPath);
        enabler.overrideOff = AssetDatabase.LoadAssetAtPath<TypeDistinguisher>(ReduceMotionPath);

        // And it arrives from behind the logo rather than simply being there, which is the moment
        // the eye is caught. Under the same two switches as the bounce - one enabler per thing
        // switched, since an enabler drives a single component.
        MenuEntrance entrance = button.GetComponent<MenuEntrance>();

        if (entrance == null)
        {
            entrance = button.gameObject.AddComponent<MenuEntrance>();
        }

        Transform logo = Find(LogoPath);

        if (logo != null)
        {
            entrance.hideBehind = (RectTransform)logo;
        }
        else
        {
            Debug.LogWarning("[EndlessSetup] No logo at " + LogoPath + " - the endless button will slide in from its own place instead of out from behind it.");
        }

        ComponentEnabler entranceEnabler = FindEnablerFor(button.gameObject, entrance);

        if (entranceEnabler == null)
        {
            entranceEnabler = button.gameObject.AddComponent<ComponentEnabler>();
        }

        entranceEnabler.target = entrance;
        entranceEnabler.typeDistinguisher = AssetDatabase.LoadAssetAtPath<TypeDistinguisher>(MenuAnimationPath);
        entranceEnabler.overrideOff = AssetDatabase.LoadAssetAtPath<TypeDistinguisher>(ReduceMotionPath);

        Button click = button.GetComponent<Button>();

        if (click == null)
        {
            Debug.LogError("[EndlessSetup] The New Game button has no Button on it to copy.");
            return;
        }

        // The copied listeners are New Game's own - the click sound is worth keeping, the rest is
        // its business and not ours. Cleared from the back so the indices left do not move.
        for (int i = click.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
        {
            if (!(click.onClick.GetPersistentTarget(i) is AudioSource))
            {
                UnityEventTools.RemovePersistentListener(click.onClick, i);
            }
        }

        SwayTheEasterEgg();

        UnityEventTools.AddVoidPersistentListener(click.onClick, starter.StartNewRun);

        // The cat first, the difficulty after: the cat tiles are what open the difficulty window,
        // exactly as a scenario tile does.
        UnityEventTools.AddBoolPersistentListener(click.onClick, avatars.gameObject.SetActive, true);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log(isNew ? "[EndlessSetup] Built the endless button." : "[EndlessSetup] Refreshed the endless button.");
    }

    // Moving a rect from one parent to another keeps the numbers on it and changes what they are
    // measured from, so the thing jumps. Asking Unity to keep the world position instead would only
    // move the problem: with the canvas set to match on width, its height is whatever the screen's
    // aspect makes it, and in batch mode that is not the shape the menu was laid out in - the button
    // would come back correct for a window nobody has. So the two anchors are worked out at the
    // resolution the menu was authored for, which is the one size that does not move.
    //
    // Written for a rect anchored to a point rather than stretched, which every menu button here is.
    private static void ReparentInPlace(RectTransform child, RectTransform newParent)
    {
        Vector2 canvas = ReferenceResolution(child);
        var oldParent = child.parent as RectTransform;

        Vector2 from = AnchorPoint(oldParent, child.anchorMin, canvas);
        Vector2 to = AnchorPoint(newParent, child.anchorMin, canvas);
        Vector2 anchored = child.anchoredPosition;

        // false: the numbers on the child are left alone and corrected below, rather than being
        // rewritten from a world position measured against the wrong canvas.
        child.SetParent(newParent, false);
        child.anchoredPosition = anchored + from - to;
    }

    private static Vector2 AnchorPoint(RectTransform parent, Vector2 anchor, Vector2 canvas)
    {
        Rect rect = RectInCanvas(parent, canvas);

        return new Vector2(rect.x + anchor.x * rect.width, rect.y + anchor.y * rect.height);
    }

    // Where a rect sits inside its canvas, worked out the way Unity works it out, from the canvas
    // down. Stops at whatever carries the Canvas, which is the rect the screen decides.
    private static Rect RectInCanvas(RectTransform rect, Vector2 canvas)
    {
        if (rect == null || rect.GetComponent<Canvas>() != null || !(rect.parent is RectTransform))
        {
            return new Rect(0f, 0f, canvas.x, canvas.y);
        }

        Rect parent = RectInCanvas((RectTransform)rect.parent, canvas);

        var anchors = new Rect(
            parent.x + rect.anchorMin.x * parent.width,
            parent.y + rect.anchorMin.y * parent.height,
            (rect.anchorMax.x - rect.anchorMin.x) * parent.width,
            (rect.anchorMax.y - rect.anchorMin.y) * parent.height);

        var size = new Vector2(anchors.width + rect.sizeDelta.x, anchors.height + rect.sizeDelta.y);
        Vector2 pivot = anchors.center + rect.anchoredPosition;

        return new Rect(pivot.x - rect.pivot.x * size.x, pivot.y - rect.pivot.y * size.y, size.x, size.y);
    }

    // The size the menu was drawn against, which is what the canvas scaler was given.
    private static Vector2 ReferenceResolution(Transform inside)
    {
        var scaler = inside.GetComponentInParent<UnityEngine.UI.CanvasScaler>();

        if (scaler != null && scaler.uiScaleMode == UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize)
        {
            return scaler.referenceResolution;
        }

        Debug.LogWarning("[EndlessSetup] No canvas scaler with a reference resolution above " + inside.name + " - falling back to 1920x1080.");

        return new Vector2(1920f, 1080f);
    }

    // The art beside the button, breathing rather than sitting there - a still picture next to a
    // moving word reads as a picture that has stopped working. Its pivot is on its right edge, so a
    // tilt about it swings the whole thing like something hung there.
    private static void SwayTheEasterEgg()
    {
        Transform easterEgg = Find(EasterEggPath);

        if (easterEgg == null)
        {
            Debug.LogWarning("[EndlessSetup] No easter egg at " + EasterEggPath + " - nothing to set swaying.");
            return;
        }

        IdleSway sway = easterEgg.GetComponent<IdleSway>();

        if (sway == null)
        {
            sway = easterEgg.gameObject.AddComponent<IdleSway>();
        }

        // Slower and wider than the letters of the button, so the two do not look like one thing
        // being animated twice.
        ComponentEnabler enabler = FindEnablerFor(easterEgg.gameObject, sway);

        if (enabler == null)
        {
            enabler = easterEgg.gameObject.AddComponent<ComponentEnabler>();
        }

        enabler.target = sway;
        enabler.typeDistinguisher = AssetDatabase.LoadAssetAtPath<TypeDistinguisher>(MenuAnimationPath);
        enabler.overrideOff = AssetDatabase.LoadAssetAtPath<TypeDistinguisher>(ReduceMotionPath);
    }

    // One cat per tile. The index is what goes into ChosenAvatar, and it is the order the avatars
    // sit in on the player prefab - the cat, her face in the menu and her paws in the game are the
    // same entry, so a cat added there is a line added here.
    private struct Cat
    {
        public int Index;
        public string Face;
        public string Name;
        public string Description;
    }

    private static readonly Cat[] Cats =
    {
        new Cat
        {
            Index = 0,
            Face = "Assets/Sprites/player/kitty.png",
            Name = "Liquid Darkness",
            Description = "The OG Kitty, quick on the bounce. The one every story here starts with.",
        },
        new Cat
        {
            Index = 1,
            Face = "Assets/Sprites/player/Simba.png",
            Name = "Simba",
            Description = "Big boy fluff, nice and soft. The slowest of the four, and the kindest to steer.",
        },
        new Cat
        {
            Index = 2,
            Face = "Assets/Sprites/player/Ziggy.png",
            Name = "Ziggy",
            Description = "A free spirit, quick on her feet. The fastest cat here, by a good margin.",
        },
        new Cat
        {
            Index = 3,
            Face = "Assets/Sprites/player/tutorialDummy.png",
            Name = "Dummy",
            Description = "The cat the tutorial is taught with. Unhurried, and forgiving to play.",
        },
    };

    // A cat picker for the endless run, since there is no story to decide which cat is in it. Built
    // out of the scenario window, so it is the same window in the same art with the same way out of
    // it - only the tiles are cats, and boinking one leads on to the difficulty screen the way a
    // scenario does.
    [MenuItem("Debug/Endless - build the cat picker", priority = 125)]
    public static void BuildAvatarWindow()
    {
        TypeDistinguisher chosenAvatar = AssetDatabase.LoadAssetAtPath<TypeDistinguisher>(ChosenAvatarPath);

        if (chosenAvatar == null)
        {
            Debug.LogError("[EndlessSetup] No " + ChosenAvatarPath + " - there is nothing to write the chosen cat into.");
            return;
        }

        Scene scene = EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);

        Transform content = Find(ContentPath);
        Transform story = Find(StoryWindowPath);
        Transform difficulty = Find(DifficultyWindowPath);

        if (content == null || story == null || difficulty == null)
        {
            Debug.LogError("[EndlessSetup] The menu is not shaped the way this expects - the scenario or difficulty window is missing.");
            return;
        }

        Transform tileTemplate = null;

        foreach (Transform child in story)
        {
            // Any scenario tile will do: what is wanted is the art, the layout and the button, all
            // of which they share. The first one is taken rather than one named, so renaming a
            // scenario cannot break this.
            if (child.GetComponent<UnityEngine.UI.Button>() != null && child.name != "exit")
            {
                tileTemplate = child;
                break;
            }
        }

        if (tileTemplate == null)
        {
            Debug.LogError("[EndlessSetup] The scenario window has no tile to copy.");
            return;
        }

        Transform window = content.Find(AvatarWindowName);
        bool isNew = window == null;

        if (isNew)
        {
            window = ((GameObject)Object.Instantiate(story.gameObject, content)).transform;
            window.name = AvatarWindowName;
            window.SetSiblingIndex(story.GetSiblingIndex() + 1);

            // The copy comes with the scenarios in it. The background, the way out and the layout
            // are what was wanted; the tiles are built below.
            foreach (Transform child in Children(window))
            {
                if (child.GetComponent<UnityEngine.UI.Button>() != null && child.name != "exit")
                {
                    Object.DestroyImmediate(child.gameObject);
                }
            }

            // Off until the endless button opens it, the same as every other window here.
            window.gameObject.SetActive(false);
            Debug.Log("[EndlessSetup] Built " + AvatarWindowName + ".");
        }

        AudioSource click = ClickSound(tileTemplate);

        for (int i = 0; i < Cats.Length; i++)
        {
            BuildCatTile(Cats[i], window, tileTemplate, chosenAvatar, difficulty, click, i);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void BuildCatTile(Cat cat, Transform window, Transform template, TypeDistinguisher chosenAvatar,
        Transform difficulty, AudioSource click, int order)
    {
        string tileName = "Cat" + cat.Index;
        Transform tile = window.Find(tileName);

        if (tile == null)
        {
            tile = ((GameObject)Object.Instantiate(template.gameObject, window)).transform;
            tile.name = tileName;
        }

        // Between the background and the way out, both of which sit outside the layout.
        tile.SetSiblingIndex(order + 1);

        // Whatever the tile was copied from is a scenario, and this is not one: a gate with nothing
        // to point at locks its button in a demo build, which is exactly the right answer for a
        // scenario and the wrong one for a cat.
        DemoContentGate gate = tile.GetComponent<DemoContentGate>();

        if (gate != null)
        {
            Object.DestroyImmediate(gate);
        }

        Sprite face = AssetDatabase.LoadAssetAtPath<Sprite>(cat.Face);
        Transform faceObject = tile.Find("kittyFace");

        if (face != null && faceObject != null)
        {
            faceObject.GetComponent<Image>().sprite = face;
        }
        else
        {
            Debug.LogWarning($"[EndlessSetup] {tileName} has no face to show - looked for {cat.Face}.");
        }

        Label(tile.Find("text"), cat.Name);
        Label(tile.Find("text (1)"), cat.Description);

        UnityEngine.UI.Button button = tile.GetComponent<UnityEngine.UI.Button>();

        if (button == null)
        {
            Debug.LogError("[EndlessSetup] " + tileName + " has no button on it.");
            return;
        }

        while (button.onClick.GetPersistentEventCount() > 0)
        {
            UnityEventTools.RemovePersistentListener(button.onClick, 0);
        }

        if (click != null)
        {
            UnityEventTools.AddVoidPersistentListener(button.onClick, click.Play);
        }

        UnityEventTools.AddIntPersistentListener(button.onClick, chosenAvatar.SetIntValue, cat.Index);
        UnityEventTools.AddBoolPersistentListener(button.onClick, difficulty.gameObject.SetActive, true);
        UnityEventTools.AddBoolPersistentListener(button.onClick, window.gameObject.SetActive, false);
    }

    // Every string a player reads goes through a mediator, keyed by the English of it, with the
    // entry in both translation files. The text is written out as well, so the tile reads right in
    // the editor, where no JSON has been loaded.
    private static void Label(Transform label, string key)
    {
        if (label == null)
        {
            Debug.LogWarning("[EndlessSetup] A cat tile is missing one of its two texts.");
            return;
        }

        TMP_Text text = label.GetComponent<TMP_Text>();

        if (text == null)
        {
            return;
        }

        text.text = key;

        TranslationMediator mediator = label.GetComponent<TranslationMediator>();

        if (mediator == null)
        {
            mediator = label.gameObject.AddComponent<TranslationMediator>();
        }

        mediator.key = key;

        WriteTextOnTranslation(mediator, text);
    }

    // The call every other mediator in the menu makes: hand what came back from the translation
    // files straight to the text's own setter. Written through the serialised object rather than
    // through UnityEventTools, which can only take a method it can turn into a delegate - and a
    // property setter is not one. The shape below is exactly what the mediators already in the
    // scene hold, so a tile built here is indistinguishable from one made by hand.
    private static void WriteTextOnTranslation(TranslationMediator mediator, TMP_Text text)
    {
        var serialised = new SerializedObject(mediator);
        SerializedProperty calls = serialised.FindProperty("onTranslationSet.m_PersistentCalls.m_Calls");

        if (calls == null)
        {
            Debug.LogError("[EndlessSetup] TranslationMediator no longer keeps its listeners where this expects them.");
            return;
        }

        calls.arraySize = 1;
        SerializedProperty call = calls.GetArrayElementAtIndex(0);

        call.FindPropertyRelative("m_Target").objectReferenceValue = text;
        call.FindPropertyRelative("m_TargetAssemblyTypeName").stringValue = "TMPro.TMP_Text, Unity.TextMeshPro";
        call.FindPropertyRelative("m_MethodName").stringValue = "set_text";

        // EventDefined: the string the event was raised with is the string that is passed on, rather
        // than a fixed one stored beside the call.
        call.FindPropertyRelative("m_Mode").enumValueIndex = (int)PersistentListenerMode.EventDefined;
        call.FindPropertyRelative("m_CallState").enumValueIndex = (int)UnityEventCallState.RuntimeOnly;
        call.FindPropertyRelative("m_Arguments.m_ObjectArgumentAssemblyTypeName").stringValue = "UnityEngine.Object, UnityEngine";

        serialised.ApplyModifiedPropertiesWithoutUndo();
    }

    // The click every button in this menu makes, taken off the tile being copied rather than looked
    // up by name.
    private static AudioSource ClickSound(Transform template)
    {
        UnityEngine.UI.Button button = template.GetComponent<UnityEngine.UI.Button>();

        for (int i = 0; button != null && i < button.onClick.GetPersistentEventCount(); i++)
        {
            if (button.onClick.GetPersistentTarget(i) is AudioSource source)
            {
                return source;
            }
        }

        Debug.LogWarning("[EndlessSetup] No click sound found on the tile being copied - the cat tiles will be silent.");
        return null;
    }

    // A copy, so the collection can be walked while its members are being destroyed.
    private static List<Transform> Children(Transform parent)
    {
        var children = new List<Transform>();

        foreach (Transform child in parent)
        {
            children.Add(child);
        }

        return children;
    }

    private static ComponentEnabler FindEnablerFor(GameObject owner, MonoBehaviour target)
    {
        foreach (ComponentEnabler enabler in owner.GetComponents<ComponentEnabler>())
        {
            if (enabler.target == target || enabler.target == null)
            {
                return enabler;
            }
        }

        return null;
    }

    private static T FindInScene<T>() where T : Component
    {
        foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            T found = root.GetComponentInChildren<T>(true);

            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private static Transform Find(string path)
    {
        foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            string[] steps = path.Split('/');

            if (root.name != steps[0])
            {
                continue;
            }

            Transform found = root.transform;

            for (int i = 1; i < steps.Length && found != null; i++)
            {
                found = found.Find(steps[i]);
            }

            if (found != null)
            {
                return found;
            }
        }

        return null;
    }
}
