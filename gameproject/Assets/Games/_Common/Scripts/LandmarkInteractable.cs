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
        // ❌ 기존에 바로 씬을 바꾸던 코드는 지우거나 주석 처리합니다.
        /*
        if (SceneTransitionManager.Instance == null)
        {
            Debug.LogWarning("SceneTransitionManager가 씬에 없습니다.");
            return;
        }
        SceneTransitionManager.Instance.LoadSceneByName(sceneToLoad);
        */

        // 🎯 [수정] 팝업창 매니저(LandmarkIntroUI)를 찾아 팝업창을 띄우게 만듭니다!
        if (LandmarkIntroUI.Instance != null)
        {
            // 인스펙터에 적어둔 이름, 설명, 이동할 씬 이름을 팝업창에 토스합니다.
            LandmarkIntroUI.Instance.ShowIntro(landmarkName, description, sceneToLoad);
            
            // 💡 마우스 커서가 팝업창 버튼을 클릭할 수 있도록 풀어줍니다.
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Debug.LogWarning("씬에 LandmarkIntroUI(싱글톤)가 존재하지 않습니다! 오브젝트를 확인해 주세요.");
        }
    }
}