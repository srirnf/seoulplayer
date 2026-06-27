using UnityEngine;

public class bukhansangameui : MonoBehaviour
{
    // 타이머 스크립트 연결용
    public GameTimer gameTimer; 

    void Update()
    {
        // 스페이스바를 누르면
        if (Input.GetKeyDown(KeyCode.Space))
        {
            HideTutorial();
        }
    }

    void HideTutorial()
    {
        // 일반 2D/3D 오브젝트의 Y 위치를 -90으로 즉시 이동시킵니다.
        // X와 Z 위치는 현재 오브젝트의 위치를 그대로 유지합니다.
        transform.position = new Vector3(transform.position.x, -90f, transform.position.z);
        
        // 타이머 시작 함수 호출
        if (gameTimer != null)
        {
            gameTimer.StartTimer();
        }
        else
        {
            Debug.LogWarning("GameTimer가 인스펙터 창에서 연결되지 않았습니다!");
        }

        // 만약 화면 밖으로 치우는 것뿐만 아니라 아예 오브젝트를 숨기고 싶다면 아래 주석을 해제하세요.
        // gameObject.SetActive(false);
    }
}