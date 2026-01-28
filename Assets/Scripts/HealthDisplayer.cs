using UnityEngine;

public class HealthDisplayer : MonoBehaviour
{
    public GameObject heartImage;
    public Transform heartContainer;

    private void Awake()
    {
        // Subskrybujemy eventy, ¿eby reagowaæ na zmiany zdrowia
        PlayerHealth.OnHealthChanged += DisplayHealth;
        SceneLoader.OnGameplayLoaded += ShowHealth;
        SceneLoader.OnMenuLoaded += HideHealth;
    }

    private void Start()
    {
        // Wyœwietl aktualne ¿ycie od razu
        DisplayHealth(PlayerHealth.Health);
    }

    public void DisplayHealth(int health)
    {
        ClearContainer();
        for (int i = 0; i < health; i++)
        {
            Instantiate(heartImage, heartContainer);
        }
    }

    public void ShowHealth()
    {
        heartContainer.gameObject.SetActive(true);
    }

    public void HideHealth()
    {
        heartContainer.gameObject.SetActive(false);
    }

    private void ClearContainer()
    {
        foreach (Transform child in heartContainer)
        {
            Destroy(child.gameObject);
        }
    }

    private void OnDestroy()
    {
        // Odsubskrybuj eventy, ¿eby unikn¹æ memory leak
        PlayerHealth.OnHealthChanged -= DisplayHealth;
        SceneLoader.OnGameplayLoaded -= ShowHealth;
        SceneLoader.OnMenuLoaded -= HideHealth;
    }
}
