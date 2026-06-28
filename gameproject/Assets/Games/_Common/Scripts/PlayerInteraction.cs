using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    public KeyCode interactKey = KeyCode.E;
    private LandmarkInteractable currentLandmark;

    private void Update()
    {
        if (currentLandmark == null)
            return;

        if (Input.GetKeyDown(interactKey))
        {
            currentLandmark.Interact();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        LandmarkInteractable landmark = other.GetComponent<LandmarkInteractable>();

        if (landmark == null)
            landmark = other.GetComponentInParent<LandmarkInteractable>();

        if (landmark != null)
        {
            currentLandmark = landmark;
            Debug.Log("상호작용 가능: " + landmark.landmarkName);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        LandmarkInteractable landmark = other.GetComponent<LandmarkInteractable>();

        if (landmark == null)
            landmark = other.GetComponentInParent<LandmarkInteractable>();

        if (landmark != null && currentLandmark == landmark)
        {
            currentLandmark = null;
            Debug.Log("상호작용 해제: " + landmark.landmarkName);
        }
    }
}
