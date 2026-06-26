using UnityEngine;

// 가로로 긴 배경에서 카메라가 플레이어를 좌우로 따라간다(횡스크롤).
// minX/maxX 범위를 벗어나지 않게 클램프해서 배경 밖이 안 보이게 한다.
public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float minX;
    [SerializeField] private float maxX;
    [SerializeField] private float smooth = 6f;

    private void LateUpdate()
    {
        if (target == null) return;
        float clampedX = Mathf.Clamp(target.position.x, minX, maxX);
        Vector3 p = transform.position;
        p.x = Mathf.Lerp(p.x, clampedX, smooth * Time.deltaTime);
        transform.position = p;
    }

    public void Setup(Transform follow, float min, float max)
    {
        target = follow;
        minX = min;
        maxX = max;
    }
}
