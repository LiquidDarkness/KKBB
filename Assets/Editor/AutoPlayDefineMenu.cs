using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

// The switch for AutoPlayer. The autoplay code is wrapped in #if AUTOPLAY, so it only exists -
// in the editor and in a build - while this symbol is set. Ticking the menu item recompiles the
// project and the setting sticks in Player Settings, so a build made afterwards has autoplay in
// it and one made with the item unticked does not.
//
// It is deliberately not a runtime toggle: the point of the symbol is that a public build can be
// guaranteed to carry none of this, and that guarantee only holds if the compiler is what strips
// it out. Only the currently selected build target group is touched, DEMO_BUILD and anything
// else already there is preserved.
public static class AutoPlayDefineMenu
{
    private const string Symbol = "AUTOPLAY";
    private const string MenuPath = "Debug/Autoplay compiled into the build";

    [MenuItem(MenuPath, priority = 100)]
    private static void Toggle()
    {
        BuildTargetGroup group = EditorUserBuildSettings.selectedBuildTargetGroup;
        List<string> symbols = ReadSymbols(group);

        if (symbols.Contains(Symbol))
        {
            symbols.Remove(Symbol);
            Debug.Log($"Autoplay is now OFF for {group} - the AutoPlayer code will not be compiled.");
        }
        else
        {
            symbols.Add(Symbol);
            Debug.Log($"Autoplay is now ON for {group} - press F9 in the Gameplay scene to use it.");
        }

        PlayerSettings.SetScriptingDefineSymbolsForGroup(group, string.Join(";", symbols));
    }

    [MenuItem(MenuPath, isValidateFunction: true)]
    private static bool ToggleValidate()
    {
        Menu.SetChecked(MenuPath, IsSet());
        return true;
    }

    private static bool IsSet()
    {
        return ReadSymbols(EditorUserBuildSettings.selectedBuildTargetGroup).Contains(Symbol);
    }

    private static List<string> ReadSymbols(BuildTargetGroup group)
    {
        return PlayerSettings.GetScriptingDefineSymbolsForGroup(group)
            .Split(';')
            .Select(symbol => symbol.Trim())
            .Where(symbol => !string.IsNullOrEmpty(symbol))
            .ToList();
    }
}
