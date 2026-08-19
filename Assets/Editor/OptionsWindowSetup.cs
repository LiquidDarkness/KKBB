using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;

// Builder for the categorised options window and the settings that go with it. Safe to run twice:
// every step either finds what it made last time or makes it. Written as an editor tool rather than
// done by hand so Unity keeps every fileID and reference straight.
public static class OptionsWindowSetup
{
    private const string OptionsPrefabPath = "Assets/Prefabs/Menu/OptionsBG.prefab";
    private const string GameSessionPrefabPath = "Assets/Prefabs/GameSession.prefab";
    private const string SettingsFolder = "Assets/Resources/TypeDistinguishers/";

    // JosefinSans-SemiBold, the face the option rows are written in. Named by guid because that is
    // what survives a rename, and pinned here rather than read off a row label: two rows are
    // authored in Coiny, a display face that is hard to read at control size.
    private const string BodyFontGuid = "11e651882b2ba1b43b935765fca134c4";

    // Top of the text-size slider. Past this the window stops being a settings screen and starts
    // being three rows of very large text, and this is a reflex arcade game - the trade is not worth
    // it. Every component that reads the setting clamps to the same number.
    private const float MaxTextScale = 1.5f;

    // Short of a full blackout on purpose - past this the background art is simply gone, and the
    // point is to quiet it down, not throw it away.
    private const float MaxBackgroundDim = 0.8f;

    private const float ValueColumnWidth = 110f;
    private const float SliderColumnWidth = 200f;
    private const float DropdownColumnWidth = 210f;
    private const float DropdownClosedHeight = 30f;

    // Ceilings as a share of the row. Together they leave the label roughly half the row at any
    // text size, which is what keeps a long label from being crushed into a column one letter wide.
    private const float ValueMaxFraction = 0.18f;
    private const float SliderMaxFraction = 0.30f;
    private const float DropdownMaxFraction = 0.34f;

    private static readonly string[] PaddlePrefabPaths =
    {
        "Assets/Prefabs/paddle.prefab",
        "Assets/Prefabs/paddle2.prefab",
        "Assets/Prefabs/paddle3.prefab",
        "Assets/Prefabs/PlayerRig.prefab",
    };

    // Which existing row goes to which tab. Anything not listed here is parked in the gameplay tab
    // and reported, so a row added later cannot silently end up visible on every tab.
    private static readonly Dictionary<string, string> RowToGroup = new Dictionary<string, string>
    {
        { "Language", "gameplayGroup" },
        { "Text", "gameplayGroup" },
        { "Narration", "gameplayGroup" },

        { "Audio", "audioGroup" },
        { "Master", "audioGroup" },
        { "Music", "audioGroup" },
        { "SFX", "audioGroup" },

        { "Video", "videoGroup" },
        { "Screen", "videoGroup" },
        { "Resolution", "videoGroup" },
        { "Fullscreen", "videoGroup" },
        { "Animations", "videoGroup" },
        { "Menu animation", "videoGroup" },
        { "Game Over", "videoGroup" },

        { "Story scroller", "accessibilityGroup" },
        { "Scroll", "accessibilityGroup" },
    };

    // Order inside each tab. Headers included, so a header always sits directly above the rows it
    // introduces. Anything not listed keeps its place at the end.
    // Switched off until there is more than one language in the json files. The rows stay in place
    // and keep their wiring; only the row objects are off, which is now the way to hide a row.
    private static readonly HashSet<string> ParkedRows = new HashSet<string> { "Language", "Text", "Narration" };

    private static readonly Dictionary<string, string[]> GroupOrder = new Dictionary<string, string[]>
    {
        { "gameplayGroup", new[] { "Language", "Text", "Narration", "Paddle control", "Ball return", "Ball return delay" } },
        { "audioGroup", new[] { "Audio", "Master", "Music", "SFX" } },
        { "videoGroup", new[] { "Screen", "Resolution", "Fullscreen", "Animations", "Menu animation", "Game Over", "Rainbow effects" } },
        { "accessibilityGroup", new[] { "Text size", "Text size preview", "Background dim", "Story scroller", "Scroll" } },
    };

    private class Readout
    {
        public string Format;
        public float Multiplier;
        public string Suffix;
    }

    // How each slider prints its value. A slider with no entry here still gets a readout, just an
    // unadorned one - better a bare number than none.
    private static readonly Dictionary<string, Readout> SliderReadouts = new Dictionary<string, Readout>
    {
        { "Master", new Readout { Format = "0", Multiplier = 100f, Suffix = "%" } },
        { "Music", new Readout { Format = "0", Multiplier = 100f, Suffix = "%" } },
        { "SFX", new Readout { Format = "0", Multiplier = 100f, Suffix = "%" } },
        { "Scroll", new Readout { Format = "0.00", Multiplier = 1f, Suffix = " lines/s" } },
        { "Ball return delay", new Readout { Format = "0", Multiplier = 1f, Suffix = " s" } },
        { "Text size", new Readout { Format = "0", Multiplier = 100f, Suffix = "%" } },
        { "Background dim", new Readout { Format = "0", Multiplier = 100f, Suffix = "%" } },
    };

    [MenuItem("Debug/Accessibility - build options tabs")]
    public static void RunAll()
    {
        BuildOptionsWindow();
        WireGameSession();
        WirePaddles();
        WireBackgroundDimmers();
        AssetDatabase.SaveAssets();
        Debug.Log("[OptionsWindowSetup] done.");
    }

    private static TypeDistinguisher Setting(string name)
    {
        var asset = AssetDatabase.LoadAssetAtPath<TypeDistinguisher>(SettingsFolder + name + ".asset");

        if (asset == null)
        {
            Debug.LogError("[OptionsWindowSetup] missing setting asset: " + name);
        }

        return asset;
    }

    private static void BuildOptionsWindow()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(OptionsPrefabPath);

        try
        {
            Transform options = Require(root.transform, "Options");

            if (options == null)
            {
                return;
            }

            Transform scrollView = Require(options, "Scroll View");
            Transform categories = Require(options, "categories");

            if (scrollView == null || categories == null)
            {
                return;
            }

            Transform content = Require(scrollView, "Viewport/Content");

            if (content == null)
            {
                return;
            }

            var groups = new Dictionary<string, GameObject>
            {
                { "gameplayGroup", EnsureGroup(content, "gameplayGroup", 0) },
                { "audioGroup", EnsureGroup(content, "audioGroup", 1) },
                { "videoGroup", EnsureGroup(content, "videoGroup", 2) },
                { "accessibilityGroup", EnsureGroup(content, "accessibilityGroup", 3) },
            };

            MoveRowsIntoGroups(content, groups);
            RenameScreenHeader(groups["videoGroup"].transform);

            Transform toggleTemplate = FindRow(groups, "Menu animation");
            Transform sliderTemplate = FindRow(groups, "Scroll");
            Transform dropdownTemplate = FindRow(groups, "Narration");
            Transform headerTemplate = FindRow(groups, "Animations");

            if (toggleTemplate == null || sliderTemplate == null || dropdownTemplate == null || headerTemplate == null)
            {
                Debug.LogError("[OptionsWindowSetup] a template row is missing - no new rows were built.");
            }
            else
            {
                BuildToggleRow(groups, groups["videoGroup"].transform, toggleTemplate, "Rainbow effects", "Rainbow text effects", Setting("rainbowAnimation"));
                BuildToggleRow(groups, groups["gameplayGroup"].transform, toggleTemplate, "Ball return", "Return the ball when a run gets stuck", Setting("ballRecallActive"));
                BuildSliderRow(groups, groups["gameplayGroup"].transform, sliderTemplate, "Ball return delay", "Seconds before the ball is returned", Setting("ballRecallDelay"), 5f, 60f, 20f, true);
                BuildSliderRow(groups, groups["accessibilityGroup"].transform, sliderTemplate, "Text size", "Text size", Setting("uiFontScale"), 0.75f, MaxTextScale, 1f, false);

                // Starts at 0 and means it: an unwritten float key reads as 0, which is exactly "no
                // dim", so this is the one slider that wants a real zero at the bottom of its range.
                BuildSliderRow(groups, groups["accessibilityGroup"].transform, sliderTemplate, "Background dim", "Background dim", Setting("backgroundDim"), 0f, MaxBackgroundDim, 0f, false);

                // Control choice belongs with the rest of how the game plays, not with the reading
                // aids, even though it was added for players who cannot use a mouse comfortably.
                BuildDropdownRow(groups, groups["gameplayGroup"].transform, dropdownTemplate, "Paddle control", "Paddle control", Setting("paddleControlMode"),
                    // Short on purpose. The caption is the dropdown's width less 35px for the
                    // arrow, so at the row font a longer entry is simply cut off mid-word.
                    new List<string> { "Last used", "Keyboard", "Mouse" });

                BuildPreviewBox(groups, groups["accessibilityGroup"].transform, headerTemplate, options);
            }

            RestructureRows(groups);
            ShowRowParts(groups);
            AddSliderReadouts(groups);

            // After the readouts, not before: a readout built in this run has no column width yet,
            // and on a fresh checkout there is no earlier run to have given it one.
            ApplyRowLayouts(groups);
            ScaleControlsWithText(groups);
            StyleDropdowns(groups, options);
            FixLanguageRowKeys(groups);
            AutoSizeTabLabels(categories);
            OrderRows(groups);
            ParkRows(groups);

            WireTabs(options, categories, scrollView, groups);
            AddFocusAndFontScaler(root, options);
            HideMenuButtonInMenuScene(options);

            PrefabUtility.SaveAsPrefabAsset(root, OptionsPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static Transform Require(Transform parent, string path)
    {
        Transform found = parent.Find(path);

        if (found == null)
        {
            Debug.LogError("[OptionsWindowSetup] not found: " + path + " under " + parent.name);
        }

        return found;
    }

    private static Transform FindRow(Dictionary<string, GameObject> groups, string rowName)
    {
        foreach (GameObject group in groups.Values)
        {
            Transform found = group.transform.Find(rowName);

            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    // Each group carries a copy of Content's own layout component, so a group stacks its rows
    // exactly the way Content used to stack them all.
    private static GameObject EnsureGroup(Transform content, string name, int siblingIndex)
    {
        Transform existing = content.Find(name);

        if (existing != null)
        {
            return existing.gameObject;
        }

        var group = new GameObject(name, typeof(RectTransform));
        group.transform.SetParent(content, false);
        group.transform.SetSiblingIndex(siblingIndex);

        var rect = (RectTransform)group.transform;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);

        var source = content.GetComponent<VerticalLayoutGroup>();

        if (source != null)
        {
            var copy = group.AddComponent<VerticalLayoutGroup>();
            EditorUtility.CopySerialized(source, copy);
        }
        else
        {
            group.AddComponent<VerticalLayoutGroup>();
        }

        var fitter = group.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        return group;
    }

    private static void MoveRowsIntoGroups(Transform content, Dictionary<string, GameObject> groups)
    {
        var rows = new List<Transform>();

        foreach (Transform child in content)
        {
            if (!groups.ContainsKey(child.name))
            {
                rows.Add(child);
            }
        }

        foreach (Transform row in rows)
        {
            string groupName;

            if (!RowToGroup.TryGetValue(row.name, out groupName))
            {
                groupName = "gameplayGroup";
                Debug.LogWarning("[OptionsWindowSetup] row not in the tab map, parked in gameplay: " + row.name);
            }

            row.SetParent(groups[groupName].transform, false);
        }
    }

    // "Video" sat above the resolution and fullscreen rows inside a tab already called video. The
    // header names what it introduces instead.
    private static void RenameScreenHeader(Transform videoGroup)
    {
        Transform header = videoGroup.Find("Video");

        if (header == null)
        {
            return;
        }

        header.name = "Screen";
        var label = header.GetComponent<TMP_Text>();

        if (label != null)
        {
            label.text = "Screen";
        }
    }

    private static GameObject CloneRow(Dictionary<string, GameObject> groups, Transform template, Transform parent, string objectName, string translationKey)
    {
        Transform existing = FindRow(groups, objectName);

        if (existing != null)
        {
            // A row that has moved between tabs since the last run follows the new home.
            if (existing.parent != parent)
            {
                existing.SetParent(parent, false);
            }

            return existing.gameObject;
        }

        GameObject copy = Object.Instantiate(template.gameObject, parent);
        copy.name = objectName;

        // The key doubles as the placeholder label: until it is added to EN/PL.json the row says
        // exactly which key it is waiting for. The label is reached through its TranslationMediator
        // rather than by taking the first TMP text in the row - a dropdown row would otherwise hand
        // back its own caption.
        var mediator = copy.GetComponentInChildren<TranslationMediator>(true);

        if (mediator != null)
        {
            mediator.key = translationKey;

            var label = mediator.GetComponent<TMP_Text>();

            if (label != null)
            {
                label.text = translationKey;
            }
        }

        return copy;
    }

    private static void BuildToggleRow(Dictionary<string, GameObject> groups, Transform parent, Transform template, string objectName, string key, TypeDistinguisher setting)
    {
        GameObject row = CloneRow(groups, template, parent, objectName, key);
        var toggle = row.GetComponentInChildren<Toggle>(true);
        var setter = row.GetComponentInChildren<ToggleSetter>(true);

        if (toggle == null || setter == null || setting == null)
        {
            Debug.LogError("[OptionsWindowSetup] could not wire toggle row: " + objectName);
            return;
        }

        setter.typeDistinguisher = setting;
        setter.toggle = toggle;
        RetargetSettingCalls(new SerializedObject(toggle), setting);
    }

    private static void BuildSliderRow(Dictionary<string, GameObject> groups, Transform parent, Transform template, string objectName, string key, TypeDistinguisher setting,
        float min, float max, float startValue, bool wholeNumbers)
    {
        GameObject row = CloneRow(groups, template, parent, objectName, key);
        var slider = row.GetComponentInChildren<Slider>(true);
        var setter = row.GetComponentInChildren<SliderSetter>(true);

        if (slider == null || setter == null || setting == null)
        {
            Debug.LogError("[OptionsWindowSetup] could not wire slider row: " + objectName);
            return;
        }

        setter.typeDistinguisher = setting;
        setter.slider = slider;
        slider.minValue = min;
        slider.maxValue = max;
        slider.wholeNumbers = wholeNumbers;
        slider.SetValueWithoutNotify(startValue);
        RetargetSettingCalls(new SerializedObject(slider), setting);
    }

    private static void BuildDropdownRow(Dictionary<string, GameObject> groups, Transform parent, Transform template, string objectName, string key, TypeDistinguisher setting,
        List<string> entries)
    {
        GameObject row = CloneRow(groups, template, parent, objectName, key);
        var dropdown = row.GetComponentInChildren<TMP_Dropdown>(true);

        if (dropdown == null || setting == null)
        {
            Debug.LogError("[OptionsWindowSetup] could not wire dropdown row: " + objectName);
            return;
        }

        dropdown.ClearOptions();
        dropdown.AddOptions(entries);

        // Whatever the template dropdown used to drive is not this one's business.
        while (dropdown.onValueChanged.GetPersistentEventCount() > 0)
        {
            UnityEventTools.RemovePersistentListener(dropdown.onValueChanged, 0);
        }

        UnityEventTools.AddPersistentListener(dropdown.onValueChanged, setting.SetIntValue);

        var setter = dropdown.GetComponent<DropdownSetter>();

        if (setter == null)
        {
            setter = dropdown.gameObject.AddComponent<DropdownSetter>();
        }

        setter.dropdown = dropdown;
        setter.typeDistinguisher = setting;
    }

    // A framed panel of sample prose that scales with the text size setting, so the player sets the
    // size against something readable instead of guessing from the slider position.
    private static void BuildPreviewBox(Dictionary<string, GameObject> groups, Transform parent, Transform labelTemplate, Transform options)
    {
        const string PreviewName = "Text size preview";
        Transform existing = FindRow(groups, PreviewName);
        GameObject box;

        if (existing != null)
        {
            box = existing.gameObject;
        }
        else
        {
            box = new GameObject(PreviewName, typeof(RectTransform));
            box.transform.SetParent(parent, false);

            GameObject sample = Object.Instantiate(labelTemplate.gameObject, box.transform);
            sample.name = "Sample";
        }

        var frame = box.GetComponent<Image>();

        if (frame == null)
        {
            frame = box.AddComponent<Image>();
        }

        // Borrowed from a control that is already in the window, so the panel is framed in the same
        // style as everything around it rather than a flat rectangle of my choosing.
        var reference = options.GetComponentInChildren<TMP_Dropdown>(true);

        if (reference != null)
        {
            var referenceImage = reference.GetComponent<Image>();

            if (referenceImage != null)
            {
                frame.sprite = referenceImage.sprite;
                frame.type = referenceImage.type;
                frame.color = referenceImage.color;
                frame.pixelsPerUnitMultiplier = referenceImage.pixelsPerUnitMultiplier;
            }
        }

        var layout = box.GetComponent<VerticalLayoutGroup>();

        if (layout == null)
        {
            layout = box.AddComponent<VerticalLayoutGroup>();
        }

        layout.padding = new RectOffset(20, 20, 14, 14);
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        Transform sampleTransform = box.transform.Find("Sample");

        if (sampleTransform == null)
        {
            Debug.LogError("[OptionsWindowSetup] the preview box lost its sample text.");
            return;
        }

        var mediator = sampleTransform.GetComponent<TranslationMediator>();

        if (mediator != null)
        {
            mediator.key = "Text size preview";
        }

        var text = sampleTransform.GetComponent<TMP_Text>();

        if (text != null)
        {
            text.text = "Kitty bounces, blocks break, the story goes on. Set this to a size you can read without leaning in.";
            text.enableWordWrapping = true;
            text.alignment = TextAlignmentOptions.TopLeft;

            // Copied from a row label rather than the header the box was cloned from: the point of
            // the preview is to show ordinary reading text at the chosen size, and a header face is
            // not what the player will be reading.
            Transform sizeRow = FindRow(groups, "Text size");
            var mediatorOnRow = sizeRow != null ? sizeRow.GetComponentInChildren<TranslationMediator>(true) : null;
            var bodyStyle = mediatorOnRow != null ? mediatorOnRow.GetComponent<TMP_Text>() : null;

            if (bodyStyle != null)
            {
                text.font = bodyStyle.font;
                text.fontSize = bodyStyle.fontSize;
                text.color = bodyStyle.color;
            }
        }
    }

    // Only calls that already pointed at a setting are moved to the new one; the click sound wired
    // next to them on the same event stays exactly where it is.
    //
    // Every persistent call list on the object is walked rather than one named event: Toggle
    // serializes its event as "onValueChanged" while Slider and TMP_Dropdown use
    // "m_OnValueChanged", and guessing wrong fails silently - the setter component would point at
    // the new setting while the event kept writing to the template's one.
    private static void RetargetSettingCalls(SerializedObject serialized, TypeDistinguisher setting)
    {
        var callListPaths = new List<string>();
        SerializedProperty walker = serialized.GetIterator();

        while (walker.Next(true))
        {
            if (walker.propertyPath.EndsWith("m_PersistentCalls.m_Calls") && walker.isArray)
            {
                callListPaths.Add(walker.propertyPath);
            }
        }

        if (callListPaths.Count == 0)
        {
            Debug.LogError("[OptionsWindowSetup] no persistent call list on " + serialized.targetObject.GetType().Name);
            return;
        }

        int retargeted = 0;

        foreach (string path in callListPaths)
        {
            SerializedProperty calls = serialized.FindProperty(path);

            for (int i = 0; i < calls.arraySize; i++)
            {
                SerializedProperty target = calls.GetArrayElementAtIndex(i).FindPropertyRelative("m_Target");

                if (target != null && target.objectReferenceValue is TypeDistinguisher)
                {
                    target.objectReferenceValue = setting;
                    retargeted++;
                }
            }
        }

        serialized.ApplyModifiedPropertiesWithoutUndo();

        if (retargeted == 0)
        {
            Debug.LogWarning("[OptionsWindowSetup] nothing pointed at a setting on " + serialized.targetObject.GetType().Name + " - check the template row.");
        }
    }

    // Every row becomes: a wrapper carrying a HorizontalLayoutGroup, the label on the left, the
    // control on the right. Headers - a label with no control beside it - are left as they are.
    private static void RestructureRows(Dictionary<string, GameObject> groups)
    {
        foreach (GameObject group in groups.Values)
        {
            var rows = new List<Transform>();

            foreach (Transform child in group.transform)
            {
                rows.Add(child);
            }

            foreach (Transform row in rows)
            {
                RestructureRow(row, group.transform);
            }
        }
    }

    private static void RestructureRow(Transform row, Transform group)
    {
        Transform wrapper = row;

        if (row.GetComponent<HorizontalLayoutGroup>() == null)
        {
            var control = row.GetComponentInChildren<Selectable>(true);

            if (control == null || control.transform.parent != row)
            {
                return;
            }

            var controlRect = (RectTransform)control.transform;

            // Read the size before reparenting: once the control is inside a layout group its rect
            // is driven, and the authored size is the only record of how big it is meant to be.
            Vector2 controlSize = controlRect.anchorMin == controlRect.anchorMax
                ? controlRect.sizeDelta
                : controlRect.rect.size;

            int siblingIndex = row.GetSiblingIndex();

            var built = new GameObject(row.name, typeof(RectTransform));
            built.transform.SetParent(group, false);
            built.transform.SetSiblingIndex(siblingIndex);
            built.AddComponent<HorizontalLayoutGroup>();

            row.name = "Label";
            row.SetParent(built.transform, false);
            controlRect.SetParent(built.transform, false);

            LayoutElement controlLayout = EnsureLayoutElement(control.gameObject);
            controlLayout.preferredWidth = controlSize.x;
            controlLayout.preferredHeight = controlSize.y;

            wrapper = built.transform;
        }

        ApplyRowLayout(wrapper);
    }

    // Re-applied on every run, not only when the wrapper is first made: this is where the column
    // widths are decided, and getting it wrong is what made every row a different shape.
    private static void ApplyRowLayout(Transform wrapper)
    {
        var layout = wrapper.GetComponent<HorizontalLayoutGroup>();

        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.spacing = 20f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandHeight = false;

        // This one has to stay off. With it on, Unity raises every child's flexible width to at
        // least 1 no matter what its LayoutElement says, so the label and the control split the
        // spare space between them - which is why each row's control came out a different width
        // depending on how long its label happened to be. Translation would have made it worse.
        layout.childForceExpandWidth = false;

        Transform label = wrapper.Find("Label");

        if (label != null)
        {
            LayoutElement labelLayout = EnsureLayoutElement(label.gameObject);
            labelLayout.flexibleWidth = 1f;
            labelLayout.minWidth = 0f;
            labelLayout.preferredWidth = -1f;
        }

        // Everything to the right of the label keeps the width it was drawn at and refuses to
        // stretch. The label eating the slack is what pins them to the right-hand edge, so the
        // controls line up down the tab however long the labels get in another language - and all
        // the sliders, drawn at the same width, come out identical.
        foreach (Transform child in wrapper)
        {
            if (child == label)
            {
                continue;
            }

            LayoutElement childLayout = EnsureLayoutElement(child.gameObject);
            childLayout.flexibleWidth = 0f;

            if (child.name == "Value")
            {
                childLayout.preferredWidth = ValueColumnWidth;
            }

            childLayout.minWidth = childLayout.preferredWidth;
        }
    }

    private static LayoutElement EnsureLayoutElement(GameObject target)
    {
        LayoutElement element = target.GetComponent<LayoutElement>();

        if (element == null)
        {
            element = target.AddComponent<LayoutElement>();
        }

        return element;
    }

    // The number goes between the label and the slider, so every slider still starts and ends at
    // the same place.
    private static void AddSliderReadouts(Dictionary<string, GameObject> groups)
    {
        foreach (GameObject group in groups.Values)
        {
            foreach (Transform row in group.transform)
            {
                var slider = row.GetComponentInChildren<Slider>(true);

                if (slider == null || slider.transform.parent != row)
                {
                    continue;
                }

                AddSliderReadout(row, slider);
            }
        }
    }

    private static void AddSliderReadout(Transform row, Slider slider)
    {
        Transform label = row.Find("Label");
        Transform value = row.Find("Value");

        if (value == null)
        {
            if (label == null)
            {
                Debug.LogWarning("[OptionsWindowSetup] no label to copy the readout style from: " + row.name);
                return;
            }

            // Cloned from the row's own label so it carries the same font, size and colour; the
            // translation component comes off, since a number is not text to translate.
            GameObject copy = Object.Instantiate(label.gameObject, row);
            copy.name = "Value";

            var mediator = copy.GetComponent<TranslationMediator>();

            if (mediator != null)
            {
                Object.DestroyImmediate(mediator);
            }

            value = copy.transform;
        }

        // Only ever moved leftwards. Setting it to the slider's index unconditionally swaps the two
        // every time this runs, because after the first pass the readout is already the earlier of
        // the pair.
        if (value.GetSiblingIndex() > slider.transform.GetSiblingIndex())
        {
            value.SetSiblingIndex(slider.transform.GetSiblingIndex());
        }

        var text = value.GetComponent<TMP_Text>();

        if (text != null)
        {
            text.alignment = TextAlignmentOptions.MidlineRight;
            text.enableWordWrapping = false;
        }

        var readout = value.GetComponent<SliderValueLabel>();

        if (readout == null)
        {
            readout = value.gameObject.AddComponent<SliderValueLabel>();
        }

        readout.slider = slider;
        readout.label = text;

        Readout style;

        if (!SliderReadouts.TryGetValue(row.name, out style))
        {
            style = new Readout { Format = "0.00", Multiplier = 1f, Suffix = "" };
        }

        readout.format = style.Format;
        readout.multiplier = style.Multiplier;
        readout.suffix = style.Suffix;
    }

    // The open list used to be Unity's default white-and-grey, which looked like a different
    // program had opened on top of the window. Colours are taken from the window itself rather than
    // typed in here, so a repaint of the options screen carries through.
    private static void StyleDropdowns(Dictionary<string, GameObject> groups, Transform options)
    {
        var windowImage = options.GetComponent<Image>();
        Color windowColor = windowImage != null ? windowImage.color : Color.white;
        var bodyFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(BodyFontGuid));

        if (bodyFont == null)
        {
            Debug.LogError("[OptionsWindowSetup] body font not found by guid " + BodyFontGuid + " - dropdown faces left as they were.");
        }
        Color highlight = new Color(windowColor.r * 0.93f, windowColor.g * 0.86f, windowColor.b * 0.93f, 1f);

        foreach (GameObject group in groups.Values)
        {
            foreach (Transform row in group.transform)
            {
                var dropdown = row.GetComponentInChildren<TMP_Dropdown>(true);

                if (dropdown == null)
                {
                    continue;
                }

                if (dropdown.template != null)
                {
                    var templateImage = dropdown.template.GetComponent<Image>();

                    if (templateImage != null)
                    {
                        templateImage.color = windowColor;
                    }

                    Transform itemBackground = dropdown.template.Find("Viewport/Content/Item/Item Background");

                    if (itemBackground != null)
                    {
                        var itemImage = itemBackground.GetComponent<Image>();

                        if (itemImage != null)
                        {
                            itemImage.color = highlight;
                        }
                    }
                }

                // Coiny is a display face - lovely on a header, hard to read at 15pt inside a
                // dropdown. Both the closed caption and the open list take the face and size of the
                // row's own label, so a dropdown reads like every other option.
                var rowMediator = row.GetComponentInChildren<TranslationMediator>(true);
                var rowLabel = rowMediator != null ? rowMediator.GetComponent<TMP_Text>() : null;

                // The row's own label first: a dropdown row should read like every other option row,
                // and two of them were authored in the display face.
                if (rowLabel != null && bodyFont != null)
                {
                    rowLabel.font = bodyFont;
                }

                foreach (TMP_Text entry in new[] { dropdown.captionText, dropdown.itemText })
                {
                    if (entry == null)
                    {
                        continue;
                    }

                    if (bodyFont != null)
                    {
                        entry.font = bodyFont;
                    }

                    if (rowLabel != null)
                    {
                        entry.fontSize = rowLabel.fontSize;
                        entry.color = rowLabel.color;
                    }

                    entry.enableWordWrapping = false;
                    entry.overflowMode = TextOverflowModes.Ellipsis;
                }

                ScaleDropdownList(dropdown);

                NameUnnamedOptions(dropdown, row.name);
            }
        }
    }

    // The list rows have to be tall enough for the entries that go in them - a 25pt entry in the
    // authored 20px row is invisible, which is what made the open list look empty.
    private static void ScaleDropdownList(TMP_Dropdown dropdown)
    {
        if (dropdown.template == null)
        {
            return;
        }

        var item = dropdown.template.Find("Viewport/Content/Item") as RectTransform;

        if (item == null)
        {
            Debug.LogWarning("[OptionsWindowSetup] dropdown template has no Viewport/Content/Item - list left unscaled.");
            return;
        }

        float entrySize = dropdown.itemText != null ? dropdown.itemText.fontSize : 20f;
        float itemHeight = Mathf.Max(item.sizeDelta.y, entrySize * 1.7f);

        // Written into the asset as well, so the list is the right shape in the editor and not only
        // once the scaler has run.
        item.sizeDelta = new Vector2(item.sizeDelta.x, itemHeight);

        var scaler = dropdown.GetComponent<DropdownListScaler>();

        if (scaler == null)
        {
            scaler = dropdown.gameObject.AddComponent<DropdownListScaler>();
            scaler.baseListHeight = dropdown.template.sizeDelta.y;
        }

        scaler.item = item;
        scaler.list = dropdown.template;
        scaler.baseItemHeight = itemHeight;
        scaler.maxScale = MaxTextScale;
        scaler.fontScaleSetting = Setting("uiFontScale");

        // Same rule for the closed control: 25pt caption text in the authored 30px box clips its
        // descenders. Taken from the constant rather than from the current height, so running this
        // again cannot ratchet the row taller each time.
        float captionSize = dropdown.captionText != null ? dropdown.captionText.fontSize : entrySize;
        float closedHeight = Mathf.Max(DropdownClosedHeight, captionSize * 1.7f);

        var element = dropdown.GetComponent<LayoutElement>();

        if (element != null)
        {
            element.preferredHeight = closedHeight;
        }

        var columnScaler = dropdown.GetComponent<ScaledLayoutSize>();

        if (columnScaler != null)
        {
            columnScaler.baseHeight = closedHeight;
        }
    }

    // Unity ships every new dropdown with Option A/B/C. The two language pickers have no code
    // behind them yet, but they should at least say what they would offer. Nothing else is touched:
    // the resolution list is cleared and rebuilt by ResolutionSelector at startup, so whatever is
    // serialised there is scaffolding, not content.
    private static readonly HashSet<string> LanguageRows = new HashSet<string> { "Text", "Narration" };

    private static void NameUnnamedOptions(TMP_Dropdown dropdown, string rowName)
    {
        if (!LanguageRows.Contains(rowName))
        {
            return;
        }

        if (dropdown.options.Count == 0 || dropdown.options[0].text != "Option A")
        {
            return;
        }

        dropdown.ClearOptions();
        dropdown.AddOptions(new List<string> { "English", "Polski" });
        dropdown.RefreshShownValue();
        Debug.LogWarning("[OptionsWindowSetup] " + rowName + " still had Unity's placeholder entries - filled in with the two languages that exist. Nothing drives it yet.");
    }

    private static void OrderRows(Dictionary<string, GameObject> groups)
    {
        foreach (KeyValuePair<string, GameObject> group in groups)
        {
            string[] order;

            if (!GroupOrder.TryGetValue(group.Key, out order))
            {
                continue;
            }

            int index = 0;

            foreach (string rowName in order)
            {
                Transform row = group.Value.transform.Find(rowName);

                if (row != null)
                {
                    row.SetSiblingIndex(index);
                    index++;
                }
            }
        }
    }

    // A row's own parts are always visible. The language block used to be hidden by switching off
    // its label, back when the control was a child of that label - the restructure separated the
    // two, so the dropdowns came back on screen while their labels stayed dark.
    //
    // The row object is now the thing to switch off when a row should not be seen; its parts are
    // not, and this puts them back.
    private static void ShowRowParts(Dictionary<string, GameObject> groups)
    {
        foreach (GameObject group in groups.Values)
        {
            foreach (Transform row in group.transform)
            {
                if (row.GetComponent<HorizontalLayoutGroup>() == null)
                {
                    continue;
                }

                foreach (Transform part in row)
                {
                    if (!part.gameObject.activeSelf)
                    {
                        part.gameObject.SetActive(true);
                        Debug.LogWarning("[OptionsWindowSetup] " + row.name + "/" + part.name + " was switched off - turned back on. Switch off the row itself to hide a row.");
                    }
                }
            }
        }
    }

    private static void ApplyRowLayouts(Dictionary<string, GameObject> groups)
    {
        foreach (GameObject group in groups.Values)
        {
            foreach (Transform row in group.transform)
            {
                if (row.GetComponent<HorizontalLayoutGroup>() != null)
                {
                    ApplyRowLayout(row);
                }
            }
        }
    }

    // Sliders, dropdowns and readouts grow with the text. Without this the label was the only thing
    // that got bigger, so at 175% a huge label sat next to a slider and a number still drawn for
    // 100% - which is exactly how the tab stopped being readable.
    //
    // Toggles are left out on purpose: a checkbox is a fixed graphic, and widening its column would
    // only push empty space in beside it.
    private static void ScaleControlsWithText(Dictionary<string, GameObject> groups)
    {
        TypeDistinguisher scaleSetting = Setting("uiFontScale");

        foreach (GameObject group in groups.Values)
        {
            foreach (Transform row in group.transform)
            {
                foreach (Transform child in row)
                {
                    bool scalable = child.name == "Value"
                        || child.GetComponent<Slider>() != null
                        || child.GetComponent<TMP_Dropdown>() != null;

                    if (!scalable)
                    {
                        continue;
                    }

                    LayoutElement element = child.GetComponent<LayoutElement>();

                    if (element == null)
                    {
                        continue;
                    }

                    float width;
                    float fraction;

                    if (child.name == "Value")
                    {
                        width = ValueColumnWidth;
                        fraction = ValueMaxFraction;
                    }
                    else if (child.GetComponent<Slider>() != null)
                    {
                        width = SliderColumnWidth;
                        fraction = SliderMaxFraction;
                    }
                    else
                    {
                        width = DropdownColumnWidth;
                        fraction = DropdownMaxFraction;
                    }

                    // The serialised size stays the scale-1 size; the component does the scaling at
                    // runtime, so a later run can never bake a scaled width into the prefab.
                    element.preferredWidth = width;
                    element.minWidth = width;

                    var scaler = child.GetComponent<ScaledLayoutSize>();

                    if (scaler == null)
                    {
                        scaler = child.gameObject.AddComponent<ScaledLayoutSize>();
                    }

                    scaler.baseWidth = width;
                    scaler.baseHeight = element.preferredHeight;
                    scaler.maxFractionOfRow = fraction;
                    scaler.maxScale = MaxTextScale;
                    scaler.fontScaleSetting = scaleSetting;
                }
            }
        }
    }

    // Both language rows carried the key "Language", so both labels rendered the same word and
    // neither dropdown said what it was for.
    private static void FixLanguageRowKeys(Dictionary<string, GameObject> groups)
    {
        RetagRow(groups, "Text", "Language", "Text language");
        RetagRow(groups, "Narration", "Language", "Narration language");
    }

    private static void RetagRow(Dictionary<string, GameObject> groups, string rowName, string oldKey, string newKey)
    {
        Transform row = FindRow(groups, rowName);

        if (row == null)
        {
            return;
        }

        var mediator = row.GetComponentInChildren<TranslationMediator>(true);

        if (mediator == null || mediator.key != oldKey)
        {
            return;
        }

        mediator.key = newKey;
        Debug.LogWarning("[OptionsWindowSetup] " + rowName + " shared the key \"" + oldKey + "\" with the header - given \"" + newKey + "\", which still needs adding to EN/PL.json.");
    }

    // The tab buttons are a fixed width each, so a long name in any language - or the same name at
    // 175% - ran straight out of its button. Auto-sizing lets them fill the button and stop there.
    private static void AutoSizeTabLabels(Transform categories)
    {
        foreach (Transform button in categories)
        {
            foreach (TMP_Text label in button.GetComponentsInChildren<TMP_Text>(true))
            {
                if (label.enableAutoSizing)
                {
                    continue;
                }

                label.enableAutoSizing = true;
                label.fontSizeMax = label.fontSize;
                label.fontSizeMin = 8f;
            }
        }
    }

    private static void ParkRows(Dictionary<string, GameObject> groups)
    {
        foreach (string rowName in ParkedRows)
        {
            Transform row = FindRow(groups, rowName);

            if (row != null && row.gameObject.activeSelf)
            {
                row.gameObject.SetActive(false);
                Debug.Log("[OptionsWindowSetup] " + rowName + " switched off - nothing to pick between while only EN.json is filled in.");
            }
        }
    }

    // Escape gets one owner: EscapeShortcut closes whichever EscapeWindow is on top, and opens the
    // options window when none is.
    private static void MarkEscapeWindow(Transform window)
    {
        if (window == null)
        {
            return;
        }

        if (window.GetComponent<EscapeWindow>() == null)
        {
            window.gameObject.AddComponent<EscapeWindow>();
        }
    }

    private static void WireTabs(Transform options, Transform categories, Transform scrollView, Dictionary<string, GameObject> groups)
    {
        var tabs = options.GetComponent<OptionsTabs>();

        if (tabs == null)
        {
            tabs = options.gameObject.AddComponent<OptionsTabs>();
        }

        tabs.tabs = new List<OptionsTabs.Tab>();
        tabs.scrollView = scrollView.GetComponent<ScrollRect>();
        tabs.defaultTab = 0;

        AddTab(tabs, categories, "gameplay", groups["gameplayGroup"]);
        AddTab(tabs, categories, "audio", groups["audioGroup"]);
        AddTab(tabs, categories, "video", groups["videoGroup"]);
        AddTab(tabs, categories, "accessibility", groups["accessibilityGroup"]);
    }

    private static void AddTab(OptionsTabs tabs, Transform categories, string buttonName, GameObject group)
    {
        Transform button = categories.Find(buttonName);

        if (button == null)
        {
            Debug.LogError("[OptionsWindowSetup] category button not found: " + buttonName);
            return;
        }

        var tab = new OptionsTabs.Tab();
        tab.button = button.GetComponent<Button>();
        tab.group = group;
        tabs.tabs.Add(tab);
    }

    private static void AddFocusAndFontScaler(GameObject root, Transform options)
    {
        if (options.GetComponent<WindowKeyboardFocus>() == null)
        {
            options.gameObject.AddComponent<WindowKeyboardFocus>();
        }

        // On the prefab root, because that is the object WindowManager switches on and off.
        MarkEscapeWindow(root.transform);

        var scaler = root.GetComponent<FontScaler>();

        if (scaler == null)
        {
            scaler = root.AddComponent<FontScaler>();
        }

        scaler.fontScaleSetting = Setting("uiFontScale");
        scaler.maxScale = MaxTextScale;
    }

    private static void HideMenuButtonInMenuScene(Transform options)
    {
        Transform button = options.Find("buttonGroup/MainMenuButton");

        if (button == null)
        {
            Debug.LogError("[OptionsWindowSetup] buttonGroup/MainMenuButton not found.");
            return;
        }

        var hider = options.GetComponent<HideInScene>();

        if (hider == null)
        {
            hider = options.gameObject.AddComponent<HideInScene>();
        }

        hider.target = button.gameObject;
        hider.sceneName = "Menu";
    }

    private static void WireGameSession()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(GameSessionPrefabPath);

        try
        {
            var recall = root.GetComponent<StuckBallRecall>();

            if (recall == null)
            {
                recall = root.AddComponent<StuckBallRecall>();
            }

            recall.activeSetting = Setting("ballRecallActive");
            recall.delaySetting = Setting("ballRecallDelay");

            TypeDistinguisher rainbow = Setting("rainbowAnimation");
            EnableWithSetting(root.transform, "Canvas/ShopScreen/Shop/shopHeaderBG", typeof(RainbowEffect), rainbow);
            EnableWithSetting(root.transform, "Canvas/ShopScreen/PurchaseBG/PurchaseWindow", typeof(RainbowAnimation), rainbow);

            AddFocus(root.transform, "Canvas/ShopScreen");
            AddFocus(root.transform, "Canvas/GameOverScreen");

            MarkEscapeWindow(root.transform.Find("Canvas/ShopScreen"));
            MarkEscapeWindow(root.transform.Find("Canvas/Pause"));

            WireEscapeShortcut(root);

            PrefabUtility.SaveAsPrefabAsset(root, GameSessionPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void WireEscapeShortcut(GameObject gameSession)
    {
        var shortcut = gameSession.GetComponent<EscapeShortcut>();

        if (shortcut == null)
        {
            shortcut = gameSession.AddComponent<EscapeShortcut>();
        }

        shortcut.windowManager = gameSession.GetComponent<WindowManager>();
        shortcut.cancelButton = "Cancel";
        shortcut.topLevelScene = "Menu";

        if (shortcut.windowManager == null)
        {
            Debug.LogError("[OptionsWindowSetup] no WindowManager on GameSession - Escape cannot open the options window.");
            return;
        }

        // The options window is taken from the manager's own list rather than by path, so it stays
        // the same window the O key opens.
        foreach (WindowManager.WindowToggle entry in shortcut.windowManager.windows)
        {
            if (entry.window != null && entry.window.name == "OptionsBG")
            {
                shortcut.optionsWindow = entry.window;
                return;
            }
        }

        Debug.LogError("[OptionsWindowSetup] WindowManager has no OptionsBG entry - Escape has nothing to open.");
    }

    private static void EnableWithSetting(Transform root, string path, System.Type effectType, TypeDistinguisher setting)
    {
        Transform host = root.Find(path);

        if (host == null)
        {
            Debug.LogError("[OptionsWindowSetup] not found: " + path);
            return;
        }

        var effect = host.GetComponent(effectType) as MonoBehaviour;

        if (effect == null)
        {
            Debug.LogError("[OptionsWindowSetup] no " + effectType.Name + " on " + path);
            return;
        }

        foreach (var existing in host.GetComponents<ComponentEnabler>())
        {
            if (existing.target == effect)
            {
                existing.typeDistinguisher = setting;
                return;
            }
        }

        var enabler = host.gameObject.AddComponent<ComponentEnabler>();
        enabler.target = effect;
        enabler.typeDistinguisher = setting;
    }

    private static void AddFocus(Transform root, string path)
    {
        Transform window = root.Find(path);

        if (window == null)
        {
            Debug.LogError("[OptionsWindowSetup] not found: " + path);
            return;
        }

        if (window.GetComponent<WindowKeyboardFocus>() == null)
        {
            window.gameObject.AddComponent<WindowKeyboardFocus>();
        }
    }

    // Three backdrops, three places to put a dimmer: the menu (UI images), the arena the blocks sit
    // on, and the panel the story text is read over. The story one lives in the Gameplay scene and
    // is wired there; these two are prefabs.
    private static void WireBackgroundDimmers()
    {
        TypeDistinguisher dim = Setting("backgroundDim");

        foreach (string path in new[] { "Assets/Prefabs/Menu/Background.prefab", "Assets/Prefabs/LevelBackground.prefab" })
        {
            GameObject root = PrefabUtility.LoadPrefabContents(path);

            try
            {
                var dimmer = root.GetComponent<BackgroundDimmer>();

                if (dimmer == null)
                {
                    dimmer = root.AddComponent<BackgroundDimmer>();
                }

                dimmer.dimSetting = dim;
                dimmer.maxDim = MaxBackgroundDim;
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }

    private static void WirePaddles()
    {
        TypeDistinguisher mode = Setting("paddleControlMode");

        foreach (string path in PaddlePrefabPaths)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(path);

            try
            {
                bool changed = false;

                foreach (var paddle in root.GetComponentsInChildren<PaddleMovement>(true))
                {
                    paddle.controlModeSetting = mode;
                    changed = true;
                }

                if (changed)
                {
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                }
                else
                {
                    Debug.LogWarning("[OptionsWindowSetup] no PaddleMovement in " + path);
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }
}
