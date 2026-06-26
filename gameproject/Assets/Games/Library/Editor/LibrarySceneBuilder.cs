using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// 메뉴에서 클릭하면 "별마당 도서관(책 찾기)" 플레이 가능한 씬을 통째로 생성한다.
// 임시(placeholder) 에셋도 자동으로 만들어 연결하므로, 실행 후 바로 ▶ Play 가능.
// 진짜 에셋은 나중에 04_Sprites 의 스프라이트만 교체하면 된다.
public static class LibrarySceneBuilder
{
    private const string GameDir = "Assets/Games/Library";
    private const string SpriteDir = GameDir + "/Sprites/_Placeholder";
    private const string DataDir = GameDir + "/Data/_Generated";
    private const string PrefabPath = GameDir + "/Prefabs/BookButton.prefab";
    private const string ScenePath = GameDir + "/Scenes/LibraryGame.unity";

    [MenuItem("별마당도서관/플레이 가능한 씬 생성")]
    public static void Build()
    {
        EnsureFolderPath(SpriteDir);
        EnsureFolderPath(DataDir);
        EnsureFolderPath(GameDir + "/Prefabs");
        EnsureFolderPath(GameDir + "/Scenes");
        AssetDatabase.Refresh(); // 새 폴더를 Unity가 인식하도록

        // 1) 임시 스프라이트 생성
        Sprite playerSprite = MakeSprite("player", 48, 48, "#3f7bd6", "#274f8a");
        Sprite shelfSprite = MakeSprite("shelf", 150, 90, "#b9824c", "#7c5429");
        Sprite floorSprite = MakeSprite("floor", 256, 256, "#efe6d2", "#e3d6ba");

        // 2) 책 12색 정의 + 책등 스프라이트 + BookData 생성
        var palette = new (string name, string hex, string dark)[]
        {
            ("빨강", "#d6453f", "#8f2b27"), ("주황", "#e08a3c", "#9c5d24"),
            ("노랑", "#e8c14e", "#a98a2f"), ("초록", "#4f9c48", "#356632"),
            ("청록", "#3fa8c2", "#2a7286"), ("파랑", "#3f63d6", "#28428f"),
            ("보라", "#8a5ad6", "#5b3a8f"), ("분홍", "#d67ba6", "#8f5172"),
            ("갈색", "#9b6b43", "#67462c"), ("크림", "#e9e0c8", "#c9bd9b"),
            ("회색", "#8a8f96", "#5a5e63"), ("버건디", "#a23b4f", "#6c2735"),
        };

        var books = new List<BookData>();
        for (int i = 0; i < palette.Length; i++)
        {
            var p = palette[i];
            int w = 36 + (i % 3) * 6;          // 두께 살짝 다양화
            int h = 120 + (i % 4) * 10;        // 높이 살짝 다양화
            Sprite spine = MakeSprite($"book_{i:00}_{p.name}", w, h, p.hex, p.dark);

            var book = ScriptableObject.CreateInstance<BookData>();
            book.id = $"book_{i:00}";
            book.title = $"{p.name} 책";
            book.spineSprite = spine;
            book.features = $"{p.name}색 책";
            string bookPath = $"{DataDir}/Book_{i:00}_{p.name}.asset";
            AssetDatabase.CreateAsset(book, bookPath);
            books.Add(book);
        }
        AssetDatabase.SaveAssets();

        // 3) 새 씬 생성
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        Camera cam = Camera.main;
        if (cam != null)
        {
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cam.transform.position = new Vector3(0f, 0f, -10f);
            cam.backgroundColor = new Color(0.93f, 0.90f, 0.83f);
            cam.clearFlags = CameraClearFlags.SolidColor;
        }

        // 바닥
        var floor = new GameObject("Floor");
        var floorSR = floor.AddComponent<SpriteRenderer>();
        floorSR.sprite = floorSprite;
        floorSR.drawMode = SpriteDrawMode.Tiled;
        floorSR.size = new Vector2(20f, 12f);
        floorSR.sortingOrder = -10;

        // 4) 플레이어
        var player = new GameObject("Player");
        player.transform.position = Vector3.zero;
        var psr = player.AddComponent<SpriteRenderer>();
        psr.sprite = playerSprite;
        psr.sortingOrder = 5;
        var rb = player.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        var body = player.AddComponent<CircleCollider2D>();  // 몸통
        body.radius = 0.22f;
        var range = player.AddComponent<CircleCollider2D>();  // 상호작용 범위
        range.radius = 1.1f;
        range.isTrigger = true;
        player.AddComponent<PlayerController>();
        player.AddComponent<PlayerInteraction>();

        // 5) 책장 4개 + 책 분배(3권씩)
        var shelfPositions = new[]
        {
            new Vector3(-5f, 2.5f, 0f), new Vector3(5f, 2.5f, 0f),
            new Vector3(-5f, -2.5f, 0f), new Vector3(5f, -2.5f, 0f),
        };
        for (int s = 0; s < shelfPositions.Length; s++)
        {
            var shelf = new GameObject($"Bookshelf_{s}");
            shelf.transform.position = shelfPositions[s];
            var ssr = shelf.AddComponent<SpriteRenderer>();
            ssr.sprite = shelfSprite;
            ssr.sortingOrder = 1;
            var col = shelf.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(2.2f, 1.6f);
            var bs = shelf.AddComponent<Bookshelf>();
            bs.books = new List<BookData>();
            for (int b = 0; b < 3; b++)
            {
                int idx = s * 3 + b;
                if (idx < books.Count) bs.books.Add(books[idx]);
            }
        }

        // 6) 매니저
        var gmGO = new GameObject("GameManager");
        var gm = gmGO.AddComponent<GameManager>();
        SetFloat(gm, "timeLimit", 120f);
        SetInt(gm, "targetCount", 5);

        var amGO = new GameObject("AudioManager");
        var am = amGO.AddComponent<AudioManager>();
        var bgm = amGO.AddComponent<AudioSource>(); bgm.playOnAwake = false; bgm.loop = true;
        var sfx = amGO.AddComponent<AudioSource>(); sfx.playOnAwake = false;
        SetRef(am, "bgmSource", bgm);
        SetRef(am, "sfxSource", sfx);

        // 7) Canvas + EventSystem (신 Input System)
        var canvasGO = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        var esGO = new GameObject("EventSystem", typeof(EventSystem));
        esGO.AddComponent<InputSystemUIInputModule>();

        // HUD
        var timerText = MakeText("TimerText", canvasGO.transform, "120", 48, TextAlignmentOptions.TopLeft,
            new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(40, -30), new Vector2(300, 70), Color.black);
        var foundText = MakeText("FoundText", canvasGO.transform, "0 / 5", 48, TextAlignmentOptions.TopRight,
            new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-40, -30), new Vector2(300, 70), Color.black);

        // 목표 안내 (썸네일 + 특징)
        var hintImg = MakeImage("TargetThumb", canvasGO.transform, Color.white,
            new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0), new Vector2(40, 40), new Vector2(70, 110));
        var hintText = MakeText("TargetFeature", canvasGO.transform, "찾을 책: ...", 34, TextAlignmentOptions.Left,
            new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0), new Vector2(125, 60), new Vector2(420, 90), Color.black);

        // 상호작용 프롬프트
        var promptGO = MakePanel("InteractPrompt", canvasGO.transform, new Color(0f, 0f, 0f, 0.6f),
            new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 120), new Vector2(360, 70));
        MakeText("Label", promptGO.transform, "[ F ] 책장 열기", 34, TextAlignmentOptions.Center,
            new Vector2(0, 0), new Vector2(1, 1), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, Color.white);

        // 정답/오답 팝업
        var correctPopup = MakePopup("CorrectPopup", canvasGO.transform, "이 책이 맞습니다!", new Color(0.27f, 0.6f, 0.32f, 0.92f));
        var wrongPopup = MakePopup("WrongPopup", canvasGO.transform, "이 책이 아닙니다", new Color(0.7f, 0.27f, 0.27f, 0.92f));

        // 승리/패배 패널
        var winPanel = MakeFullPanel("WinPanel", canvasGO.transform, "🎉 클리어!", new Color(0.1f, 0.3f, 0.15f, 0.85f));
        var losePanel = MakeFullPanel("LosePanel", canvasGO.transform, "시간 초과...", new Color(0.3f, 0.1f, 0.1f, 0.85f));

        // 책장 정면뷰 패널
        var shelfPanel = MakePanel("ShelfViewPanel", canvasGO.transform, new Color(0.15f, 0.12f, 0.1f, 0.96f),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900, 620));
        MakeText("Title", shelfPanel.transform, "책을 골라보세요", 40, TextAlignmentOptions.Top,
            new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -20), new Vector2(0, 70), Color.white);
        var slots = MakeImage("BookSlots", shelfPanel.transform, new Color(0, 0, 0, 0),
            new Vector2(0, 0), new Vector2(1, 1), new Vector2(0.5f, 0.5f), new Vector2(0, -20), new Vector2(-80, -140));
        var grid = slots.gameObject.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(70, 130);
        grid.spacing = new Vector2(20, 20);
        grid.padding = new RectOffset(20, 20, 20, 20);
        grid.childAlignment = TextAnchor.MiddleCenter;
        var closeBtnGO = MakeButton("CloseButton", shelfPanel.transform, "X", new Color(0.6f, 0.2f, 0.2f, 1f),
            new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-20, -20), new Vector2(60, 60));
        var closeBtn = closeBtnGO.GetComponent<Button>();

        // 8) 책 버튼 프리팹 생성
        BookButton bookButtonPrefab = BuildBookButtonPrefab();

        // 9) UI 매니저 + 정면뷰 매니저 연결
        var uiGO = new GameObject("UIManager");
        var ui = uiGO.AddComponent<GameUIManager>();
        SetRef(ui, "timerText", timerText);
        SetRef(ui, "foundText", foundText);
        SetRef(ui, "targetThumbnail", hintImg);
        SetRef(ui, "targetFeatureText", hintText);
        SetRef(ui, "interactPrompt", promptGO);
        SetRef(ui, "correctPopup", correctPopup);
        SetRef(ui, "wrongPopup", wrongPopup);
        SetRef(ui, "winPanel", winPanel);
        SetRef(ui, "losePanel", losePanel);

        var viewGO = new GameObject("BookshelfView");
        var view = viewGO.AddComponent<BookshelfView>();
        SetRef(view, "panel", shelfPanel);
        SetRef(view, "bookSlotParent", slots.transform);
        SetRef(view, "bookButtonPrefab", bookButtonPrefab);
        SetRef(view, "closeButton", closeBtn);

        // 10) 저장 + 빌드세팅 등록
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AddSceneToBuildSettings(ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("완료",
            "플레이 가능한 씬을 생성했습니다!\n\n" +
            "01_Scenes/LibraryGame 이 열려 있습니다.\n▶ Play 를 눌러 테스트하세요.\n\n" +
            "WASD 이동 → 책장 근처에서 F → 책 클릭", "확인");
        Debug.Log("[LibrarySceneBuilder] 씬 생성 완료: " + ScenePath);
    }

    // ---------- 헬퍼들 ----------

    private static BookButton BuildBookButtonPrefab()
    {
        var go = new GameObject("BookButton", typeof(RectTransform), typeof(Image), typeof(Button));
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(70, 130);
        go.GetComponent<Image>().color = new Color(1, 1, 1, 0.15f);

        var spineGO = new GameObject("Spine", typeof(RectTransform), typeof(Image));
        spineGO.transform.SetParent(go.transform, false);
        var srt = spineGO.GetComponent<RectTransform>();
        srt.anchorMin = Vector2.zero; srt.anchorMax = Vector2.one;
        srt.offsetMin = new Vector2(6, 6); srt.offsetMax = new Vector2(-6, -6);
        var spineImg = spineGO.GetComponent<Image>();
        spineImg.preserveAspect = true;

        var bb = go.AddComponent<BookButton>();
        SetRef(bb, "spineImage", spineImg);

        var prefab = PrefabUtility.SaveAsPrefabAsset(go, PrefabPath);
        Object.DestroyImmediate(go);
        return prefab.GetComponent<BookButton>();
    }

    private static GameObject MakePopup(string name, Transform parent, string text, Color bg)
    {
        var go = MakePanel(name, parent, bg,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0, 200), new Vector2(560, 120));
        MakeText("Label", go.transform, text, 44, TextAlignmentOptions.Center,
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, Color.white);
        return go;
    }

    private static GameObject MakeFullPanel(string name, Transform parent, string text, Color bg)
    {
        var go = MakePanel(name, parent, bg,
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        MakeText("Label", go.transform, text, 80, TextAlignmentOptions.Center,
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, Color.white);
        return go;
    }

    private static GameObject MakePanel(string name, Transform parent, Color color,
        Vector2 aMin, Vector2 aMax, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        var img = MakeImage(name, parent, color, aMin, aMax, pivot, pos, size);
        return img.gameObject;
    }

    private static Image MakeImage(string name, Transform parent, Color color,
        Vector2 aMin, Vector2 aMax, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        SetRect(go, aMin, aMax, pivot, pos, size);
        var img = go.GetComponent<Image>();
        img.color = color;
        return img;
    }

    private static GameObject MakeButton(string name, Transform parent, string label, Color color,
        Vector2 aMin, Vector2 aMax, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        SetRect(go, aMin, aMax, pivot, pos, size);
        go.GetComponent<Image>().color = color;
        MakeText("Label", go.transform, label, 34, TextAlignmentOptions.Center,
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

    private static Sprite MakeSprite(string name, int w, int h, string fillHex, string borderHex)
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
            imp.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static void EnsureFolder(string parent, string child)
    {
        if (!AssetDatabase.IsValidFolder($"{parent}/{child}"))
            AssetDatabase.CreateFolder(parent, child);
    }

    // 중첩 경로 폴더 생성(디스크에 직접 만들고 임포트). 파인더로 만든 폴더도 인식되게.
    private static void EnsureFolderPath(string folder)
    {
        Directory.CreateDirectory(folder); // 프로젝트 루트 기준 상대경로
    }

    private static void AddSceneToBuildSettings(string path)
    {
        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        if (scenes.Exists(s => s.path == path)) return;
        scenes.Insert(0, new EditorBuildSettingsScene(path, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    // private [SerializeField] 필드에 참조/값 주입 (SerializedObject 사용)
    private static void SetRef(Object comp, string field, Object value)
    {
        var so = new SerializedObject(comp);
        var sp = so.FindProperty(field);
        if (sp == null) { Debug.LogWarning($"[SceneBuilder] 필드 못 찾음: {comp.GetType().Name}.{field}"); return; }
        sp.objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetFloat(Object comp, string field, float value)
    {
        var so = new SerializedObject(comp);
        var sp = so.FindProperty(field);
        if (sp != null) { sp.floatValue = value; so.ApplyModifiedPropertiesWithoutUndo(); }
    }

    private static void SetInt(Object comp, string field, int value)
    {
        var so = new SerializedObject(comp);
        var sp = so.FindProperty(field);
        if (sp != null) { sp.intValue = value; so.ApplyModifiedPropertiesWithoutUndo(); }
    }
}
