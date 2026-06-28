using UnityEngine;

public class Stain : MonoBehaviour
{
    public int hp = 1; // 지우기 위해 필요한 클릭 횟수
    private bool isPlayerTouching = false; // 청소솔 털 부분이 닿아있는지 체크

    void Start()
    {
        // 프리팹 원본 이름에 "Bird"(새똥)가 들어가 있다면 맷집을 2로 설정합니다.
        if (gameObject.name.Contains("Bird"))
        {
            hp = 2;
        }
    }

    void Update()
    {
        // 털 부분이 얼룩에 닿아있고 + 마우스 왼쪽 버튼을 누른 순간!
        if (isPlayerTouching && Input.GetMouseButtonDown(0))
        {
            hp--; // 체력을 1 깎음

            // 체력이 0이 되어 완벽히 닦였다면?
            if (hp <= 0)
            {
                if (GameManager.instance != null)
                {
                    // [점수 판정] 새똥은 2점, 일반 얼룩은 1점을 줍니다!
                    if (gameObject.name.Contains("Bird"))
                    {
                        GameManager.instance.AddScore(2);
                    }
                    else
                    {
                        GameManager.instance.AddScore(1);
                    }
                }
                
                // 얼룩 삭제
                Destroy(gameObject);
            }
        }
    }

    // 얼룩이 닦이지 않고 화면 위쪽(Y: 6.0)을 넘어가 버리면 자동 삭제 (점수 변동 없음)
    void LateUpdate()
    {
        if (transform.position.y > 6.0f)
        {
            Destroy(gameObject);
        }
    }

    // 청소솔의 Collider(털 부분)가 들어왔을 때 조준 상태 ON
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.name == "player")
        {
            isPlayerTouching = true;
        }
    }

    // 청소솔이 조준 범위를 벗어났을 때 조준 상태 OFF
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.name == "player")
        {
            isPlayerTouching = false;
        }
    }
}