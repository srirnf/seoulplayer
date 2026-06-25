using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// NotoSansKR-Regular.ttf 를 TMP 폰트(Dynamic)로 변환하고,
// TMP 기본 폰트로 지정 + 현재 씬의 모든 TMP 텍스트에 적용한다.
// → 한글 깨짐(□) 해결. TMP Essentials를 먼저 Import 한 상태여야 함.
public static class KoreanFontSetup
{
    [MenuItem("서울플레이업/한글 폰트 적용")]
    public static void Apply()
    {
        // 경로 하드코딩 대신 "이름으로" 폰트를 찾는다 (한글 폴더 경로 문제 회피)
        Font font = null;
        string ttfPath = null;
        foreach (var guid in AssetDatabase.FindAssets("NotoSansKR-Regular t:Font"))
        {
            ttfPath = AssetDatabase.GUIDToAssetPath(guid);
            font = AssetDatabase.LoadAssetAtPath<Font>(ttfPath);
            if (font != null) break;
        }
        if (font == null)
        {
            EditorUtility.DisplayDialog("오류",
                "NotoSansKR-Regular.ttf 를 못 찾았어요.\n" +
                "1) Games/_공통/Fonts 에 폰트가 있는지\n" +
                "2) Git LFS 로 폰트를 받았는지 (git lfs pull)\n" +
                "확인하세요.", "확인");
            return;
        }

        // 폰트와 같은 폴더에 TMP 폰트 에셋 생성
        string fontDir = System.IO.Path.GetDirectoryName(ttfPath).Replace("\\", "/");
        string fontAssetPath = fontDir + "/NotoSansKR SDF.asset";

        // 1) TMP 폰트 에셋 생성(없으면)
        var fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontAssetPath);
        if (fontAsset == null)
        {
            fontAsset = TMP_FontAsset.CreateFontAsset(font); // Dynamic - 한글을 실시간 렌더
            AssetDatabase.CreateAsset(fontAsset, fontAssetPath);

            // 머티리얼/아틀라스를 서브에셋으로 저장해야 폰트가 정상 보존됨
            if (fontAsset.material != null)
            {
                fontAsset.material.name = "NotoSansKR SDF Material";
                AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
            }
            if (fontAsset.atlasTextures != null)
            {
                foreach (var tex in fontAsset.atlasTextures)
                    if (tex != null) AssetDatabase.AddObjectToAsset(tex, fontAsset);
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(fontAssetPath);
            Debug.Log("[KoreanFontSetup] TMP 폰트 에셋 생성: " + fontAssetPath);
        }

        // 2) TMP 기본 폰트로 지정 (앞으로 새로 만드는 텍스트도 한글 OK)
        SetAsDefaultFont(fontAsset);

        // 3) 현재 열린 씬의 모든 TMP 텍스트에 적용
        int count = 0;
        foreach (var t in Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            t.font = fontAsset;
            EditorUtility.SetDirty(t);
            count++;
        }
        EditorSceneManager.MarkAllScenesDirty();
        EditorSceneManager.SaveOpenScenes();

        EditorUtility.DisplayDialog("완료",
            $"한글 폰트 적용 완료!\n\n- TMP 기본 폰트 = NotoSansKR\n- 현재 씬 텍스트 {count}개에 적용\n\n" +
            "※ 다른 씬도 고치려면 그 씬을 열고 이 메뉴를 한 번 더 누르세요.", "확인");
    }

    private static void SetAsDefaultFont(TMP_FontAsset fontAsset)
    {
        var settings = Resources.Load<TMP_Settings>("TMP Settings");
        if (settings == null)
        {
            Debug.LogWarning("[KoreanFontSetup] TMP Settings 없음 - 'Window > TextMeshPro > Import TMP Essential Resources' 먼저 실행하세요.");
            return;
        }
        var so = new SerializedObject(settings);
        var prop = so.FindProperty("m_defaultFontAsset");
        if (prop != null)
        {
            prop.objectReferenceValue = fontAsset;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
        }
    }
}
