using UnityEngine;
using UnityEngine.UI;

// The way off the Game Over screen that is not the main menu: buy a life with the score the shop
// spends, and carry on where the run stopped.
//
// It sits on the Game Over screen itself, which is switched off until the player dies, so it only
// watches the score while the offer is actually on the table - the same arrangement ShopManager
// uses, and for the same reason.
public class ContinuePurchase : MonoBehaviour
{
    [Header("References")]
    public GameObject gameOverScreen;
    public GameSession gameSession;
    public Button button;

    [Header("Offer")]
    [Tooltip("Take the price from a shop offer, so the same life costs the same either way. Cleared, the price below is used.")]
    public ShopEntry priceSource;

    [Tooltip("Score spent on a continue, when no shop offer is named above.")]
    public int price = 2500;

    [Tooltip("Hearts the player is left holding after a continue.")]
    public int livesGranted = 1;

    private void OnEnable()
    {
        Score.OnScoreChanged += HandleScoreChanged;
        PlayerHealth.OnHealthChanged += HandleHealthChanged;
        Refresh();
    }

    private void OnDisable()
    {
        Score.OnScoreChanged -= HandleScoreChanged;
        PlayerHealth.OnHealthChanged -= HandleHealthChanged;
    }

    private void HandleScoreChanged(int _)
    {
        Refresh();
    }

    private void HandleHealthChanged(int _)
    {
        Refresh();
    }

    // A heart bought in the shop while the Game Over screen is up has already paid for the life,
    // which is why the health event is worth listening to as well: it turns the button on without
    // the score having moved, and the continue itself is then free.
    private void Refresh()
    {
        if (button == null)
        {
            return;
        }

        button.interactable = AlreadyAlive || Score.currentScore >= Price;
    }

    // The shop's heart buys exactly this, so it sets the price by default rather than a second
    // number being kept in step with it by hand.
    private int Price => priceSource != null ? priceSource.price : price;

    // Death is declared at -1, so anything from zero up is a player who can play on.
    private bool AlreadyAlive => PlayerHealth.healthTD != null && PlayerHealth.Health >= 0;

    public void Continue()
    {
        if (PlayerHealth.healthTD == null)
        {
            Debug.LogWarning($"{name}: no health to give back - PlayerRig never initialised PlayerHealth.", this);
            return;
        }

        if (!AlreadyAlive)
        {
            // Charged here rather than trusting the button state: the click's other listeners -
            // the sound - fire whatever this one decides.
            int cost = Price;

            if (Score.currentScore < cost)
            {
                Debug.Log($"Cannot afford to continue: {Score.currentScore} of {cost}.");
                return;
            }

            Score.AddToScore(-cost);

            // One GainHealth only brings the count back to zero: playable, but with no heart on
            // screen and the next miss ending the run again. Buy up to a count the player can see.
            int target = Mathf.Max(livesGranted, 0);

            while (PlayerHealth.Health < target)
            {
                PlayerHealth.GainHealth();
            }
        }

        Resume();
    }

    private void Resume()
    {
        if (gameOverScreen != null)
        {
            gameOverScreen.SetActive(false);
        }
        else
        {
            gameObject.SetActive(false);
        }

        // Only GameSession's own lock, the one PlayerHealth.OnDeath took. ReleaseAll would also
        // throw away the lock of any window still open over this one.
        GameSession session = gameSession != null ? gameSession : FindObjectOfType<GameSession>();

        if (session != null)
        {
            session.Unpause();
        }
        else
        {
            Debug.LogError($"{name}: no GameSession to unpause - the game would stay frozen.", this);
        }

        // LoseCollider already put the ball back on the paddle before the death was declared, so
        // this only matters if something moved it since. BallMovement.SetLaunchBool runs every
        // frame, so the ball becomes launchable again on its own once the pause is off.
        if (PlayerRig.instance != null)
        {
            PlayerRig.instance.LockToPaddle();
        }
    }
}
