using UnityEngine;
using System.Collections;

public class Spawner : MonoBehaviour
{
    public GameObject rockPrefab;

    public float interval = 0.5f;   // 생성 간격(초)

    public float minX = -55f;
    public float maxX = 55f;

    public float minY = 110f;
    public float maxY = 110f;

    void Start()
    {
        StartCoroutine(SpawnRock());
    }

    IEnumerator SpawnRock()
{
    while (true)
    {
        Vector3 pos = new Vector3(
            Random.Range(minX, maxX),
            Random.Range(minY, maxY),
            -1
        );
        //Debug.Log(rockPrefab);

        GameObject clone = Instantiate(rockPrefab, pos, Quaternion.identity);
        //Debug.Log("생성됨: " + clone.name);

        yield return new WaitForSeconds(interval);
    }
}
}