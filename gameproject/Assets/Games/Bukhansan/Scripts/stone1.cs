using UnityEngine;

public class stone1 : MonoBehaviour
{
    void Update()
    {
        if (transform.position.y <= -10f)
        {
            Destroy(gameObject);
        }
    }
}