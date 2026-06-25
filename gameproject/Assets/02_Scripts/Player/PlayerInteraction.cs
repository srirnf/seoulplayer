using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// 플레이어 주변 책장을 감지하고, F키로 가장 가까운 책장을 연다.
// 플레이어 오브젝트에 이 스크립트 + (이동용과 별개의) Is Trigger Collider2D 를 두면
// 그 트리거 범위 안에 들어온 책장을 인식한다.
public class PlayerInteraction : MonoBehaviour
{
    private readonly List<Bookshelf> nearbyShelves = new List<Bookshelf>();

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsPaused)
        {
            GameUIManager.Instance?.ShowInteractPrompt(false);
            return;
        }

        Bookshelf nearest = GetNearest();
        GameUIManager.Instance?.ShowInteractPrompt(nearest != null);

        var kb = Keyboard.current;
        if (nearest != null && kb != null && kb.fKey.wasPressedThisFrame)
        {
            BookshelfView.Instance?.Open(nearest);
        }
    }

    private Bookshelf GetNearest()
    {
        Bookshelf best = null;
        float bestDist = float.MaxValue;
        foreach (var shelf in nearbyShelves)
        {
            if (shelf == null) continue;
            float d = ((Vector2)shelf.transform.position - (Vector2)transform.position).sqrMagnitude;
            if (d < bestDist)
            {
                bestDist = d;
                best = shelf;
            }
        }
        return best;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        var shelf = other.GetComponent<Bookshelf>();
        if (shelf != null && !nearbyShelves.Contains(shelf))
            nearbyShelves.Add(shelf);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        var shelf = other.GetComponent<Bookshelf>();
        if (shelf != null)
            nearbyShelves.Remove(shelf);
    }
}
