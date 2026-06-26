using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// 메뉴 클릭으로 별마당도서관 2.5D 탐험 씬을 자동 구성한다.
// FBX 배치(콜라이더 생성) + 플레이어 + 45도 카메라 + 조명 + 책찾기 UI 까지.
// 생성 후 FBX 크기/위치와 책 격자(uv)만 살짝 맞추면 됨.
public static class LibraryExploreBuilder
{
    private const string GameDir = "Assets/Games/Library";
    private const string FbxPath = GameDir + "/Models/MultiStoryBookWall.fbx";
    private const string ShelfPng = GameDir + "/Sprites/bookshelf.png";
    private const string ScenePath = GameDir + "/Scenes/LibraryExplore.unity";

    [MenuItem("별마당도서관/플레이 가능한 씬 생성")]
    public static void Build()
    {
        Directory.CreateDirectory(GameDir + "/Scenes");
        AssetDatabase.Refresh();

        // 책장 이미지: RawImage용 Default 텍스처로
        var pngImp = AssetImporter.GetAtPath(ShelfPng) as TextureImporter;
        if (pngImp != null) { pngImp.textureType = TextureImporterType.Default; pngImp.SaveAndReimport(); }
        var shelfTex = AssetDatabase.LoadAssetAtPath<Texture2D>(ShelfPng);

        // FBX: 콜라이더 + 임베드 재질로 강제(예전 External 설정 덮어써서 경고/흰색 제거)
        var fbxImp = AssetImporter.GetAtPath(FbxPath) as ModelImporter;
        if (fbxImp != null)
        {
            fbxImp.addCollider = true;
            fbxImp.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            fbxImp.materialLocation = ModelImporterMaterialLocation.InPrefab;
            fbxImp.SaveAndReimport();
        }

        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // 조명
        var lightGO = GameObject.Find("Directional Light");
        if (lightGO == null)
        {
            lightGO = new GameObject("Directional Light");
            var l = lightGO.AddComponent<Light>(); l.type = LightType.Directional;
        }
        lightGO.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        // 바닥
        var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "Floor";
        floor.transform.localScale = new Vector3(10f, 1f, 10f);

        // FBX 배치
        var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(FbxPath);
        if (fbx != null)
        {
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(fbx);
            inst.name = "BookWall";
            inst.transform.position = new Vector3(0f, 0f, 12f);
            inst.transform.localScale = Vector3.one * 5f; // 모델이 작으면 키움(인스펙터에서 조정)
            FixToURP(inst); // 재질을 URP로 변환(분홍/안보임 방지)
            // 상호작용 트리거(모델 둘레 크게)
            var zone = new GameObject("InteractZone");
            zone.transform.position = new Vector3(0f, 2f, 9f);
            var bc = zone.AddComponent<BoxCollider>(); bc.isTrigger = true; bc.size = new Vector3(40f, 12f, 20f);
            zone.AddComponent<InteractZone>();
        }

        // 플레이어
        var player = new GameObject("Player");
        player.transform.position = new Vector3(0f, 0f, 0f);
        var cc = player.AddComponent<CharacterController>();
        cc.center = new Vector3(0f, 1f, 0f); cc.height = 2f; cc.radius = 0.4f;
        var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "Body"; body.transform.SetParent(player.transform, false);
        body.transform.localPosition = new Vector3(0f, 1f, 0f);
        Object.DestroyImmediate(body.GetComponent<Collider>());
        var explorer = player.AddComponent<LibraryExplorer>();
        var interact = player.AddComponent<LibraryInteract>();
        // 상호작용 감지용 트리거 + 키네마틱 리지드바디
        var trig = player.AddComponent<SphereCollider>(); trig.isTrigger = true; trig.radius = 1.8f; trig.center = new Vector3(0f, 1f, 0f);
        var rb = player.AddComponent<Rigidbody>(); rb.isKinematic = true; rb.useGravity = false;

        // 카메라
        var cam = Camera.main;
        if (cam == null) { var camGO = new GameObject("Main Camera"); cam = camGO.AddComponent<Camera>(); camGO.tag = "MainCamera"; }
        var follow = cam.gameObject.AddComponent<IsoCameraFollow>();
        SetRef(follow, "target", player.transform);
        SetVector(follow, "offset", new Vector3(0f, 18f, -18f)); // 큰 공간이라 멀리서

        // ===== 책찾기 UI =====
        var canvasGO = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGO.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920, 1080);
        new GameObject("EventSystem", typeof(EventSystem)).AddComponent<InputSystemUIInputModule>();

        // "[E] 책 찾기" 안내
        var prompt = MakePanel("Prompt", canvasGO.transform, new Color(0, 0, 0, 0.6f),
            new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 60), new Vector2(320, 70));
        MakeText("T", prompt.transform, "[ E ] 책 찾기", 34, Vector2.zero, Vector2.one, Color.white);

        // FindScreen 패널
        var screen = MakePanel("FindScreen", canvasGO.transform, new Color(0.05f, 0.04f, 0.03f, 0.96f),
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

        // 책장 이미지(가운데 크게) + 버튼 부모(같은 영역 덮기)
        var shelfGO = new GameObject("ShelfImage", typeof(RectTransform), typeof(RawImage));
        shelfGO.transform.SetParent(screen.transform, false);
        SetRect(shelfGO, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -30), new Vector2(1280, 853));
        var shelfRaw = shelfGO.GetComponent<RawImage>(); shelfRaw.texture = shelfTex;
        var buttonsParent = new GameObject("Buttons", typeof(RectTransform));
        buttonsParent.transform.SetParent(shelfGO.transform, false);
        var bpRT = buttonsParent.GetComponent<RectTransform>();
        bpRT.anchorMin = Vector2.zero; bpRT.anchorMax = Vector2.one; bpRT.offsetMin = Vector2.zero; bpRT.offsetMax = Vector2.zero;

        // 찾을 책(우상단) + 안내문
        var thumbGO = new GameObject("TargetThumb", typeof(RectTransform), typeof(RawImage));
        thumbGO.transform.SetParent(screen.transform, false);
        SetRect(thumbGO, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-60, -60), new Vector2(90, 150));
        var info = MakeText("Info", screen.transform, "이 책을 찾으세요!", 36,
            new Vector2(0, 1), new Vector2(1, 1), Color.white);
        SetRect(info.gameObject, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -40), new Vector2(-300, 70));
        info.alignment = TextAlignmentOptions.Left;

        // 결과 패널(시간 종료 → 점수)
        var resultP = MakePanel("ResultPanel", screen.transform, new Color(0.1f, 0.25f, 0.15f, 0.94f),
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        var resultT = MakeText("T", resultP.transform, "결과", 72, Vector2.zero, Vector2.one, Color.white);
        resultP.SetActive(false);

        var find = screen.AddComponent<FindBookScreen>();
        SetRef(find, "panel", screen);
        SetRef(find, "shelfImage", shelfRaw);
        SetRef(find, "buttonsParent", bpRT);
        SetRef(find, "targetThumb", thumbGO.GetComponent<RawImage>());
        SetRef(find, "infoText", info);
        SetRef(find, "resultPanel", resultP);
        SetRef(find, "resultText", resultT);
        screen.SetActive(false);

        // 플레이어 상호작용 연결
        SetRef(interact, "findScreen", find);
        SetRef(interact, "prompt", prompt);
        SetRef(interact, "explorer", explorer);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AddSceneToBuild(ScenePath);
        AssetDatabase.SaveAssets();

        EditorUtility.DisplayDialog("완료",
            "별마당도서관 2.5D 씬 생성 완료!\n\n" +
            "▶ Play → WASD 탐험 → 도서관 앞에서 E → 제한시간 내 책 많이 찾기\n\n" +
            "· FBX(BookWall) 크기/위치가 안 맞으면 인스펙터 Scale/Position 조정\n" +
            "· 책 42칸 격자가 이미지와 안 맞으면 FindScreen의 Cabinets(좌/우 uv) 조정\n" +
            "· 재질이 분홍/안보이면: FBX 선택 > Materials > Extract, 또는 URP 변환", "확인");
    }

    // FBX 재질이 URP에서 안 보이면, 텍스처를 가져와 새 URP/Lit 재질을 만들어 입힌다.
    private static void FixToURP(GameObject root)
    {
        var urp = Shader.Find("Universal Render Pipeline/Lit");
        if (urp == null) return;
        string matDir = GameDir + "/Materials";
        Directory.CreateDirectory(matDir);
        AssetDatabase.Refresh();

        var cache = new System.Collections.Generic.Dictionary<Material, Material>();
        foreach (var rend in root.GetComponentsInChildren<Renderer>(true))
        {
            var src = rend.sharedMaterials;
            var dst = new Material[src.Length];
            for (int i = 0; i < src.Length; i++)
            {
                var m = src[i];
                if (m == null) { dst[i] = null; continue; }
                if (m.shader != null && m.shader.name.Contains("Universal")) { dst[i] = m; continue; }
                if (!cache.TryGetValue(m, out var nm))
                {
                    nm = new Material(urp);
                    if (m.mainTexture != null) nm.SetTexture("_BaseMap", m.mainTexture);
                    if (m.HasProperty("_Color")) nm.SetColor("_BaseColor", m.GetColor("_Color"));
                    string path = AssetDatabase.GenerateUniqueAssetPath($"{matDir}/{m.name}_URP.mat");
                    AssetDatabase.CreateAsset(nm, path);
                    cache[m] = nm;
                }
                dst[i] = nm;
            }
            rend.sharedMaterials = dst;
        }
        AssetDatabase.SaveAssets();
    }

    // ---- 헬퍼 ----
    private static GameObject MakeBigLabel(string name, Transform parent, string text, Color bg)
    {
        var go = MakePanel(name, parent, bg, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        MakeText("T", go.transform, text, 72, Vector2.zero, Vector2.one, Color.white);
        go.SetActive(false);
        return go;
    }

    private static GameObject MakePanel(string name, Transform parent, Color color,
        Vector2 aMin, Vector2 aMax, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        SetRect(go, aMin, aMax, pivot, pos, size);
        go.GetComponent<Image>().color = color;
        return go;
    }

    private static TextMeshProUGUI MakeText(string name, Transform parent, string text, float size, Vector2 aMin, Vector2 aMax, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = aMin; rt.anchorMax = aMax; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = text; t.fontSize = size; t.alignment = TextAlignmentOptions.Center; t.color = color; t.raycastTarget = false;
        return t;
    }

    private static void SetRect(GameObject go, Vector2 aMin, Vector2 aMax, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = aMin; rt.anchorMax = aMax; rt.pivot = pivot; rt.anchoredPosition = pos; rt.sizeDelta = size;
    }

    private static void SetRef(Object comp, string field, Object value)
    {
        var so = new SerializedObject(comp);
        var sp = so.FindProperty(field);
        if (sp != null) { sp.objectReferenceValue = value; so.ApplyModifiedPropertiesWithoutUndo(); }
        else Debug.LogWarning($"[LibraryExploreBuilder] 필드 없음: {comp.GetType().Name}.{field}");
    }

    private static void SetVector(Object comp, string field, Vector3 v)
    {
        var so = new SerializedObject(comp);
        var sp = so.FindProperty(field);
        if (sp != null) { sp.vector3Value = v; so.ApplyModifiedPropertiesWithoutUndo(); }
    }

    private static void AddSceneToBuild(string path)
    {
        var list = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        if (list.Exists(s => s.path == path)) return;
        list.Add(new EditorBuildSettingsScene(path, true));
        EditorBuildSettings.scenes = list.ToArray();
    }
}
