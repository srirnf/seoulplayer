using UnityEngine;

[System.Serializable]
public class LandmarkData
{
    public string landmarkId;
    public string landmarkName;

    [TextArea(2, 5)]
    public string description;

    public string sceneToLoad;
    public bool isUnlocked = true;
}
