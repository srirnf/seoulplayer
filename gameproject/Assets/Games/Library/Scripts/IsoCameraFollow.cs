using UnityEngine;

// 45도 내려다보는 카메라가 플레이어를 부드럽게 따라간다 (2.5D 느낌).
public class IsoCameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [Tooltip("플레이어 기준 카메라 위치 오프셋 (위+뒤). 45도면 y와 -z를 비슷하게.")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 12f, -12f);
    [SerializeField] private float pitch = 45f;
    [SerializeField] private float yaw = 0f;
    [SerializeField] private float smooth = 8f;

    private void Start()
    {
        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    private void LateUpdate()
    {
        if (target == null) return;
        Vector3 desired = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, desired, smooth * Time.deltaTime);
        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    public void SetTarget(Transform t) => target = t;
}
