using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;

// Builder for the continue button on the Game Over screen. Written as an editor tool rather than
// done by hand for the same reason as OptionsWindowSetup: Unity keeps every fileID and reference
// straight, and the whole thing can be run again after a change without making a second button.
//
// The button is a copy of the Shop button next to it, so it comes out in the same art and the same
// font with nothing to restyle. Its price and the number of lives it hands back are set once, when
// the button is first made, and left alone afterwards - those are tuning values, and re-running
// this should not undo an afternoon of them.
public static class GameOverContinueSetup
{
    private const string GameSessionPrefabPath = "Assets/Prefabs/GameSession.prefab";

    private const string ScreenPath = "Canvas/GameOverScreen";
    private const string TemplateName = "Shop";
    private const string ButtonName = "Continue";
    private const string ButtonLabel = "Continue";

    // Where the offer sits in the shop, so both ways of buying a life quote the same number.
    private const string HeartOfferPath = "Canvas/ShopScreen/Shop/ShopContainer/heartDrop";

    [MenuItem("Debug/Game Over - build the continue button", priority = 101)]
    public static void Build()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(GameSessionPrefabPath);

        try
        {
            Transform screen = Require(root.transform, ScreenPath);
            Transform buttons = screen == null ? null : Require(screen, "Buttons");
            Transform template = buttons == null ? null : Require(buttons, TemplateName);

            if (template == null)
            {
                return;
            }

            bool isNew = buttons.Find(ButtonName) == null;
            Transform button = isNew
                ? Object.Instantiate(template.gameObject, buttons).transform
                : buttons.Find(ButtonName);

            button.name = ButtonName;

            // First of the three: it is the one a player who just died is looking for.
            button.SetSiblingIndex(0);

            Text label = button.GetComponentInChildren<Text>(true);

            if (label != null)
            {
                label.text = ButtonLabel;
            }

            ContinuePurchase purchase = screen.GetComponent<ContinuePurchase>();

            if (purchase == null)
            {
                purchase = screen.gameObject.AddComponent<ContinuePurchase>();
            }

            purchase.gameOverScreen = screen.gameObject;
            purchase.gameSession = root.GetComponent<GameSession>();
            purchase.button = button.GetComponent<Button>();

            if (isNew)
            {
                Transform heart = root.transform.Find(HeartOfferPath);
                purchase.priceSource = heart == null ? null : heart.GetComponent<ShopEntry>();

                if (purchase.priceSource == null)
                {
                    Debug.LogWarning($"[GameOverContinueSetup] no shop offer at {HeartOfferPath} - the continue keeps its own price of {purchase.price}.");
                }
            }

            WireClick(button.gameObject, purchase);

            PrefabUtility.SaveAsPrefabAsset(root, GameSessionPrefabPath);
            AssetDatabase.SaveAssets();

            Debug.Log(isNew
                ? "[GameOverContinueSetup] continue button added to the Game Over screen."
                : "[GameOverContinueSetup] continue button re-wired.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    // The copy arrives carrying the Shop button's listeners - the click sound, and the call that
    // opens the shop window. The sound is worth keeping and is re-added against this button's own
    // AudioSource; the rest is replaced outright rather than added to, so running this twice
    // cannot leave a button that both continues the game and opens the shop.
    private static void WireClick(GameObject button, ContinuePurchase purchase)
    {
        var click = button.GetComponent<Button>();

        if (click == null)
        {
            Debug.LogError("[GameOverContinueSetup] the copied button has no Button component.");
            return;
        }

        while (click.onClick.GetPersistentEventCount() > 0)
        {
            UnityEventTools.RemovePersistentListener(click.onClick, 0);
        }

        var sound = button.GetComponent<AudioSource>();

        if (sound != null)
        {
            UnityEventTools.AddVoidPersistentListener(click.onClick, sound.Play);
        }

        UnityEventTools.AddVoidPersistentListener(click.onClick, purchase.Continue);
    }

    private static Transform Require(Transform parent, string path)
    {
        Transform found = parent.Find(path);

        if (found == null)
        {
            Debug.LogError($"[GameOverContinueSetup] missing: {parent.name}/{path}");
        }

        return found;
    }
}
