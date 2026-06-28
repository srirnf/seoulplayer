using UnityEngine;
using UnityEngine.InputSystem;

// 플레이어에 붙음. InteractZone(콜라이더) 근처에서 [F]를 누르면 책 찾기 화면을 연다.
// 트리거 이벤트 대신 "거리(ClosestPoint)"로 검출해서 물리 설정 없이도 확실히 동작.
public class LibraryInteract : MonoBehaviour
{
    [SerializeField] private FindBookScreen findScreen;
    [SerializeField] private GameObject prompt;      // "[F] 책 찾기" 안내 UI
    [SerializeField] private LibraryExplorer explorer;
    [SerializeField] private float range = 2.5f;     // 콜라이더 표면에서 이 거리 안이면 상호작용 가능

    private InteractZone[] zones;

    private void Start()
    {
        zones = Object.FindObjectsByType<InteractZone>(FindObjectsSortMode.None);
    }

    private void Update()
    {
        bool canInteract = NearZone() && (findScreen == null || !findScreen.IsOpen);
        if (prompt) prompt.SetActive(canInteract);

        var kb = Keyboard.current;
        if (canInteract && kb != null && kb.fKey.wasPressedThisFrame)
        {
            if (explorer) explorer.CanMove = false;
            findScreen.Open(this);
        }
    }

    private bool NearZone()
    {
        if (zones == null) return false;
        Vector3 p = transform.position;
        foreach (var z in zones)
        {
            if (z == null) continue;
            var col = z.GetComponent<Collider>();
            if (col == null) continue;
            if ((col.ClosestPoint(p) - p).sqrMagnitude <= range * range) return true;
        }
        return false;
    }

    public void OnScreenClosed()
    {
        if (explorer) explorer.CanMove = true;
    }
}
