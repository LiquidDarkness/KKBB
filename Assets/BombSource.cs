using TMPro;
using UnityEngine;

public class BombSource : MonoBehaviour
{
    public int bombCount;
    public TextMeshPro bombNumber;
    public SpriteRenderer bombFace;

    public void Start()
    {
        UpdateBombDisplay();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (bombCount == 0)
        {
            return;
        }

        Bomb bomb = collision.gameObject.GetComponent<Bomb>();
        if (bomb == null || bomb.hasBomb)
        {
            return;
        }

        bomb.ActivateBomb();
        bombCount--;
        UpdateBombDisplay();
    }

    internal void AddBomb()
    {
        bombCount++;
        UpdateBombDisplay();
    }

    private void UpdateBombDisplay()
    {
        if (bombCount > 0)
        {
            bombNumber.gameObject.SetActive(true);
            bombFace.gameObject.SetActive(true);
            bombNumber.text = bombCount.ToString();
        }
        else
        {
            bombNumber.gameObject.SetActive(false);
            bombFace.gameObject.SetActive(false);
        }
    }

    private void ShowBombDisplay()
    {
        bombNumber.gameObject.SetActive(true);
        bombFace.gameObject.SetActive(true);
        bombNumber.text = bombCount.ToString();
    }
}