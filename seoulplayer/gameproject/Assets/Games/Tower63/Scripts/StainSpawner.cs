using UnityEngine;

public class StainSpawner : MonoBehaviour
{
    // 프리팹 폴더에 저장한 얼룩 2개를 여기에 연결할 겁니다.
    public GameObject stainNormalPrefab;
    public GameObject stainBirdPrefab;

    // 현재 위로 움직이고 있는 배경 오브젝트들을 연결할 겁니다.
    public Transform background1;
    public Transform background2;

    // 얼룩이 생성되는 시간 간격 (1.5초마다 하나씩)
    public float spawnInterval = 1.5f;

    void Start()
    {
        // spawnInterval마다 SpawnStain 함수를 반복해서 실행합니다.
        InvokeRepeating("SpawnStain", 1.0f, spawnInterval);
    }

    void SpawnStain()
    {
        // 1. 일반 얼룩을 만들지, 새똥을 만들지 무작위로 결정
        GameObject selectedPrefab = (Random.value > 0.5f) ? stainNormalPrefab : stainBirdPrefab;

        if (selectedPrefab == null) return;

        // 2. 화면 하단(-6)부터 중간(0) 사이의 무작위 위치 계산
        float randomX = Random.Range(-3.0f, 3.0f);
        float randomY = Random.Range(-6.0f, 0.0f); // 이 줄이 반드시 위에 있어야 합니다!
        Vector3 spawnPosition = new Vector3(randomX, randomY, 0f);

        // 3. 얼룩을 실제로 화면에 생성
        GameObject newStain = Instantiate(selectedPrefab, spawnPosition, Quaternion.identity);

        // 4. 생성 시점에 더 아래쪽에 있는 배경을 찾아서 자식으로 등록
        if (background1.position.y < background2.position.y)
        {
            newStain.transform.SetParent(background1);
        }
        else
        {
            newStain.transform.SetParent(background2);
        }
    }
}