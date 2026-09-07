using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;

// Puts the Polish letters into the font atlases.
//
// A TMP font asset is not a font: it is a picture of the letters somebody asked for, taken once and
// saved. Every atlas in this project was taken over ASCII and Latin-1, which has o-acute in it and
// not one of the other eight Polish letters - so a Polish build would have come out with holes in
// it whatever the translation said. The .ttf files themselves are fine: Coiny and every weight of
// Josefin Sans carry the whole set. Only Metal Mania does not, which costs nothing, because the one
// word set in it is METAL.
//
// The atlas is retaken rather than added to, because a glyph is packed wherever there is room and
// there is no telling where. Everything the asset already had is asked for again alongside the new
// letters, from the same font file at the same point size, padding and render mode - so nothing
// that was on screen yesterday changes shape, and the only texture written to is the asset's own,
// which is what keeps every reference in every prefab pointing at the same place.
public static class PolishLetters
{
    // Written as escapes on purpose. Thirteen files in this project are cp1250 and a Polish letter
    // typed into a source file is one bad save away from being lost; a number cannot be mangled.
    // In order: a-ogonek, c-acute, e-ogonek, l-stroke, n-acute, o-acute, s-acute, z-acute, z-dot,
    // and the same nine again in upper case.
    private const string Letters =
        "\u0105\u0107\u0119\u0142\u0144\u00F3\u015B\u017A\u017C" +
        "\u0104\u0106\u0118\u0141\u0143\u00D3\u015A\u0179\u017B";

    // Not letters, but what Polish punctuation actually looks like once someone writes properly in
    // it: the low opening quote, both closing quotes, the two dashes that are not hyphens, and an
    // ellipsis that is one character rather than three full stops.
    private const string Punctuation = "\u201E\u201D\u201C\u2013\u2014\u2026";

    // How far an atlas is allowed to grow before this gives up and says so. Two thousand square is
    // already far more room than a Latin alphabet at this size needs; anything still refused past
    // that is a letter the font file does not have, which no amount of room will conjure.
    private const int MaxAtlasSize = 2048;

    private const string TextureWidthProperty = "_TextureWidth";
    private const string TextureHeightProperty = "_TextureHeight";

    // Every face anything a player reads is written in, by how much of the game is set in it.
    private static readonly string[] FontAssetPaths =
    {
        "Assets/Fonts/JosefinSans-SemiBold SDF.asset",
        "Assets/Coiny-Regular.asset",
        "Assets/Fonts/JosefinSans-Medium SDF.asset",
        "Assets/Fonts/JosefinSans-Bold SDF.asset",
        "Assets/Fonts/JosefinSans-Italic SDF.asset",
    };

    [MenuItem("Debug/Language - put the Polish letters in the fonts", priority = 131)]
    public static void RunAll()
    {
        foreach (string path in FontAssetPaths)
        {
            Rebuild(path);
        }

        AssetDatabase.SaveAssets();
        Report();
    }

    [MenuItem("Debug/Language - check the fonts for Polish letters", priority = 132)]
    public static void Report()
    {
        foreach (string path in FontAssetPaths)
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);

            if (font == null)
            {
                Debug.LogError("[PolishLetters] there is no font asset at " + path);
                continue;
            }

            string missing = Missing(font, Letters + Punctuation);

            if (string.IsNullOrEmpty(missing))
            {
                Debug.Log("[PolishLetters] " + font.name + ": all " + (Letters.Length + Punctuation.Length)
                    + " characters are in the atlas, which holds " + font.characterTable.Count + " in all.");
            }
            else
            {
                Debug.LogError("[PolishLetters] " + font.name + " is still missing " + Describe(missing));
            }
        }
    }

    private static void Rebuild(string path)
    {
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);

        if (font == null)
        {
            Debug.LogError("[PolishLetters] there is no font asset at " + path);
            return;
        }

        if (string.IsNullOrEmpty(Missing(font, Letters + Punctuation)))
        {
            Debug.Log("[PolishLetters] " + font.name + " already has them - left alone.");
            return;
        }

        // What it had, so that retaking the atlas gives everything back. Read before anything is
        // cleared, because clearing is what empties this table.
        var wanted = new StringBuilder();

        foreach (TMP_Character character in font.characterTable)
        {
            if (character.unicode > 0 && character.unicode <= 0xFFFF)
            {
                wanted.Append((char)character.unicode);
            }
        }

        int had = font.characterTable.Count;
        wanted.Append(Letters).Append(Punctuation);

        AtlasPopulationMode wasIn = font.atlasPopulationMode;
        int width = font.atlasWidth;
        int height = font.atlasHeight;
        bool complete = false;
        string refused = string.Empty;

        // These atlases were taken at exactly the size the letters of the day needed, and eighteen
        // more do not fit in what is left: asked to take them, the packer runs out of room and the
        // asset comes back with fewer characters than it started with. So the atlas is allowed to
        // grow, one doubling at a time, and only as far as it has to - a page of this texture ships
        // with the game and there is no sense in paying for room nothing stands in.
        while (true)
        {
            Resize(font, width, height);

            // Dynamic is the only mode that will draw a glyph that is not already there. The setter
            // hands the source font file back to the asset on the way in and takes it away again on
            // the way out, which is what keeps a static asset from carrying a whole .ttf into the
            // build.
            font.atlasPopulationMode = AtlasPopulationMode.Dynamic;

            if (font.sourceFontFile == null)
            {
                font.atlasPopulationMode = wasIn;
                Debug.LogError("[PolishLetters] " + font.name + " has no font file behind it any more - there is nothing to draw the letters from.");
                return;
            }

            // Empties the tables and, on the way, makes the atlas texture readable and the size it
            // has just been told to be - a static atlas is saved without a copy anything can write
            // into, and TMP refuses to draw into one.
            font.ClearFontAssetData();

            complete = font.TryAddCharacters(wanted.ToString(), out refused, true);
            font.atlasPopulationMode = wasIn;

            if (complete || (width >= MaxAtlasSize && height >= MaxAtlasSize))
            {
                break;
            }

            // Taller before wider, so the first thing tried is half the texture a doubling of both
            // would cost. Eighteen letters do not need four times the room, and this atlas is paid
            // for in every build - the browser one most of all.
            if (height <= width)
            {
                height *= 2;
            }
            else
            {
                width *= 2;
            }
        }

        font.material.SetFloat(TextureWidthProperty, width);
        font.material.SetFloat(TextureHeightProperty, height);

        EditorUtility.SetDirty(font);
        EditorUtility.SetDirty(font.material);

        if (font.atlasTextures != null && font.atlasTextures.Length > 0 && font.atlasTextures[0] != null)
        {
            EditorUtility.SetDirty(font.atlasTextures[0]);
        }

        if (!complete)
        {
            // A letter the font file does not have, rather than one there was no room for - the
            // atlas has been given every chance to grow by now. That is a font to replace, not
            // anything this can fix, and the asset is worse than it was: git will put it back.
            Debug.LogError("[PolishLetters] " + font.name + " would not take " + Describe(refused)
                + " even at " + width + "x" + height + ". It had " + had + " characters and now holds "
                + font.characterTable.Count + " - restore the asset from git before building.");
            return;
        }

        // The atlas is left readable, which is how it comes back from being cleared and refilled -
        // TMP makes it readable to draw into and only its own font asset creator, through an
        // internal editor call nothing outside Unity can make, ever puts that back. It costs a copy
        // of the texture in memory and nothing in the build, the pixels being in the asset either
        // way; the same is true of every dynamic font asset a project ever ships.
        Debug.Log("[PolishLetters] " + font.name + ": " + had + " characters became " + font.characterTable.Count
            + " in a " + width + "x" + height + " atlas.");
    }

    // The atlas size is a private field with nothing but a getter in front of it, and the shader
    // has to be told the same numbers or every glyph is sampled from the wrong place.
    private static void Resize(TMP_FontAsset font, int width, int height)
    {
        if (font.atlasWidth == width && font.atlasHeight == height)
        {
            return;
        }

        var serialized = new SerializedObject(font);
        serialized.FindProperty("m_AtlasWidth").intValue = width;
        serialized.FindProperty("m_AtlasHeight").intValue = height;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static string Missing(TMP_FontAsset font, string characters)
    {
        var missing = new StringBuilder();
        var present = new HashSet<uint>();

        foreach (TMP_Character character in font.characterTable)
        {
            present.Add(character.unicode);
        }

        foreach (char c in characters)
        {
            if (!present.Contains(c))
            {
                missing.Append(c);
            }
        }

        return missing.ToString();
    }

    // By number, not by letter: the editor log is written wherever the console happens to be, and a
    // Polish letter printed into a console that cannot spell it says nothing at all.
    private static string Describe(string characters)
    {
        var text = new StringBuilder();

        foreach (char c in characters)
        {
            if (text.Length > 0)
            {
                text.Append(", ");
            }

            text.Append("U+").Append(((int)c).ToString("X4"));
        }

        return characters.Length + " characters (" + text + ")";
    }
}
