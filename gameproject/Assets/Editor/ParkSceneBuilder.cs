using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// 메뉴 클릭으로 "서울대공원(동물꼬시기)" 플레이 가능한 씬을 통째로 생성한다.
// 임시 에셋(사육사/동물/우리/게이지)도 자동 생성해 연결한다. 진짜 에셋은 나중에 교체.
public static class ParkSceneBuilder
{
    private const string SpriteDir = "Assets/04_Sprites/_ParkPlaceholder";
    private const string ScenePath = "Assets/01_Scenes/ParkGame.unity";
    private const string BgPath = "Assets/04_Sprites/park_bg.png";

    [MenuItem("서울대공원/플레이 가능한 씬 생성")]
    public static void Build()
    {
        EnsureFolder("Assets/04_Sprites", "_ParkPlaceholder");
        EnsureFolder("Assets", "01_Scenes");

        Sprite playerS = MakeSprite("keeper", 48, 56, "#3f7bd6", "#274f8a");
        Sprite gaugeBg = MakeSprite("gauge_bg", 44, 10, "#222222", "#000000");
        Sprite gaugeFill = MakeSprite("gauge_fill", 44, 10, "#5fd06a", "#3a9c45", SpriteAlignment.LeftCenter);
        Sprite lockS = MakeSprite("lock_ring", 64, 72, "#ffd54a", "#e0a615");

        var animals = new (string name, string hex, string dark)[]
        {
            ("호랑이", "#e8954a", "#a8632a"), ("토끼", "#e9e0d2", "#c2b39a"),
            ("원숭이", "#9b6b43", "#67462c"), ("코끼리", "#9aa0a6", "#646a70"),
            ("펭귄", "#3a4250", "#20252e"),
        };

        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // 배경(가로로 긴 어린이대공원)
        Sprite bg = ImportSprite(BgPath);
        float bgW = 40f, bgH = 8f;
        if (bg != null) { bgW = bg.bounds.size.x; bgH = bg.bounds.size.y; }
        float halfH = bgH * 0.5f;
        float halfW = bgW * 0.5f;

        var bgGO = new GameObject("Background");
        var bgsr = bgGO.AddComponent<SpriteRenderer>();
        bgsr.sprite = bg;
        bgsr.sortingOrder = -10;

        Camera cam = Camera.main;
        float camSize = halfH;
        float halfView = camSize * (16f / 9f);
        if (cam != null)
        {
            cam.orthographic = true;
            cam.orthographicSize = camSize;
            cam.transform.position = new Vector3(-halfW + halfView, 0f, -10f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.62f, 0.82f, 0.95f);
        }

        // 플레이 가능한 바닥 영역(배경 아래쪽 길 부근)
        float playY = -halfH + bgH * 0.22f;
        float leftEdge = -halfW + 1.5f;
        float rightEdge = halfW - 1.5f;

        // 우리 위치(보이지 않는 목표점 - 배경에 그려진 울타리 부근)
        var cagePoints = new List<Object>();
        for (int i = 0; i < animals.Length; i++)
        {
            float x = Mathf.Lerp(leftEdge, rightEdge, (i + 0.5f) / animals.Length);
            var cage = new GameObject($"CagePoint_{i}");
            cage.transform.position = new Vector3(x, playY + bgH * 0.18f, 0f);
            cagePoints.Add(cage.transform);
        }

        // 플레이어(왼쪽에서 시작)
        var player = new GameObject("Player");
        player.transform.position = new Vector3(-halfW + halfView, playY, 0f);
        var psr = player.AddComponent<SpriteRenderer>();
        psr.sprite = playerS;
        psr.sortingOrder = 2;
        player.AddComponent<AnimalCatcher>();

        // 카메라가 플레이어를 좌우로 따라가도록
        if (cam != null)
        {
            var follow = cam.gameObject.AddComponent<CameraFollow>();
            follow.Setup(player.transform, -halfW + halfView, halfW - halfView);
        }

        // 동물 5마리(배경 전체에 분산)
        for (int i = 0; i < animals.Length; i++)
        {
            float x = Mathf.Lerp(leftEdge, rightEdge, (i + 0.5f) / animals.Length);
            Sprite body = MakeSprite($"animal_{i}_{animals[i].name}", 46, 46, animals[i].hex, animals[i].dark);
            BuildAnimal(animals[i].name, new Vector3(x, playY, 0f), body, lockS, gaugeBg, gaugeFill);
        }

        // 매니저
        var gmGO = new GameObject("ParkGameManager");
        var gm = gmGO.AddComponent<ParkGameManager>();
        SetArray(gm, "cagePoints", cagePoints.ToArray());

        // Canvas + EventSystem (신 Input System)
        var canvasGO = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        new GameObject("EventSystem", typeof(EventSystem)).AddComponent<InputSystemUIInputModule>();

        var caughtText = MakeText("CaughtText", canvasGO.transform, "동물 0 / 5", 46, TextAlignmentOptions.TopLeft,
            new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(40, -30), new Vector2(420, 70), Color.black);
        var timerText = MakeText("TimerText", canvasGO.transform, "0.0초", 46, TextAlignmentOptions.TopRight,
            new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-40, -30), new Vector2(360, 70), Color.black);
        MakeText("Hint", canvasGO.transform, "← → (A/D) 이동 · 가까운 동물에 Space 꾹 눌러 꼬시기", 30, TextAlignmentOptions.Bottom,
            new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 30), new Vector2(960, 50), new Color(0.1f, 0.1f, 0.1f));

        var ending = MakePanel("EndingTransition", canvasGO.transform, new Color(0f, 0f, 0f, 0.6f),
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        MakeText("Label", ending.transform, "동물들을 우리로 데려다 주는 중...", 50, TextAlignmentOptions.Center,
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, Color.white);

        var clearPanel = MakePanel("ClearPanel", canvasGO.transform, new Color(0.1f, 0.3f, 0.12f, 0.9f),
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        MakeText("Title", clearPanel.transform, "모두 잘 보냈어요!", 70, TextAlignmentOptions.Center,
            new Vector2(0, 0.5f), new Vector2(1, 1), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, Color.white);
        var clearTime = MakeText("ClearTime", clearPanel.transform, "클리어 시간: 0.0초", 44, TextAlignmentOptions.Center,
            new Vector2(0, 0), new Vector2(1, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, Color.white);

        var uiGO = new GameObject("ParkUIManager");
        var ui = uiGO.AddComponent<ParkUIManager>();
        SetRef(ui, "caughtText", caughtText);
        SetRef(ui, "timerText", timerText);
        SetRef(ui, "endingTransition", ending);
        SetRef(ui, "clearPanel", clearPanel);
        SetRef(ui, "clearTimeText", clearTime);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AddSceneToBuildSettings(ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("완료",
            "서울대공원(동물꼬시기) 플레이 씬 생성 완료!\n\n" +
            "01_Scenes/ParkGame 이 열려 있습니다. ▶ Play!\n\n" +
            "WASD 이동 → 가까운 동물 자동 록온 → Space 꾹 눌러 꼬시기\n" +
            "다 모으면 동물들이 우리로 들어가는 엔딩!", "확인");
        Debug.Log("[ParkSceneBuilder] 씬 생성 완료: " + ScenePath);
    }

    private static void BuildAnimal(string name, Vector3 pos, Sprite body, Sprite lockS, Sprite gaugeBg, Sprite gaugeFill)
    {
        var go = new GameObject("Animal_" + name);
        go.transform.position = pos;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = body;
        sr.sortingOrder = 0;
        var animal = go.AddComponent<Animal>();

        // 록온 링(동물 뒤)
        var ring = new GameObject("LockRing");
        ring.transform.SetParent(go.transform, false);
        var rsr = ring.AddComponent<SpriteRenderer>();
        rsr.sprite = lockS;
        rsr.sortingOrder = -1;
        rsr.color = new Color(1f, 1f, 1f, 0.7f);
        ring.SetActive(false);

        // 유인 게이지(동물 위)
        var gaugeRoot = new GameObject("Gauge");
        gaugeRoot.transform.SetParent(go.transform, false);
        gaugeRoot.transform.localPosition = new Vector3(0f, 0.42f, 0f);

        var bg = new GameObject("Bg");
        bg.transform.SetParent(gaugeRoot.transform, false);
        var bgsr = bg.AddComponent<SpriteRenderer>();
        bgsr.sprite = gaugeBg;
        bgsr.sortingOrder = 5;

        var fill = new GameObject("Fill");
        fill.transform.SetParent(gaugeRoot.transform, false);
        fill.transform.localPosition = new Vector3(-0.22f, 0f, 0.01f); // bg 폭 44px=0.44, 왼쪽 끝
        var fillsr = fill.AddComponent<SpriteRenderer>();
        fillsr.sprite = gaugeFill;
        fillsr.sortingOrder = 6;

        gaugeRoot.SetActive(false);

        SetRef(animal, "lockIndicator", ring);
        SetRef(animal, "gaugeRoot", gaugeRoot);
        SetRef(animal, "gaugeFill", fill.transform);
    }

    // ---------- 헬퍼 ----------

    private static GameObject MakePanel(string name, Transform parent, Color color,
        Vector2 aMin, Vector2 aMax, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        SetRect(go, aMin, aMax, pivot, pos, size);
        go.GetComponent<Image>().color = color;
        return go;
    }

    private static TextMeshProUGUI MakeText(string name, Transform parent, string text, float size,
        TextAlignmentOptions align, Vector2 aMin, Vector2 aMax, Vector2 pivot, Vector2 pos, Vector2 sizeDelta, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        SetRect(go, aMin, aMax, pivot, pos, sizeDelta);
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = text;
        t.fontSize = size;
        t.alignment = align;
        t.color = color;
        t.raycastTarget = false;
        return t;
    }

    private static void SetRect(GameObject go, Vector2 aMin, Vector2 aMax, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = aMin;
        rt.anchorMax = aMax;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    private static Sprite MakeSprite(string name, int w, int h, string fillHex, string borderHex,
        SpriteAlignment align = SpriteAlignment.Center)
    {
        string path = $"{SpriteDir}/{name}.png";
        ColorUtility.TryParseHtmlString(fillHex, out Color fill);
        ColorUtility.TryParseHtmlString(borderHex, out Color border);

        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        var px = new Color[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                bool edge = x < 3 || x >= w - 3 || y < 3 || y >= h - 3;
                px[y * w + x] = edge ? border : fill;
            }
        tex.SetPixels(px);
        tex.Apply();
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        var imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (imp != null)
        {
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.spritePixelsPerUnit = 100;
            imp.filterMode = FilterMode.Point;
            var s = new TextureImporterSettings();
            imp.ReadTextureSettings(s);
            s.spriteAlignment = (int)align;
            imp.SetTextureSettings(s);
            imp.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    // 이미 존재하는 PNG(배경)를 Sprite로 임포트
    private static Sprite ImportSprite(string path)
    {
        if (!File.Exists(path))
        {
            Debug.LogWarning("[ParkSceneBuilder] 배경 파일 없음: " + path);
            return null;
        }
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        var imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (imp != null)
        {
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.spritePixelsPerUnit = 100;
            imp.maxTextureSize = 8192;   // 5472px 폭 유지
            imp.mipmapEnabled = false;
            imp.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static void EnsureFolder(string parent, string child)
    {
        if (!AssetDatabase.IsValidFolder($"{parent}/{child}"))
            AssetDatabase.CreateFolder(parent, child);
    }

    private static void AddSceneToBuildSettings(string path)
    {
        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        if (scenes.Exists(s => s.path == path)) return;
        scenes.Add(new EditorBuildSettingsScene(path, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static void SetRef(Object comp, string field, Object value)
    {
        var so = new SerializedObject(comp);
        var sp = so.FindProperty(field);
        if (sp == null) { Debug.LogWarning($"[ParkSceneBuilder] 필드 못 찾음: {comp.GetType().Name}.{field}"); return; }
        sp.objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetArray(Object comp, string field, Object[] values)
    {
        var so = new SerializedObject(comp);
        var sp = so.FindProperty(field);
        if (sp == null) { Debug.LogWarning($"[ParkSceneBuilder] 배열 필드 못 찾음: {field}"); return; }
        sp.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
            sp.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
