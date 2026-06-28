using UnityEngine;

public class PigeonSpawner : MonoBehaviour
{
    public GameObject pigeonPrefab; 
    public float spawnInterval = 2.0f; // 꼼수를 방지하기 위해 출격 간격을 2초로 살짝 줄여 긴장감을 줍니다!

    void Start()
    {
        InvokeRepeating("SpawnPigeon", 1.5f, spawnInterval);
    }

    void SpawnPigeon()
    {
        if (pigeonPrefab == null) return;

        bool isRightSide = Random.value > 0.5f;
        float spawnX = isRightSide ? 8.0f : -8.0f;

        // [수정] 청소솔이 움직이는 위아래 전 구역(-4.0 ~ 4.0)에서 무작위로 태어납니다!
        // 이제 한 곳에 가만히 서 있으면 무조건 무작위로 지나가는 비둘기한테 걸리게 됩니다.
        float spawnY = Random.Range(-4.0f, 4.0f);

        Vector3 spawnPosition = new Vector3(spawnX, spawnY, 0f);

        GameObject newPigeon = Instantiate(pigeonPrefab, spawnPosition, Quaternion.identity);

        Pigeon pigeonScript = newPigeon.GetComponent<Pigeon>();
        if (pigeonScript != null)
        {
            pigeonScript.SetDirection(spawnX);
        }
    }
}