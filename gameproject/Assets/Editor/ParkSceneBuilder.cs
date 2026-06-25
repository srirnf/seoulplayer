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
        // 록온 선택 링 + 충전 게이지 + 타이밍 바 (둥글고 크게)
        Sprite ringS = MakeRing("sel_ring", 128, 12, "#ffce2e");
        Sprite chargeTrackS = MakeRoundedFill("charge_track", 150, 24, "#222831", SpriteAlignment.Center);
        Sprite chargeFillS = MakeRoundedFill("charge_fill", 150, 24, "#ffcf3f", SpriteAlignment.LeftCenter);
        Sprite trackS = MakeRoundedFill("timing_track", 220, 34, "#222831", SpriteAlignment.Center);
        Sprite zoneS = MakeRoundedFill("timing_zone", 48, 34, "#5fd06a", SpriteAlignment.Center);
        Sprite markerS = MakeRoundedFill("timing_marker", 14, 50, "#ff5a5a", SpriteAlignment.Center);

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
            BuildAnimal(animals[i].name, new Vector3(x, playY, 0f), body, ringS, chargeTrackS, chargeFillS, trackS, zoneS, markerS);
        }

        // 매니저
        var gmGO = new GameObject("ParkGameManager");
        var gm = gmGO.AddComponent<ParkGameManager>();
        SetArray(gm, "cagePoints", cagePoints.ToArray());

        // ===== UI (둥근 알약 스타일) =====
        Sprite round = MakeRoundedSprite("ui_round", 48, 16);

        var canvasGO = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        new GameObject("EventSystem", typeof(EventSystem)).AddComponent<InputSystemUIInputModule>();

        Color pillDark = new Color(0.12f, 0.14f, 0.18f, 0.82f);
        Color textLight = new Color(0.97f, 0.97f, 0.94f);

        // 동물 카운트(좌상단)
        var caughtPill = MakeSliced("CaughtPill", canvasGO.transform, round, pillDark,
            new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(36, -28), new Vector2(270, 78));
        var caughtText = MakeText("Text", caughtPill.transform, "동물 0 / 5", 40, TextAlignmentOptions.Center,
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, textLight);

        // 시간(우상단)
        var timerPill = MakeSliced("TimerPill", canvasGO.transform, round, pillDark,
            new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-36, -28), new Vector2(230, 78));
        var timerText = MakeText("Text", timerPill.transform, "0.0초", 40, TextAlignmentOptions.Center,
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, textLight);

        // 엔딩 전환 오버레이
        var ending = MakePanel("EndingTransition", canvasGO.transform, new Color(0f, 0f, 0f, 0.55f),
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        var endPill = MakeSliced("Pill", ending.transform, round, new Color(0.12f, 0.14f, 0.18f, 0.92f),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760, 140));
        MakeText("Text", endPill.transform, "동물들을 우리로 데려다 주는 중...", 44, TextAlignmentOptions.Center,
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, textLight);

        // 클리어 화면: 어두운 오버레이 + 가운데 카드
        var clearPanel = MakePanel("ClearPanel", canvasGO.transform, new Color(0f, 0f, 0f, 0.55f),
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        var card = MakeSliced("Card", clearPanel.transform, round, new Color(0.99f, 0.97f, 0.90f, 1f),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(780, 460));
        MakeText("Title", card.transform, "모두 잘 보냈어요!", 64, TextAlignmentOptions.Center,
            new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -54), new Vector2(0, 120), new Color(0.16f, 0.46f, 0.26f));
        var clearTime = MakeText("ClearTime", card.transform, "클리어 시간: 0.0초", 44, TextAlignmentOptions.Center,
            new Vector2(0, 0.5f), new Vector2(1, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 6), new Vector2(0, 80), new Color(0.27f, 0.23f, 0.18f));
        var clearRestart = MakePrettyButton("RestartButton", card.transform, round, "다시하기",
            new Color(0.30f, 0.62f, 0.36f, 1f),
            new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 56), new Vector2(300, 96));
        UnityEventTools.AddPersistentListener(clearRestart.GetComponent<Button>().onClick, gm.Restart);

        var uiGO = new GameObject("ParkUIManager");
        var ui = uiGO.AddComponent<ParkUIManager>();
        SetRef(ui, "caughtText", caughtText);
        SetRef(ui, "timerText", timerText);
        SetRef(ui, "endingTransition", ending);
        SetRef(ui, "clearPanel", clearPanel);
        SetRef(ui, "clearTimeText", clearTime);

        // ===== 게임방법 안내 시작 화면 =====
        var howTo = MakePanel("HowToPanel", canvasGO.transform, new Color(0f, 0f, 0f, 0.62f),
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        var htCard = MakeSliced("Card", howTo.transform, round, new Color(0.99f, 0.97f, 0.90f, 1f),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(940, 600));
        MakeText("Title", htCard.transform, "동물꼬시기 — 게임 방법", 56, TextAlignmentOptions.Center,
            new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -46), new Vector2(0, 100), new Color(0.16f, 0.46f, 0.26f));
        MakeText("Body", htCard.transform,
            "서울어린이대공원에서 탈출한 동물들을 다시 우리로!\n\n" +
            "· ← → (또는 A / D) 로 좌우 이동\n" +
            "· 동물 가까이서 Space 를 길게 눌러 충전 → 록온\n" +
            "· 록온되면 Space 를 떼면 타이밍 바 등장\n" +
            "· 빨간 마커가 초록 구간일 때 Space! → 성공하면 따라옴\n" +
            "· 타이밍 실패 시 동물이 잠깐 도망가요\n\n" +
            "모든 동물을 모으면 클리어! (걸린 시간 기록)",
            32, TextAlignmentOptions.TopLeft,
            new Vector2(0, 0), new Vector2(1, 1), new Vector2(0.5f, 0.5f), new Vector2(0, 20), new Vector2(-100, -220), new Color(0.25f, 0.22f, 0.18f));
        var startBtn = MakePrettyButton("StartButton", htCard.transform, round, "게임 시작",
            new Color(0.30f, 0.62f, 0.36f, 1f),
            new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 46), new Vector2(320, 96));
        UnityEventTools.AddPersistentListener(startBtn.GetComponent<Button>().onClick, ui.CloseHowToAndStart);
        SetRef(ui, "howToPanel", howTo);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AddSceneToBuildSettings(ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("완료",
            "서울대공원(동물꼬시기) 플레이 씬 생성 완료!\n\n" +
            "01_Scenes/ParkGame 이 열려 있습니다. ▶ Play!\n\n" +
            "시작화면에서 게임시작 → A/D 이동 → Space 길게 충전 록온 →\n" +
            "떼면 타이밍 바 → 초록일 때 Space! → 다 모으면 클리어", "확인");
        Debug.Log("[ParkSceneBuilder] 씬 생성 완료: " + ScenePath);
    }

    private static void BuildAnimal(string name, Vector3 pos, Sprite body, Sprite ringS,
        Sprite chargeTrackS, Sprite chargeFillS, Sprite trackS, Sprite zoneS, Sprite markerS)
    {
        var go = new GameObject("Animal_" + name);
        go.transform.position = pos;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = body;
        sr.sortingOrder = 0;
        var animal = go.AddComponent<Animal>();

        float h = body != null ? body.bounds.size.y : 1f;
        float w = body != null ? body.bounds.size.x : 1f;

        // 록온 선택 링(동물 둘레) - 이미지 복사가 아닌 별도 링 스프라이트
        var ring = new GameObject("SelectionRing");
        ring.transform.SetParent(go.transform, false);
        ring.transform.localPosition = new Vector3(0f, h * 0.5f, 0f);
        float ringScale = Mathf.Max(w, h) * 1.35f / 1.28f; // 링 원본 1.28유닛(128px) 기준
        ring.transform.localScale = Vector3.one * ringScale;
        var rsr = ring.AddComponent<SpriteRenderer>();
        rsr.sprite = ringS;
        rsr.sortingOrder = -1;
        ring.SetActive(false);

        // 충전 게이지(머리 위)
        var charge = new GameObject("ChargeGauge");
        charge.transform.SetParent(go.transform, false);
        charge.transform.localPosition = new Vector3(0f, h + 0.22f, 0f);
        var ctrack = new GameObject("Track");
        ctrack.transform.SetParent(charge.transform, false);
        var ctsr = ctrack.AddComponent<SpriteRenderer>();
        ctsr.sprite = chargeTrackS; ctsr.sortingOrder = 5;
        var cfill = new GameObject("Fill");
        cfill.transform.SetParent(charge.transform, false);
        cfill.transform.localPosition = new Vector3(-0.75f, 0f, 0.01f); // 트랙 폭 150px=1.5, 왼쪽 끝
        var cfsr = cfill.AddComponent<SpriteRenderer>();
        cfsr.sprite = chargeFillS; cfsr.sortingOrder = 6;
        charge.SetActive(false);

        // 타이밍 바(머리 위): 트랙 + 성공구간(초록) + 마커
        var bar = new GameObject("TimingBar");
        bar.transform.SetParent(go.transform, false);
        bar.transform.localPosition = new Vector3(0f, h + 0.22f, 0f);
        var track = new GameObject("Track");
        track.transform.SetParent(bar.transform, false);
        var tsr = track.AddComponent<SpriteRenderer>();
        tsr.sprite = trackS; tsr.sortingOrder = 5;
        var zone = new GameObject("Zone");
        zone.transform.SetParent(bar.transform, false);
        zone.transform.localPosition = new Vector3(0f, 0f, 0.01f);
        var zsr = zone.AddComponent<SpriteRenderer>();
        zsr.sprite = zoneS; zsr.sortingOrder = 6;
        var marker = new GameObject("Marker");
        marker.transform.SetParent(bar.transform, false);
        marker.transform.localPosition = new Vector3(0f, 0f, 0.02f);
        var msr = marker.AddComponent<SpriteRenderer>();
        msr.sprite = markerS; msr.sortingOrder = 7;
        bar.SetActive(false);

        SetRef(animal, "selectionRing", ring);
        SetRef(animal, "chargeRoot", charge);
        SetRef(animal, "chargeFill", cfill.transform);
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

    private static Image MakeSliced(string name, Transform parent, Sprite sprite, Color color,
        Vector2 aMin, Vector2 aMax, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        SetRect(go, aMin, aMax, pivot, pos, size);
        var img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.type = Image.Type.Sliced;
        img.color = color;
        return img;
    }

    private static GameObject MakePrettyButton(string name, Transform parent, Sprite sprite, string label, Color color,
        Vector2 aMin, Vector2 aMax, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        SetRect(go, aMin, aMax, pivot, pos, size);
        var img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.type = Image.Type.Sliced;
        img.color = color;
        var btn = go.GetComponent<Button>();
        var cb = btn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f);
        cb.pressedColor = new Color(0.82f, 0.82f, 0.82f, 1f);
        cb.fadeDuration = 0.08f;
        btn.colors = cb;
        MakeText("Label", go.transform, label, 32, TextAlignmentOptions.Center,
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, Color.white);
        return go;
    }

    // 둥근 모서리 9-슬라이스 스프라이트 생성(UI 알약/카드/버튼용)
    private static Sprite MakeRoundedSprite(string name, int size, int radius)
    {
        string path = $"{SpriteDir}/{name}.png";
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var px = new Color[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                px[y * size + x] = RoundedInside(x, y, size, radius) ? Color.white : new Color(1f, 1f, 1f, 0f);
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
            imp.filterMode = FilterMode.Bilinear;
            imp.mipmapEnabled = false;
            var s = new TextureImporterSettings();
            imp.ReadTextureSettings(s);
            s.spriteBorder = new Vector4(radius, radius, radius, radius);
            s.spriteAlignment = (int)SpriteAlignment.Center;
            imp.SetTextureSettings(s);
            imp.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static bool RoundedInside(int x, int y, int size, int r)
    {
        float cx = Mathf.Clamp(x, r, size - 1 - r);
        float cy = Mathf.Clamp(y, r, size - 1 - r);
        float dx = x - cx, dy = y - cy;
        return dx * dx + dy * dy <= (float)r * r;
    }

    // 둥근(알약) 채워진 월드 스프라이트 (게이지/바용). align으로 피벗 지정.
    private static Sprite MakeRoundedFill(string name, int w, int h, string fillHex, SpriteAlignment align)
    {
        string path = $"{SpriteDir}/{name}.png";
        ColorUtility.TryParseHtmlString(fillHex, out Color fill);
        Color clear = new Color(fill.r, fill.g, fill.b, 0f);
        int r = Mathf.Min(w, h) / 2;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        var px = new Color[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float cx = Mathf.Clamp(x, r, w - 1 - r);
                float cy = Mathf.Clamp(y, r, h - 1 - r);
                float dx = x - cx, dy = y - cy;
                px[y * w + x] = (dx * dx + dy * dy <= (float)r * r) ? fill : clear;
            }
        tex.SetPixels(px);
        tex.Apply();
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        ApplySpriteImport(path, align);
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    // 링(도넛) 월드 스프라이트 (록온 표시용)
    private static Sprite MakeRing(string name, int size, int thickness, string fillHex)
    {
        string path = $"{SpriteDir}/{name}.png";
        ColorUtility.TryParseHtmlString(fillHex, out Color fill);
        Color clear = new Color(fill.r, fill.g, fill.b, 0f);
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var px = new Color[size * size];
        float c = (size - 1) * 0.5f;
        float outer = size * 0.5f - 1f;
        float inner = outer - thickness;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = x - c, dy = y - c;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                px[y * size + x] = (d <= outer && d >= inner) ? fill : clear;
            }
        tex.SetPixels(px);
        tex.Apply();
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        ApplySpriteImport(path, SpriteAlignment.Center);
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static void ApplySpriteImport(string path, SpriteAlignment align)
    {
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        var imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (imp == null) return;
        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = SpriteImportMode.Single;
        imp.spritePixelsPerUnit = 100;
        imp.filterMode = FilterMode.Bilinear;
        imp.mipmapEnabled = false;
        var s = new TextureImporterSettings();
        imp.ReadTextureSettings(s);
        s.spriteAlignment = (int)align;
        imp.SetTextureSettings(s);
        imp.SaveAndReimport();
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
