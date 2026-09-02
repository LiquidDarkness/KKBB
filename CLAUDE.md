# Kitty Kitty Bounce Bounce

Unity **2021.3.15f1**, 2D breakout where the ball is the cat. Story-driven: each scenario is a
series of story beats, each beat paired with a level. Shipping to Steam as a demo and a full game
from one project, split by a scripting define.

New code, comments, commit messages and asset names are English; Polish only in chat. Older files
still carry Polish comments, which is where the encoding trap below comes from.

## Scenes and how a run starts

`SteamInit` (build index 0) -> `Menu` -> `Gameplay`. `NonSteamInit` is empty and disabled in the
build settings, a leftover of an older idea; the switch for a non-Steam build is `NO_STEAM` (below),
not a different starting scene. `test.unity` is a scratchpad and is not in the build.

**`GameSession.prefab`** is the persistent object: HUD, shop, pause and game-over screens, the score
manager, the window manager, achievements, and the scenario score summary. It carries
`DontDestroyOnLoad`, so anything on it outlives every scene - and anything in `Gameplay` that needs
to reach it goes through **`CoreReferences`** (`Assets/Data/CoreReferences.asset`), the shared asset
components register themselves into. `GameEnding` reaching the story manager is the pattern to copy.

## The systems worth knowing before touching anything

- **Settings and saving.** Every setting and every piece of progress is a `TypeDistinguisher`
  ScriptableObject in `Assets/Resources/TypeDistinguishers`, backed by PlayerPrefs and mirrored to
  `save.json` (`SaveManager`) so Steam Cloud can carry it. PlayerPrefs answers **0 for a key nobody
  has written**, which for a switch reads as "the player turned it off" - so every setting that
  wants a different default declares one in its `defaultValue` field, and `SaveManager.LoadDefaults`
  seeds the missing ones on every start. Blank means "do not seed", which is what progress wants.
- **Difficulty and scenarios.** `New Diffculty Manager.asset` and `New Scenario Manager.asset` hold
  the lists; the chosen one is an index in a setting, and both guard against an index outside the
  list rather than throwing. `DifficultySettings` carries lives, speeds, drop table, chain chance
  and `scoreMultiplier`.
- **Story.** A `StoryContainer` per scenario holds the beats in order. **The last beat is the
  farewell**, and its level is a placeholder with no content that must never be loaded - showing a
  beat is what decides whether the ending block or the continue block is on screen
  (`StoryManager.IsOnEndingBeat`), because a scenario can be walked into on that beat with Continue.
  A scenario is asked for its beats rather than read off its array - `BeatCount`, `BeatAt`,
  `HasFarewellBeat`, `HasBeatText` - so one can answer without keeping a list. `OnBeatShown` fires
  every time a beat goes up, and is what anything per-level should hang off.
- **Endless** is a scenario like any other (`EndlessScenario`, `Assets/Stories/Endless`), last in
  the scenario manager's list and started by its own button on the main menu, which rolls a seed
  and sends the player through the same difficulty screen. It has no beats written down: wave N is
  dealt from `endlessSeed` and N alone, the whole pool shuffled per cycle and no level twice
  running, so a save carries one number and the run comes back the same. It has no farewell either,
  so a run ends at death - `EndlessRunController` tallies it there and puts the summary up over the
  Game Over screen, and a bought continue re-arms it. Each wave is quicker (part of the way from
  `baseGameSpeed` to `maxSpeed`) and pays more (`Score.pointsMultiplier`), with a life back every
  few waves and no ceiling on them; `EndlessBeatCard` writes the wave card in place of story text. Having no
  story to pick a cat for it, it asks: the button opens `AvatarWindow` - the scenario window copied,
  one tile per entry in `AvatarSwitcher.avatars`, writing `ChosenAvatar` - and the cat tiles open the
  difficulty window the way a scenario tile does. The button lives in `Content` just after the
  easter egg art so it is drawn over it and still under the windows; it wears the cat's own halo
  (`TextGlow`, the font's glow rather than a sprite), arrives from behind the logo (`MenuEntrance`,
  which is hierarchy order and nothing else - it is lent to the logo's branch for the flight) and
  bounces its letters (`TextBounceEffect`, in the mesh rather than the transform, so no layout is
  disturbed), while the art beside it breathes (`IdleSway`). Every one of those that moves is under
  the Menu animations setting through a `ComponentEnabler` of its own, like every other animation. Rebuilt or
  re-wired from **Debug > Endless** (`Assets/Editor/EndlessSetup.cs`), which is idempotent and
  leaves the button where anyone has since dragged it.
- **Translations.** Every string a player reads goes through a `TranslationMediator` (key +
  `onTranslationSet` -> `TMP_Text.set_text`) with entries in **both** `Assets/Stories/texts/EN.json`
  and `PL.json`. The key is the English text. This includes labels sitting in a scene, like the
  boinks. Two things the component cannot reach have their own way in: a dropdown keeps its options
  as strings on itself rather than in labels, so `DropdownOptionTranslator` looks the whole list up
  and refreshes the caption; and text whose key is not known until it is on screen - the face of a
  key binding is whatever the player pressed last - asks `TranslationLookup.Get`, which reads the
  same dictionary and hands back the key itself when there is no entry. That fallback is why a bare
  key name like "A" or "F5" needs no entry, while the words - "Space", "Left mouse", "Numpad 1" -
  have one. Anything looking a string up itself has to redraw on
  `TranslationJSONDeserializer.OnTransaltionUpdated`, the same event the mediator listens to.
- **Windows over the game.** A window is a GameObject with `EscapeWindow` (Escape closes the topmost
  one) and `WindowKeyboardFocus` (selection lands on a control, so Enter/Space work without a
  mouse). Pausing is `PauseManager.Pause(name)` / `Unpause(name)` - **the lock name must be the
  window GameObject's name**, because that is what `EscapeWindow` gives back when it closes one
  nobody else claims. Windows in `WindowManager.windows` get all of this for free, but their
  `toggleKey` is read every frame through `Controls.Pressed`, which warns loudly about a key that is
  not on the list - so do not register a window with an empty toggle key.
- **Controllers.** A gamepad plays the game without Steam Input and without a line of Steam code.
  Menus were already covered by the Input Manager's own defaults - `Submit` carries joystick button
  0, `Cancel` carries button 1, and `Horizontal`/`Vertical` each have a joystick entry beside the
  keyboard one. Gameplay is `Controls`: the paddle takes the left stick through the **`Gamepad
  Horizontal`** axis, which is joystick-only on purpose, because the Input Manager's own
  `Horizontal` folds the arrow keys into the same reading and arrows a player had rebound away
  would go on steering. The buttons are a fixed table in `Controls.GamepadButtons` - A launches, X
  opens the shop, Back opens the options, Start pauses, in XInput numbering - checked by `Held` and
  `Pressed` beside the two keys each action already had. A is also `Submit`, so it alone is
  swallowed while the game is paused; the buttons that open and close windows have to keep working
  while a window is up. **The rebinding screen is the keyboard's**: it ignores a joystick button
  pressed while it is waiting, because a pad is remapped in Steam's configurator, which is where
  someone holding one looks. A stick skips the wind-up ramp the keys need - it already says how
  hard it is being pushed.
- **Scoring.** Blocks pay by how hard they are to break (5 for one hit, 15 for the invisible
  three-hit ones, per block in `Block.pointsPerBlockDestroyed`); drops pay through `PointAdder`. A
  scenario counts from nothing at its opening beat and is added up when it ends:
  `(collected + lives * 750) * scoreMultiplier`. The result is kept per scenario **and difficulty**
  in `scenarioBestScores` as `Scenario|Difficulty=score` (`ScenarioRecords`), and shown by
  `ScenarioScoreSummary` - which lives on GameSession, while `SummaryBoinkController` in the scene
  puts the block that opens it on screen.
- **Steam Input is not integrated**, and the game does not miss it: see Controllers above. Every
  controller type is opted into Steam Input on the partner site, and the game has been played
  through the Steam Link app on a phone, which is the awkward case - it arrives as an emulated
  XInput pad, the same road a real Xbox pad takes on Windows. What is still owed is partner-site
  work nobody can do from here: a default configuration built in Big Picture and published, which
  needs a pad in hand, and a touch configuration recorded for Remote Play. Neither is code. Full
  Steam Input - an in-game actions file, action sets, `ISteamInput` polling and device-specific
  glyphs - is a separate decision that has not been taken.
- **Steam** is Facepunch.Steamworks (not Steamworks.NET), in `Assets/Facepunch.Steamworks`.
  `Achievements` writes every unlock to the save first and tells Steam second, so a demo player
  keeps what they earned when they first run the full game. `Leaderboards` holds one board per
  scenario and difficulty. Both, and `SteamInitializer`, answer to the defines below.

## Scripting defines

| define | what it does |
| --- | --- |
| `DEMO_BUILD` | Gates which scenarios are playable and which app id is reported. Clear it for the full build, or the full game ships as the demo. Also switches leaderboards off, the demo being a separate app id. |
| `NO_STEAM` | Takes all of Steam out: no client started, no callbacks pumped, no achievement sent, no score posted. For copies uploaded anywhere other than Steam. Play, scoring, records and the save are untouched, and the native steam_api dll need not ship, since nothing loads it. |
| `AUTOPLAY` | Debug autoplayer and the story shortcuts. Never ship with it. |

## Conventions and traps, all of them found the hard way

1. **Thirteen `.cs` files are cp1250, not UTF-8** - among them `Block.cs`, `StoryManager.cs`,
   `LevelLoader.cs`, `GameController.cs`, `PlayerHealth.cs`, `RainbowEffect.cs`, `WindowManager.cs`.
   Editing them as text destroys the Polish characters in their comments. Patch them **byte-wise**
   (perl in byte mode, or Python reading `rb`/writing `wb`) with ASCII-only replacements, and check
   `git diff --stat` afterwards: the line count should match what you changed and nothing else.
2. **Static events outlive the scene.** `StoryManager`, `Level`, `Block` and `SceneLoader` all raise
   static events while living in `Gameplay`. A subscriber that does not unsubscribe is called on a
   destroyed object, and the exception stops every delegate queued behind it - which is how one
   stale handler silences the whole chain. Subscribe in `OnEnable`, drop it in `OnDisable`.
3. **`StoryManager.OnScenarioFinished` fires twice** at the end of a scenario: from `Progress` and
   again from `GameEnding.Start`. Anything acting on it must be idempotent or guarded.
4. **Disabling a MonoBehaviour does not stop its coroutines.** Start them in `OnEnable` and stop
   them in `OnDisable`, or an option switched off keeps running until the scene reloads.
5. **The story canvas is world space and sits under everything**; the HUD canvas on GameSession is
   screen-space overlay at sorting order 1330. Anything meant to cover the game belongs in the
   latter.
6. **`Block` sits on hundreds of objects** across the level prefabs, which is why block-side code
   reads settings by name through `OptionSettings.Find` instead of a serialized reference.
   Everywhere else, use the serialized reference.
7. **Steam answers are not guaranteed.** `Friend.Name` throws outright with no client behind it, and
   it is read while drawing a window. Guard anything Steam-facing that runs inside UI code.
8. Leaderboard limits worth remembering: **10 uploads per 10 minutes**, one call outstanding at a
   time, **one entry per player per board** (hence the split by difficulty), up to 64 extra int32s
   beside an entry. Boards are looked up with `FindLeaderboardAsync`, which exists in the shipped
   dll even though it is missing from its XML docs; they are only created as a fallback, because a
   board made from code stays off the community hub until it is given a Community Name on the
   partner site.

## Verifying without the Editor GUI

The Editor holds a lock on the project, so **it has to be closed** before any of this runs.

```bash
"/c/Program Files/Unity/Hub/Editor/2021.3.15f1/Editor/Unity.exe" -batchmode -nographics -quit \
  -projectPath "C:/Users/Jumack/Desktop/Projekty/Otwarte/Kitty Kitty Bounce Bounce" -logFile /tmp/unity.log
```

Then `grep -niE "error CS|exception" /tmp/unity.log`; success ends with `Exiting batchmode
successfully now!`.

Scenes and prefabs are best edited by a throwaway `Assets/Editor` script run with `-executeMethod`,
rather than by hand in YAML: `PrefabUtility.LoadPrefabContents` / `SaveAsPrefabAsset` for prefabs,
`EditorSceneManager.OpenScene` / `SaveScene` for scenes. Delete the script afterwards. Unity rewrites
the whole file, so check nothing was lost by comparing sorted lines against a copy taken first -
normalise line endings before comparing, or every line reads as changed:

```bash
diff <(tr -d '\r' < before.prefab | sort) <(sort after.prefab) | grep -c "^<"
```

**A test that touches PlayerPrefs must put them back.** Copy `save.json` from
`%USERPROFILE%/AppData/LocalLow/Liquid Darkness/KittyKittyBounceBounce/` before the test and restore
from that file afterwards - not from a snapshot held in the test, because the `TypeDistinguisher`
assets are unloaded when a scene is opened and a snapshot holding them cannot be read after that.
`save.json` is the source of truth anyway: `SaveManager.Load` applies it to PlayerPrefs at every
start.

Edit mode has no `Awake`, no `OnEnable`, no `EventSystem.current` and nothing injected into
`CoreReferences`, so a test walking a runtime path has to call those by hand and expect the music
switcher to be missing.

## Git

Branch `dev`. Commit **`Assets/` only, plus `ProjectSettings/InputManager.asset` when an axis
changes** - it is the one file outside `Assets/` the game will not run without. `ProjectSettings.asset`
is tracked but stays out of commits, because its scripting defines get changed constantly for local
tests and builds. Commit messages are English prose saying what changed and why it was wrong before.
