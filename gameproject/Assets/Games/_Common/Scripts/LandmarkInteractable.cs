using UnityEngine;

public class LandmarkInteractable : MonoBehaviour
{
    [Header("Landmark Info")]
    public string landmarkId;
    public string landmarkName;
    [TextArea(2, 5)] public string description;

    [Header("Scene Link")]
    public string sceneToLoad;

    public void Interact()
    {
        if (SceneTransitionManager.Instance == null)
        {
            Debug.LogWarning("SceneTransitionManager가 씬에 없습니다.");
            return;
        }

        SceneTransitionManager.Instance.LoadSceneByName(sceneToLoad);
    }
}
