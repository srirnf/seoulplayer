using UnityEngine;

public class CameraFollowbookhansan : MonoBehaviour
{
    public Transform player; // 따라갈 캐릭터
    public Vector3 offset = new Vector3(0, 0, -10); // 카메라와 캐릭터 사이의 거리

    // 기존에 중복되어 있던 Start, Update, LateUpdate를 다 지우고 딱 이것만 남겨야 합니다!
    void LateUpdate()
    {
        if (player != null)
        {
            transform.position = player.position + offset;
        }
    }
}