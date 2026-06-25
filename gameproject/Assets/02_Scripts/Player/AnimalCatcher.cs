using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// 플레이어(사육사). WASD로 이동하고, 가장 가까운 자유 동물을 자동 록온한 뒤
// Space를 꾹 눌러 유인 게이지를 채운다. 꼬셔진 동물은 줄줄이 따라온다.
public class AnimalCatcher : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 4.5f;
    [Tooltip("자동 록온 범위")]
    [SerializeField] private float lockRange = 3f;
    [Tooltip("Space 누를 때 초당 게이지 증가량 (1이면 1초)")]
    [SerializeField] private float lureSpeed = 0.8f;

    private readonly List<Animal> caughtChain = new List<Animal>();
    private Animal locked;

    private void Update()
    {
        var gm = ParkGameManager.Instance;
        if (gm != null && !gm.IsPlaying) return;

        Move();
        UpdateLockAndLure();
    }

    private void Move()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        float x = (kb.dKey.isPressed ? 1f : 0f) - (kb.aKey.isPressed ? 1f : 0f);
        float y = (kb.wKey.isPressed ? 1f : 0f) - (kb.sKey.isPressed ? 1f : 0f);
        Vector3 dir = new Vector3(x, y, 0f);
        if (dir.sqrMagnitude > 1f) dir.Normalize();
        transform.position += dir * moveSpeed * Time.deltaTime;
    }

    private void UpdateLockAndLure()
    {
        Animal nearest = ParkGameManager.Instance != null
            ? ParkGameManager.Instance.GetNearestFreeAnimal(transform.position, lockRange)
            : null;

        if (nearest != locked)
        {
            if (locked != null) locked.SetLocked(false);
            locked = nearest;
            if (locked != null) locked.SetLocked(true);
        }

        var kb = Keyboard.current;
        bool luring = kb != null && kb.spaceKey.isPressed;

        if (locked != null && luring)
        {
            bool full = locked.ApplyLure(lureSpeed * Time.deltaTime);
            if (full) CatchAnimal(locked);
        }
    }

    private void CatchAnimal(Animal a)
    {
        // 첫 동물은 플레이어를, 이후엔 줄의 맨 뒤 동물을 따라간다(줄줄이)
        Transform target = caughtChain.Count == 0 ? transform : caughtChain[caughtChain.Count - 1].transform;
        a.Catch(target);
        caughtChain.Add(a);
        if (locked == a) locked = null;
        ParkGameManager.Instance?.OnAnimalCaught(a);
    }
}
