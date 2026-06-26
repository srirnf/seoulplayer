using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// 플레이어에 붙음. InteractZone 근처에서 [E]를 누르면 책 찾기 화면을 연다.
// 플레이어에 (이동용 CharacterController와 별개로) Is Trigger Collider + Rigidbody(Is Kinematic) 필요.
public class LibraryInteract : MonoBehaviour
{
    [SerializeField] private FindBookScreen findScreen;
    [SerializeField] private GameObject prompt;      // "[E] 책 찾기" 안내 UI
    [SerializeField] private LibraryExplorer explorer;

    private readonly HashSet<InteractZone> near = new HashSet<InteractZone>();

    private void Update()
    {
        bool canInteract = near.Count > 0 && (findScreen == null || !findScreen.IsOpen);
        if (prompt) prompt.SetActive(canInteract);

        var kb = Keyboard.current;
        if (canInteract && kb != null && kb.eKey.wasPressedThisFrame)
        {
            if (explorer) explorer.CanMove = false;
            findScreen.Open(this);
        }
    }

    // 화면이 닫히면 호출됨
    public void OnScreenClosed()
    {
        if (explorer) explorer.CanMove = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        var z = other.GetComponent<InteractZone>();
        if (z != null) near.Add(z);
    }

    private void OnTriggerExit(Collider other)
    {
        var z = other.GetComponent<InteractZone>();
        if (z != null) near.Remove(z);
    }
}
