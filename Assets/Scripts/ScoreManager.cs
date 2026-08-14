using TMPro;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI scoreText;
    public TypeDistinguisher savedScore;
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
            //UnityThings. Nie dzia³a xd
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

    public void LoadScore()
    {
        Score.SetScore(savedScore.IntValue);
    }
}
