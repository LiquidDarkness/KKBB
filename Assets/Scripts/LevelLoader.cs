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

    [Tooltip("Given to every collider inside a formation as it loads, so a block bounces the same whether or not it is still linked to its prefab.")]
    public PhysicsMaterial2D blockMaterial;

    public void LoadLevel(LevelData levelToLoad)
    {
        if (container.childCount != 0)
        {
            foreach (Transform child in container)
            {
                Destroy(child.gameObject);
            }
        }

        // The ending placeholders deliberately carry no content and are never loaded, so
        // reaching this with an empty one means something routed us at a level that cannot
        // be played. Say so rather than throwing inside Instantiate, and still switch the
        // music below so the scene is not left in silence.
        if (levelToLoad.content == null)
        {
            Debug.LogError($"[{nameof(LevelLoader)}] {levelToLoad.name} has no content to instantiate.", levelToLoad);
        }
        else
        {
            GameObject formation = Instantiate(levelToLoad.content, container);
            ApplySharedPhysics(formation);
            StartCoroutine(RandomBlockInstantiotor(formation));
        }

        if (levelToLoad.intro != null)
        {
            coreReferences.musicSwitcher.SwitchToSequence(levelToLoad.intro, levelToLoad.loop);
        }
        else
        {
            coreReferences.musicSwitcher.SwitchAudio(levelToLoad.loop);
        }
    }

    // Most of the blocks in SimbaBimba, and the first level of ZiggiesMunda, are copies pasted
    // straight into their formation with no link back to the block prefab - and they have already
    // lost the physics material the prefab carries. Chasing that through the assets would hold
    // only until Unity unpacks something again, so the material is handed out here instead, once
    // per load. Linked or unpacked, every block then bounces the same. Done before the reveal
    // coroutine hides them: SetActive rebuilds the fixture, and it is the material set here that
    // it picks up.
    private void ApplySharedPhysics(GameObject formation)
    {
        if (blockMaterial == null)
        {
            return;
        }

        foreach (Collider2D blockCollider in formation.GetComponentsInChildren<Collider2D>(true))
        {
            blockCollider.sharedMaterial = blockMaterial;
        }
    }

    private IEnumerator RandomBlockInstantiotor(GameObject blockFormation)
    {
        yield return null;
        // Pobierz wszystkie bloki w content
        Block[] blocks = blockFormation.GetComponentsInChildren<Block>(true);
        var wait = new WaitForSeconds(spawnTime / blocks.Length);

        // Every block is hidden first, index 0 included. The hiding used to be done by the
        // shuffle below, and Fisher-Yates leaves index 0 alone - so one block, a different
        // one every time, stayed on screen from the first frame, hittable long before its
        // turn to appear came up.
        foreach (Block block in blocks)
        {
            block.gameObject.SetActive(false);
        }

        // Losowo przetasuj bloki (Fisher-Yates shuffle)
        for (int i = blocks.Length - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            Block temp = blocks[i];
            blocks[i] = blocks[randomIndex];
            blocks[randomIndex] = temp;
        }

        // Teraz bloki s¹ losowo przetasowane, pojawiaj je po kolei
        foreach (Block block in blocks)
        {
            // A block can be gone before its turn comes up: the ball is already in play
            // while this is still running. Reaching into a destroyed one throws, and that
            // exception kills the coroutine with the rest of the blocks still hidden. Level
            // has already counted them, so they can never be hit and the level can never be
            // finished - the ball just rattles around a board that will not end.
            if (block != null)
            {
                block.gameObject.SetActive(true);
            }

            yield return wait; // czas oczekiwania miêdzy pojawieniami siê bloków
        }
    }
}
