using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
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

        Sprite playerS = MakeSprite("keeper", 70, 130, "#3f7bd6", "#274f8a", SpriteAlignment.BottomCenter);
        Sprite trackS = MakeSprite("timing_track", 120, 16, "#1e1e1e", "#000000");
        Sprite zoneS = MakeSprite("timing_zone", 28, 16, "#5fd06a", "#3a9c45");
        Sprite markerS = MakeSprite("timing_marker", 8, 28, "#ffffff", "#d03030");

        // 실제 동물 이미지 (Assets/04_Sprites/Animals/*.png)
        var animals = new (string name, string file)[]
        {
            ("수달", "sudal"), ("코끼리", "elephant"), ("펭귄", "penguin"),
            ("사자", "lion"), ("원숭이", "monkey"),
        };

        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // 배경(가로로 긴 어린이대공원)
        Sprite bg = ImportSprite(BgPath, SpriteAlignment.Center, 100f, 8192);
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

        // 플레이 가능한 바닥 영역(배경 아래쪽 길 부근) - 값을 낮춰 길에 가깝게
        float playY = -halfH + bgH * 0.07f;
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
        var catcher = player.AddComponent<AnimalCatcher>();
        catcher.SetBounds(leftEdge, rightEdge); // 배경 밖으로 못 나가게

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
            Sprite body = ImportSprite($"Assets/04_Sprites/Animals/{animals[i].file}.png", SpriteAlignment.BottomCenter, 650f, 2048);
            BuildAnimal(animals[i].name, new Vector3(x, playY, 0f), body, trackS, zoneS, markerS);
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
        MakeText("Hint", canvasGO.transform, "← → (A/D) 이동 · 동물 근처서 Space 길게 누르고, 마커 초록일 때 떼기! (틀리면 도망)", 28, TextAlignmentOptions.Bottom,
            new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 30), new Vector2(960, 50), new Color(0.1f, 0.1f, 0.1f));

        // HUD 다시하기 버튼(상단 중앙)
        var hudRestart = MakeButton("RestartButton", canvasGO.transform, "다시하기", new Color(0.22f, 0.45f, 0.7f, 0.95f),
            new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -28), new Vector2(180, 60));
        UnityEventTools.AddPersistentListener(hudRestart.GetComponent<Button>().onClick, gm.Restart);

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

        // 클리어 화면 다시하기 버튼
        var clearRestart = MakeButton("RestartButton", clearPanel.transform, "다시하기", new Color(0.2f, 0.55f, 0.32f, 1f),
            new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 130), new Vector2(280, 84));
        UnityEventTools.AddPersistentListener(clearRestart.GetComponent<Button>().onClick, gm.Restart);

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

    private static void BuildAnimal(string name, Vector3 pos, Sprite body, Sprite trackS, Sprite zoneS, Sprite markerS)
    {
        var go = new GameObject("Animal_" + name);
        go.transform.position = pos;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = body;
        sr.sortingOrder = 0;
        var animal = go.AddComponent<Animal>();

        float h = body != null ? body.bounds.size.y : 1f;

        // 록온 테두리: 같은 스프라이트를 뒤에 굵게 깔아 노란 실루엣 외곽선
        const float outlineScale = 1.22f;
        var outline = new GameObject("Outline");
        outline.transform.SetParent(go.transform, false);
        outline.transform.localScale = Vector3.one * outlineScale;
        outline.transform.localPosition = new Vector3(0f, -(outlineScale - 1f) * 0.5f * h, 0f); // 바닥피벗 보정
        var osr = outline.AddComponent<SpriteRenderer>();
        osr.sprite = body;
        osr.color = new Color(1f, 0.82f, 0f, 1f); // 진한 노랑
        osr.sortingOrder = -1;
        outline.SetActive(false);

        // 타이밍 바(머리 위): 트랙 + 성공구간(초록) + 마커
        var bar = new GameObject("TimingBar");
        bar.transform.SetParent(go.transform, false);
        bar.transform.localPosition = new Vector3(0f, h + 0.2f, 0f);

        var track = new GameObject("Track");
        track.transform.SetParent(bar.transform, false);
        var tsr = track.AddComponent<SpriteRenderer>();
        tsr.sprite = trackS;
        tsr.sortingOrder = 5;

        var zone = new GameObject("Zone");
        zone.transform.SetParent(bar.transform, false);
        zone.transform.localPosition = new Vector3(0f, 0f, 0.01f);
        var zsr = zone.AddComponent<SpriteRenderer>();
        zsr.sprite = zoneS;
        zsr.sortingOrder = 6;

        var marker = new GameObject("Marker");
        marker.transform.SetParent(bar.transform, false);
        marker.transform.localPosition = new Vector3(0f, 0f, 0.02f);
        var msr = marker.AddComponent<SpriteRenderer>();
        msr.sprite = markerS;
        msr.sortingOrder = 7;

        bar.SetActive(false);

        SetRef(animal, "lockIndicator", outline);
        SetRef(animal, "timingBar", bar);
        SetRef(animal, "marker", marker.transform);
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

    private static GameObject MakeButton(string name, Transform parent, string label, Color color,
        Vector2 aMin, Vector2 aMax, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        SetRect(go, aMin, aMax, pivot, pos, size);
        go.GetComponent<Image>().color = color;
        MakeText("Label", go.transform, label, 30, TextAlignmentOptions.Center,
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, Color.white);
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

    // 이미 존재하는 PNG를 Sprite로 임포트 (정렬/PPU/최대크기 지정)
    private static Sprite ImportSprite(string path, SpriteAlignment align, float ppu, int maxSize)
    {
        if (!File.Exists(path))
        {
            Debug.LogWarning("[ParkSceneBuilder] 파일 없음: " + path);
            return null;
        }
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        var imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (imp != null)
        {
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.spritePixelsPerUnit = ppu;
            imp.maxTextureSize = maxSize;
            imp.mipmapEnabled = false;
            var s = new TextureImporterSettings();
            imp.ReadTextureSettings(s);
            s.spriteAlignment = (int)align;
            imp.SetTextureSettings(s);
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
