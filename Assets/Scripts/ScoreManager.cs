using TMPro;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI scoreText;
    public TypeDistinguisher savedScore;

    [Tooltip("Which story beat the player is on. The opening one is where a scenario starts counting from nothing.")]
    public TypeDistinguisher currentLevel;

    public GameObject scoreContainer;

    public void Awake()
    {
        Debug.Log("ScoreManager created");
        Score.OnScoreChanged += HandleScoreChanged;
        // The score lives in two places - the static Score.currentScore and the
        // scoreValue TypeDistinguisher in PlayerPrefs - and Awake syncs them only once
        // per app launch, because this manager is never destroyed. Re-reading on every
        // entry into gameplay is what makes New Game (which zeroes the saved value) and
        // Purge show up on screen, instead of carrying the previous run's score over.
        SceneLoader.OnGameplayLoaded += LoadScore;
        SceneLoader.OnGameplayLoaded += ShowScore;
        SceneLoader.OnMenuLoaded += HideScore;
        Level.OnLevelCompleted += HandleLevelCompleted;
        LoadScore();
        HideScore();
    }

    private void HandleLevelCompleted()
    {
        savedScore.SetIntValue(Score.currentScore);
    }

    public void OnDestroy()
    {
        Score.OnScoreChanged -= HandleScoreChanged;
        SceneLoader.OnGameplayLoaded -= LoadScore;
        SceneLoader.OnGameplayLoaded -= ShowScore;
        SceneLoader.OnMenuLoaded -= HideScore;
        Level.OnLevelCompleted -= HandleLevelCompleted;
    }

    public void Start()
    {
        scoreText.text = Score.currentScore.ToString();
    }

    public void HideScore()
    {
        if (this == null)
        {
            //UnityThings. Nie dzia�a xd
            return;
        }
        scoreContainer.SetActive(false);
    }

    public void HandleScoreChanged(int scoreToDisplay)
    {
        ShowScore();
        scoreText.text = "";
        scoreText.text = scoreToDisplay.ToString();
    }

    private void ShowScore()
    {
        if (scoreContainer.active != true)
        {
            scoreContainer.SetActive(true);
        }
    }

    // A scenario counts from nothing. The opening story beat is the one place a run can begin -
    // every other beat continues one - so that is where the tally is cleared, and it covers every
    // way in: New Game, a scenario picked in the menu, or the same one started over after its
    // ending. Without it the score followed the player out of the scenario they had just finished
    // and into the next one they chose, which made the number at the end mean nothing in
    // particular.
    public void LoadScore()
    {
        if (currentLevel != null && currentLevel.IntValue == 0 && savedScore.IntValue != 0)
        {
            savedScore.SetIntValue(0);
        }

        Score.SetScore(savedScore.IntValue);
    }
}
