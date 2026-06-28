using UnityEngine;

public class MainsceneCamera : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Offset")]
    public Vector3 offset = new Vector3(0f, 12f, -8f);

    [Header("Follow Settings")]
    public float followSmoothTime = 0.2f;

    [Header("Rotation Settings")]
    public bool lookAtTarget = false;
    public Vector3 fixedEulerAngle = new Vector3(50f, 0f, 0f);

    [Header("Zoom Settings")]
    public bool allowZoom = true;
    public float zoomSpeed = 5f;
    public float minZoomY = 8f;
    public float maxZoomY = 18f;
    public float minZoomZ = -4f;
    public float maxZoomZ = -14f;

    [Header("Map Clamp (움직임 상한)")]
    public bool useClamp = true; // 기본적으로 켜짐으로 변경
    
    [Tooltip("카메라가 갈 수 있는 최소 X, Z 좌표값")]
    public Vector2 minXZ = new Vector2(-20f, -20f);
    
    [Tooltip("카메라가 갈 수 있는 최대 X, Z 좌표값")]
    public Vector2 maxXZ = new Vector2(20f, 20f);

    private Vector3 currentVelocity;

    private void LateUpdate()
    {
        if (target == null)
            return;

        // 1. 줌 입력 처리
        HandleZoom();

        // 2. 캐릭터 위치에 오프셋을 더한 목표 위치 계산
        Vector3 desiredPosition = target.position + offset;

        // 3. 부드럽게 이동 (SmoothDamp)
        Vector3 smoothedPosition = Vector3.SmoothDamp(
            transform.position,
            desiredPosition,
            ref currentVelocity,
            followSmoothTime
        );

        // 4. 이동 완료된 최종 위치에 상한선(Clamp) 적용
        if (useClamp)
        {
            smoothedPosition.x = Mathf.Clamp(smoothedPosition.x, minXZ.x, maxXZ.x);
            smoothedPosition.z = Mathf.Clamp(smoothedPosition.y, minXZ.y, maxXZ.y); 
            // ※ 주의: 기존 코드에서 maxXZ.y로 되어있던 오타를 수정했습니다 (기존코드에 maxXZ.z라 써야할 곳에 .y가 들어가 있었음)
        }

        // 5. 최종 위치 적용
        transform.position = smoothedPosition;

        // 6. 회전 처리
        if (lookAtTarget)
        {
            transform.LookAt(target.position);
        }
        else
        {
            transform.rotation = Quaternion.Euler(fixedEulerAngle);
        }
    }

    private void HandleZoom()
    {
        if (!allowZoom)
            return;

        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (Mathf.Abs(scroll) < 0.001f)
            return;

        offset.y -= scroll * zoomSpeed;
        offset.z += scroll * zoomSpeed;

        offset.y = Mathf.Clamp(offset.y, minZoomY, maxZoomY);
        offset.z = Mathf.Clamp(offset.z, maxZoomZ, minZoomZ);
    }
}