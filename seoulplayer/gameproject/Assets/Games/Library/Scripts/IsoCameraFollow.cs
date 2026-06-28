using UnityEngine;
using UnityEngine.InputSystem;

// 로블록스식 3인칭 카메라: 플레이어를 축으로
//   · 왼쪽 버튼 드래그 → 회전
//   · 스크롤 휠        → 확대/축소
// 회전은 즉각 반응(1:1), 위치만 부드럽게 따라간다.
public class IsoCameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [Tooltip("시작 위치 오프셋(위+뒤). 시작 거리/각도를 정한다.")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 24f, -24f);
    [SerializeField] private float pitch = 45f;
    [SerializeField] private float yaw = 0f;
    [SerializeField] private float followSmooth = 12f;   // 위치 추적 부드러움

    [Header("회전(왼쪽 버튼 드래그)")]
    [Tooltip("마우스 픽셀당 회전 각도(로블록스 비슷하게 ~0.3)")]
    [SerializeField] private float sensitivity = 0.3f;
    [SerializeField] private float minPitch = 10f;       // 위아래 각도 제한
    [SerializeField] private float maxPitch = 80f;

    [Header("줌(스크롤)")]
    [SerializeField] private float zoomStep = 5f;        // 휠 한 칸당 거리 변화
    [SerializeField] private float minDistance = 8f;
    [SerializeField] private float maxDistance = 90f;

    private float distance;
    private LibraryExplorer explorer;

    private void Start()
    {
        distance = offset.magnitude > 0.01f ? offset.magnitude : 24f;
        if (target != null) explorer = target.GetComponent<LibraryExplorer>();
        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    private void LateUpdate()
    {
        if (target == null) return;
        var mouse = Mouse.current;
        bool canLook = explorer == null || explorer.CanMove; // 책찾기 중엔 잠금

        if (canLook && mouse != null)
        {
            // 왼쪽 버튼 드래그로 회전(즉각 반응). 책찾기 중엔 canLook=false라 책 클릭과 충돌 없음
            if (mouse.leftButton.isPressed)
            {
                Vector2 d = mouse.delta.ReadValue();
                yaw += d.x * sensitivity;
                pitch = Mathf.Clamp(pitch - d.y * sensitivity, minPitch, maxPitch);
            }
            // 스크롤 줌
            float scroll = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) > 0.01f)
                distance = Mathf.Clamp(distance - (scroll / 120f) * zoomStep, minDistance, maxDistance);
        }

        // 플레이어를 축으로 회전/줌
        Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 desired = target.position + rot * new Vector3(0f, 0f, -distance);
        transform.position = Vector3.Lerp(transform.position, desired, followSmooth * Time.deltaTime);
        transform.rotation = rot;
    }

    public void SetTarget(Transform t)
    {
        target = t;
        explorer = t != null ? t.GetComponent<LibraryExplorer>() : null;
    }
}
