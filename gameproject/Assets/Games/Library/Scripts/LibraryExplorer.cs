using UnityEngine;
using UnityEngine.InputSystem;

// 2.5D 탐험: 3D 도서관을 WASD로 걷는다. (카메라는 45도 내려다보는 뷰)
// CharacterController로 벽/책장과 충돌. 카메라 기준으로 이동 방향이 자연스럽게 맞춰진다.
[RequireComponent(typeof(CharacterController))]
public class LibraryExplorer : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float rotateSpeed = 12f;
    [Tooltip("이동 방향 기준 카메라(보통 Main Camera). 비우면 월드축 기준.")]
    [SerializeField] private Transform cam;

    public bool CanMove { get; set; } = true;

    private CharacterController cc;
    private float vy;

    private void Awake()
    {
        cc = GetComponent<CharacterController>();
        if (cam == null && Camera.main != null) cam = Camera.main.transform;
    }

    private void Update()
    {
        var kb = Keyboard.current;
        Vector3 input = Vector3.zero;
        if (CanMove && kb != null)
        {
            float x = (kb.dKey.isPressed ? 1f : 0f) - (kb.aKey.isPressed ? 1f : 0f);
            float z = (kb.wKey.isPressed ? 1f : 0f) - (kb.sKey.isPressed ? 1f : 0f);
            input = new Vector3(x, 0f, z);
            if (input.sqrMagnitude > 1f) input.Normalize();
        }

        // 카메라 기준 평면 방향
        Vector3 dir;
        if (cam != null)
        {
            Vector3 f = cam.forward; f.y = 0f; f.Normalize();
            Vector3 r = cam.right; r.y = 0f; r.Normalize();
            dir = f * input.z + r * input.x;
        }
        else dir = input;

        Vector3 vel = dir * moveSpeed;

        if (cc.isGrounded && vy < 0f) vy = -2f;
        vy += gravity * Time.deltaTime;
        vel.y = vy;
        cc.Move(vel * Time.deltaTime);

        if (dir.sqrMagnitude > 0.001f)
        {
            Quaternion target = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(transform.rotation, target, rotateSpeed * Time.deltaTime);
        }
    }
}
