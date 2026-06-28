using UnityEngine;

public class LandmarkInteractable : MonoBehaviour
{
    [Header("Landmark Settings")]
    public LandmarkData data;

    [Header("Interaction Point")]
    public Transform interactionPoint;

    public string LandmarkId => data != null ? data.landmarkId : "";
    public string LandmarkName => data != null ? data.landmarkName : "";
    public string Description => data != null ? data.description : "";
    public string SceneToLoad => data != null ? data.sceneToLoad : "";
    public bool IsUnlocked => data != null && data.isUnlocked;

    public Vector3 GetInteractionPosition()
    {
        if (interactionPoint != null)
            return interactionPoint.position;

        return transform.position;
    }

    public void Interact(Vector3 playerPosition)
    {
        if (data == null)
        {
            Debug.LogWarning($"{name}: LandmarkData가 없습니다.");
            return;
        }

        if (!data.isUnlocked)
        {
            Debug.Log($"{data.landmarkName}은(는) 아직 잠겨 있습니다.");
            return;
        }

        if (SceneTransitionManager.Instance == null)
        {
            Debug.LogWarning("SceneTransitionManager가 없습니다.");
            return;
        }

        SceneTransitionManager.Instance.LoadMiniGame(
            data.sceneToLoad,
            data.landmarkId,
            playerPosition
        );
    }
}
