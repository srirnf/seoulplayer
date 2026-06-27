using UnityEngine;
using UnityEngine.UI;

public class PlayerHP : MonoBehaviour
{
    public Image[] hearts;
    public int hp = 4;

    void Start()
    {
        UpdateHearts();
    }

    public void TakeDamage(int damage = 1)
    {
        hp -= damage;

        if (hp < 0)
            hp = 0;

        UpdateHearts();
    }

    void UpdateHearts()
    {
        for (int i = 0; i < hearts.Length; i++)
        {
            hearts[i].enabled = i < hp;
        }
    }
}