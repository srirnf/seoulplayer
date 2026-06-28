using UnityEngine;

public class BackgroundScroll : MonoBehaviour
{
    // 배경이 위로 올라가는 속도
    public float scrollSpeed = 2.0f;
    // 배경이 다시 아래로 리셋될 기준 위치 (Scene 창을 보며 조절 가능)
    public float resetPositionY = 10.0f; 

    void Update()
    {
        // 배경을 Y축 방향(위쪽)으로 매 프레임 이동시킵니다.
        transform.Translate(Vector3.up * scrollSpeed * Time.deltaTime);

        // 배경이 너무 위로 올라가서 화면을 벗어나면, 다시 아래쪽으로 순간이동 시킵니다.
        if (transform.position.y >= resetPositionY)
        {
            // 원래 위치보다 아래쪽으로 배경 2개 높이만큼 내려줍니다.
            transform.position = new Vector3(transform.position.x, transform.position.y - (resetPositionY * 2), transform.position.z);
        }
    }
}