using UnityEngine;

public class stone1 : MonoBehaviour
{
    private Rigidbody rb;
    private Vector3 customGravity;
    void Update()
    {
        if (transform.position.y <= -10f)
        {
            Destroy(gameObject);
        }
    }
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        
        // 1. 유니티 기본 글로벌 중력의 영향을 받지 않도록 끕니다.
        rb.useGravity = false;

        // 2. 이 객체만의 고유한 Y축 중력 값을 랜덤으로 정합니다. (-5.0 ~ -20.0)
        float randomGravityY = Random.Range(-10.0f, -30.0f);
        customGravity = new Vector3(0, randomGravityY, 0);
    }

    void FixedUpdate()
    {
        // 3. 물리 연산 프레임마다 정해진 고유 중력을 아래로 가합니다.
        rb.AddForce(customGravity, ForceMode.Acceleration);
    }
}