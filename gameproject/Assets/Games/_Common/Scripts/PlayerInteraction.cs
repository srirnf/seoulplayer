using System.Collections.Generic;
using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Interaction Settings")]
    public KeyCode interactKey = KeyCode.E;

    private readonly List<LandmarkInteractable> landmarksInRange = new List<LandmarkInteractable>();
    private LandmarkInteractable currentLandmark;

    private void Update()
    {
        UpdateNearestLandmark();
        UpdateUI();
        HandleInteractionInput();
    }

    private void UpdateNearestLandmark()
    {
        float closestDistance = float.MaxValue;
        LandmarkInteractable nearest = null;

        for (int i = landmarksInRange.Count - 1; i >= 0; i--)
        {
            if (landmarksInRange[i] == null)
            {
                landmarksInRange.RemoveAt(i);
                continue;
            }

            float distance = Vector3.Distance(
                transform.position,
                landmarksInRange[i].GetInteractionPosition()
            );

            if (distance < closestDistance)
            {
                closestDistance = distance;
                nearest = landmarksInRange[i];
            }
        }

        currentLandmark = nearest;
    }

    private void UpdateUI()
    {
        if (InteractionPopupUI.Instance == null)
            return;

        if (currentLandmark == null)
        {
            InteractionPopupUI.Instance.Hide();
            return;
        }

        InteractionPopupUI.Instance.Show(
            currentLandmark.LandmarkName,
            currentLandmark.Description,
            currentLandmark.IsUnlocked
        );
    }

    private void HandleInteractionInput()
    {
        if (currentLandmark == null)
            return;

        if (InteractionPopupUI.Instance != null && InteractionPopupUI.Instance.IsConfirmOpen)
            return;

        if (Input.GetKeyDown(interactKey))
        {
            InteractionPopupUI.Instance?.OpenConfirm(currentLandmark, transform.position);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        LandmarkInteractable landmark = other.GetComponent<LandmarkInteractable>();

        if (landmark != null && !landmarksInRange.Contains(landmark))
        {
            landmarksInRange.Add(landmark);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        LandmarkInteractable landmark = other.GetComponent<LandmarkInteractable>();

        if (landmark != null && landmarksInRange.Contains(landmark))
        {
            landmarksInRange.Remove(landmark);
        }

        if (landmarksInRange.Count == 0 && InteractionPopupUI.Instance != null)
        {
            InteractionPopupUI.Instance.Hide();
        }
    }
}

