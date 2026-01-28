using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LevelLoader : MonoBehaviour
{
    //TODO: make it a class only responsible for loading levels, with cute animations (to story and for the block formations)
    //This class should only exist in the Gameplay scene and be accessible to LevelManager
    public CoreReferences coreReferences;
    public Transform container;

    public float spawnTime;

    public TypeDistinguisher recentLevel;

    public void LoadLevel(LevelData levelToLoad)
    {
        if (container.childCount != 0)
        {
            foreach (Transform child in container)
            {
                Destroy(child.gameObject);
            }

        }
        StartCoroutine(RandomBlockInstantiotor(Instantiate(levelToLoad.content, container)));
        coreReferences.musicSwitcher.SwitchAudio(levelToLoad.ambience);
    }

    private IEnumerator RandomBlockInstantiotor(GameObject blockFormation)
    {
        yield return null;
        // TODO: zrobiæ maksymaln¹ duration na pojawienie siê wszystkich klocków jako zmienn¹.
        // Pobierz wszystkie bloki w content
        Block[] blocks = blockFormation.GetComponentsInChildren<Block>(true);
        var wait = new WaitForSeconds(spawnTime / blocks.Length);

        // Losowo przetasuj bloki (Fisher-Yates shuffle)
        for (int i = blocks.Length - 1; i > 0; i--)
        {
            blocks[i].gameObject.SetActive(false);
            int randomIndex = Random.Range(0, i + 1);
            Block temp = blocks[i];
            blocks[i] = blocks[randomIndex];
            blocks[randomIndex] = temp;
        }

        // Teraz bloki s¹ losowo przetasowane, pojawiaj je po kolei
        foreach (Block block in blocks)
        {
            block.gameObject.SetActive(true);
            yield return wait; // czas oczekiwania miêdzy pojawieniami siê bloków
        }
    }

    class Dummy : MonoBehaviour { }
}
