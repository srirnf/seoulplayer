using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PlayerHP : MonoBehaviour
{
    public Image[] hearts;
    public int hp = 4;

    public bool isInvincible = false;

    void Start()
    {
        UpdateHearts();
    }

    public void TakeDamage(int damage = 1)
    {
        if (isInvincible) return;

        hp -= damage;

        if (hp < 0)
            hp = 0;

        UpdateHearts();

        if (hp <= 0)
        {
            GameOver();
            return;
        }

        StartCoroutine(InvincibilityCoroutine());
    }

    IEnumerator InvincibilityCoroutine()
    {
        isInvincible = true;
        yield return new WaitForSeconds(0.5f);
        isInvincible = false;
    }

    void UpdateHearts()
    {
        for (int i = 0; i < hearts.Length; i++)
        {
            hearts[i].enabled = i < hp;
        }
    }

    void GameOver()
    {
        Debug.Log("게임 오버");

        Time.timeScale = 0f; // 게임 정지
        // 여기 나중에 UI 넣으면 됨 (GameOver Panel 등)
    }
}