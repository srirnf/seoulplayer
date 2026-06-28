using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
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
        if (shelfTex == null)
            Debug.LogError("[LibraryExploreBuilder] 책장 이미지를 못 불러왔습니다: " + ShelfPng +
                "\n→ 파일이 실제 이미지인지(LFS 포인터 아님), 경로가 맞는지 확인하세요. (책찾기 화면이 빈칸으로 보입니다)");

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

        // 채광 가득한 실내 느낌으로 환경광 밝게
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.72f, 0.73f, 0.76f);

        // ===== 환경(바닥/유리벽/채광천장) — 별마당 도서관 느낌 =====
        var woodTex = MakeWoodTexture();                                          // 절차적 우드 플랭크 텍스처
        var woodMat = MakeUnlitMat(Color.white, false);
        if (woodTex != null) { woodMat.SetTexture("_BaseMap", woodTex); woodMat.SetTextureScale("_BaseMap", new Vector2(10f, 10f)); }
        var glassMat = MakeUnlitMat(new Color(0.72f, 0.84f, 0.92f, 0.20f), true); // 옅은 유리벽
        var roofMat = MakeUnlitMat(new Color(0.96f, 0.97f, 0.99f, 1f), false);   // 밝은 채광 천장

        const float room = 90f;   // 바닥 한 변(유닛)
        const float wallH = 42f;  // 벽/천장 높이
        const float half = room * 0.5f;

        // 바닥(우드)
        var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "Floor";
        floor.transform.localScale = new Vector3(room / 10f, 1f, room / 10f);
        floor.GetComponent<Renderer>().sharedMaterial = woodMat;

        // 천장(밝은 유리 채광, 아래를 향하게 뒤집음)
        var ceil = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ceil.name = "Ceiling";
        ceil.transform.position = new Vector3(0f, wallH, 0f);
        ceil.transform.localScale = new Vector3(room / 10f, 1f, room / 10f);
        ceil.transform.rotation = Quaternion.Euler(180f, 0f, 0f);
        ceil.GetComponent<Renderer>().sharedMaterial = roofMat;
        Object.DestroyImmediate(ceil.GetComponent<Collider>());

        // 유리벽 4면(투명, 콜라이더로 플레이어 가둠)
        CreateWall("Wall_N", new Vector3(0f, wallH / 2f, half), new Vector3(room, wallH, 0.5f), glassMat);
        CreateWall("Wall_S", new Vector3(0f, wallH / 2f, -half), new Vector3(room, wallH, 0.5f), glassMat);
        CreateWall("Wall_E", new Vector3(half, wallH / 2f, 0f), new Vector3(0.5f, wallH, room), glassMat);
        CreateWall("Wall_W", new Vector3(-half, wallH / 2f, 0f), new Vector3(0.5f, wallH, room), glassMat);

        // 길/에스컬레이터/화분/벤치/2층 발코니 (사진 느낌으로 꾸밈)
        BuildDecor(room, wallH);

        // 상호작용 영역은 무조건 하나 생성(FBX가 실패해도 F는 작동)
        var zone = new GameObject("InteractZone");
        zone.transform.position = new Vector3(0f, 4f, 18f);
        var bc = zone.AddComponent<BoxCollider>(); bc.isTrigger = true;
        bc.size = new Vector3(80f, 30f, 60f);
        zone.AddComponent<InteractZone>();

        // FBX 배치 (실패해도 나머지 씬은 정상 생성되게 try/catch)
        try
        {
            var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(FbxPath);
            if (fbx != null)
            {
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(fbx);
                inst.name = "BookWall";
                inst.transform.rotation = Quaternion.Euler(0f, 180f, 0f); // 앞뒤 뒤집힘 보정(필요시 인스펙터에서 Y회전 조정)
                FixToURP(inst);

                var rends = inst.GetComponentsInChildren<Renderer>();
                if (rends.Length > 0)
                {
                    Bounds b = rends[0].bounds;
                    for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
                    float maxDim = Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z));
                    if (maxDim > 0.0001f) inst.transform.localScale *= (40f / maxDim);

                    b = rends[0].bounds;
                    for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
                    inst.transform.position += new Vector3(18f - b.center.x, -b.min.y, 4f - b.center.z); // 오른쪽 앞 사이드

                    b = rends[0].bounds;
                    for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
                    zone.transform.position = b.center;
                    bc.size = b.size + new Vector3(10f, 10f, 10f);
                }
            }
            else Debug.LogWarning("[LibraryExploreBuilder] FBX를 못 찾음: " + FbxPath);
        }
        catch (System.Exception e)
        {
            Debug.LogError("[LibraryExploreBuilder] FBX 처리 중 오류(나머지는 정상 생성): " + e.Message);
        }

        // 플레이어
        var player = new GameObject("Player");
        player.transform.position = new Vector3(0f, 0f, -12f); // 도서관에서 떨어져서 시작(탐험)
        var cc = player.AddComponent<CharacterController>();
        cc.center = new Vector3(0f, 1f, 0f); cc.height = 2f; cc.radius = 0.4f;
        var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "Body"; body.transform.SetParent(player.transform, false);
        body.transform.localPosition = new Vector3(0f, 1f, 0f);
        Object.DestroyImmediate(body.GetComponent<Collider>());
        var explorer = player.AddComponent<LibraryExplorer>();
        var interact = player.AddComponent<LibraryInteract>();
        // (상호작용은 거리기반이라 별도 트리거/리지드바디 불필요)

        // 카메라
        var cam = Camera.main;
        if (cam == null) { var camGO = new GameObject("Main Camera"); cam = camGO.AddComponent<Camera>(); camGO.tag = "MainCamera"; }
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.85f, 0.91f, 0.97f); // 유리 너머 밝은 하늘
        var follow = cam.gameObject.AddComponent<IsoCameraFollow>();
        SetRef(follow, "target", player.transform);
        SetVector(follow, "offset", new Vector3(0f, 24f, -24f)); // 큰 도서관이라 멀리서

        // ===== 책찾기 UI =====
        var canvasGO = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGO.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920, 1080);
        new GameObject("EventSystem", typeof(EventSystem)).AddComponent<InputSystemUIInputModule>();

        // "[E] 책 찾기" 안내
        var prompt = MakePanel("Prompt", canvasGO.transform, new Color(0, 0, 0, 0.6f),
            new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 60), new Vector2(320, 70));
        MakeText("T", prompt.transform, "[ F ] 책 찾기", 34, Vector2.zero, Vector2.one, Color.white);

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

        // 안내문(좌상단 상태줄)
        var info = MakeText("Info", screen.transform, "이 책을 찾으세요!", 36,
            new Vector2(0, 1), new Vector2(1, 1), Color.white);
        SetRect(info.gameObject, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -40), new Vector2(-340, 70));
        info.alignment = TextAlignmentOptions.Left;

        // "찾을 책" 카드(우상단): 금색 테두리 + 어두운 속지 + 라벨 + 책 썸네일
        var card = MakePanel("TargetCard", screen.transform, new Color(0.82f, 0.66f, 0.32f, 1f),
            new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-40, -40), new Vector2(220, 330));
        var inner = MakePanel("Inner", card.transform, new Color(0.10f, 0.09f, 0.07f, 1f),
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        var innerRT = inner.GetComponent<RectTransform>();
        innerRT.offsetMin = new Vector2(6, 6); innerRT.offsetMax = new Vector2(-6, -6); // 6px 금색 테두리
        var cardLabel = MakeText("Label", inner.transform, "찾을 책", 32,
            new Vector2(0, 1), new Vector2(1, 1), new Color(0.96f, 0.84f, 0.48f, 1f));
        SetRect(cardLabel.gameObject, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -10), new Vector2(0, 48));

        var thumbGO = new GameObject("TargetThumb", typeof(RectTransform), typeof(RawImage), typeof(AspectRatioFitter));
        thumbGO.transform.SetParent(inner.transform, false);
        SetRect(thumbGO, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -26), new Vector2(88, 240));
        thumbGO.GetComponent<RawImage>().raycastTarget = false;
        var arf = thumbGO.GetComponent<AspectRatioFitter>();       // 높이 240 고정, 너비를 책 비율대로(찌그러짐 방지)
        arf.aspectMode = AspectRatioFitter.AspectMode.HeightControlsWidth;
        arf.aspectRatio = 0.37f;                                    // 첫 표시용 기본값(이후 FindBookScreen이 실제 비율로 갱신)

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

        // ===== 시작 게임 설명 UI (서울어린이대공원 스타일) =====
        var round = MakeRoundedSprite("ui_round", 48, 16);
        Color cream = new Color(0.99f, 0.97f, 0.90f, 1f);
        Color green = new Color(0.16f, 0.46f, 0.26f);
        Color brown = new Color(0.27f, 0.23f, 0.18f);
        Vector2 CC = new Vector2(0.5f, 0.5f);

        Vector2 top = new Vector2(0.5f, 1f);
        Vector2 bot = new Vector2(0.5f, 0f);

        var intro = MakePanel("IntroPanel", canvasGO.transform, new Color(0f, 0f, 0f, 0.62f),
            Vector2.zero, Vector2.one, CC, Vector2.zero, Vector2.zero);
        var introCard = MakeSliced("Card", intro.transform, round, cream, CC, CC, CC, Vector2.zero, new Vector2(1060, 940));

        // 제목: 카드 상단
        MakeLabel("Title", introCard.transform, "별마당 도서관", 60, TextAlignmentOptions.Center,
            top, top, top, new Vector2(0, -44), new Vector2(960, 84), green);

        // 본문: 상단에서부터 좌측 정렬로 흐르게(겹침 방지)
        string introBody =
            "서울 <b>별마당 도서관</b>을 구경하며 책을 찾는 게임이에요!\n\n" +
            "<b>[ 조작 ]</b>\n" +
            "·  WASD : 이동\n" +
            "·  마우스 왼쪽 드래그 : 시야 회전\n" +
            "·  마우스 휠 : 확대 / 축소\n\n" +
            "<b>[ 책 찾기 ]</b>\n" +
            "·  책장에 다가가 <b>F</b> → 책 찾기 시작\n" +
            "·  우측 '찾을 책'과 같은 책을 클릭\n" +
            "·  정답 +점수 / 오답 시간 -2초\n" +
            "·  제한시간 안에 최대한 많이 찾기!";
        MakeLabel("Body", introCard.transform, introBody, 30, TextAlignmentOptions.TopLeft,
            top, top, top, new Vector2(0, -150), new Vector2(880, 600), brown);

        // 버튼: 카드 하단 고정
        var startBtn = MakePrettyButton("StartButton", introCard.transform, round, "게임 시작",
            new Color(0.30f, 0.62f, 0.36f, 1f), bot, bot, bot, new Vector2(0, 44), new Vector2(320, 96));

        var introComp = intro.AddComponent<LibraryIntro>();
        SetRef(introComp, "panel", intro);
        SetRef(introComp, "explorer", explorer);
        UnityEventTools.AddPersistentListener(startBtn.GetComponent<Button>().onClick, introComp.StartGame);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AddSceneToBuild(ScenePath);
        AssetDatabase.SaveAssets();

        EditorUtility.DisplayDialog("완료",
            "별마당도서관 2.5D 씬 생성 완료!\n\n" +
            "▶ Play → WASD 탐험 → 도서관 앞에서 F → 제한시간 내 책 많이 찾기\n\n" +
            "· FBX(BookWall) 크기/위치가 안 맞으면 인스펙터 Scale/Position 조정\n" +
            "· 책 42칸 격자가 이미지와 안 맞으면 FindScreen의 Cabinets(좌/우 uv) 조정\n" +
            "· 재질이 분홍/안보이면: FBX 선택 > Materials > Extract, 또는 URP 변환", "확인");
    }

    // FBX 재질을 URP/Unlit 으로 입힌다. (VARCO 텍스처는 조명이 구워져 있어 Unlit이 렌더와 비슷하게 밝음)
    private static void FixToURP(GameObject root)
    {
        var urp = Shader.Find("Universal Render Pipeline/Unlit");
        if (urp == null) urp = Shader.Find("Universal Render Pipeline/Lit");
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
                if (m.shader != null && m.shader.name.Contains("Unlit")) { dst[i] = m; continue; } // 이미 Unlit이면 통과, URP/Lit는 Unlit으로 변환(밝게)
                if (!cache.TryGetValue(m, out var nm))
                {
                    nm = new Material(urp);
                    Texture tex = m.mainTexture;
                    if (tex == null && m.HasProperty("_BaseMap")) tex = m.GetTexture("_BaseMap");
                    if (tex == null && m.HasProperty("_MainTex")) tex = m.GetTexture("_MainTex");
                    if (tex != null) nm.SetTexture("_BaseMap", tex);
                    nm.SetColor("_BaseColor", Color.white); // 텍스처를 풀 밝기로(원본 어두운 색 무시)
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

    // ---- 환경 헬퍼 ----
    // URP/Unlit 단색 재질(밝게, 조명 영향 X). transparent=true면 알파 블렌딩(유리).
    private static Material MakeUnlitMat(Color c, bool transparent)
    {
        var sh = Shader.Find("Universal Render Pipeline/Unlit");
        if (sh == null) sh = Shader.Find("Unlit/Color");
        var m = new Material(sh);
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Color")) m.SetColor("_Color", c);
        if (transparent)
        {
            m.SetFloat("_Surface", 1f); // 0=Opaque, 1=Transparent
            m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetInt("_ZWrite", 0);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.DisableKeyword("_SURFACE_TYPE_OPAQUE");
            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }
        return m;
    }

    // 벽(큐브). 기본 BoxCollider가 남아 플레이어를 가둔다.
    private static void CreateWall(string name, Vector3 pos, Vector3 scale, Material mat)
    {
        var w = GameObject.CreatePrimitive(PrimitiveType.Cube);
        w.name = name;
        w.transform.position = pos;
        w.transform.localScale = scale;
        w.GetComponent<Renderer>().sharedMaterial = mat;
    }

    // 장식용 박스(콜라이더 제거 — 순수 비주얼, 플레이어 안 막음)
    private static GameObject CreateBox(string name, Vector3 pos, Vector3 scale, Vector3 euler, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.position = pos;
        go.transform.localScale = scale;
        go.transform.rotation = Quaternion.Euler(euler);
        go.GetComponent<Renderer>().sharedMaterial = mat;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        return go;
    }

    // 절차적 우드 플랭크 텍스처를 만들어 PNG 에셋으로 저장하고 반환한다.
    private static Texture2D MakeWoodTexture()
    {
        const string rel = GameDir + "/Materials/floor_wood.png";
        int W = 512, H = 512, planks = 6, ph = H / planks;
        var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
        var rng = new System.Random(7);
        var baseCol = new Color(0.60f, 0.43f, 0.27f);
        var tone = new float[planks];
        for (int i = 0; i < planks; i++) tone[i] = 0.82f + (float)rng.NextDouble() * 0.30f;

        for (int y = 0; y < H; y++)
        {
            int p = Mathf.Clamp(y / ph, 0, planks - 1);
            int yIn = y % ph;
            for (int x = 0; x < W; x++)
            {
                float grain = Mathf.Sin(x * 0.07f + p * 9f) * 0.5f + 0.5f;     // 결무늬
                float n = (float)rng.NextDouble();
                float shade = tone[p] * (0.92f + grain * 0.06f) * (0.97f + n * 0.03f);
                if (yIn < 2) shade *= 0.55f;                                    // 판자 가로 이음새
                int joint = (p % 2 == 0) ? 0 : 64;
                if (((x + joint) % 128) < 2) shade *= 0.6f;                     // 세로 이음새(엇갈림)
                tex.SetPixel(x, y, baseCol * shade);
            }
        }
        tex.Apply();

        string abs = Path.Combine(Application.dataPath, "Games/Library/Materials/floor_wood.png");
        Directory.CreateDirectory(Path.GetDirectoryName(abs));
        File.WriteAllBytes(abs, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(rel);
        var imp = AssetImporter.GetAtPath(rel) as TextureImporter;
        if (imp != null) { imp.wrapMode = TextureWrapMode.Repeat; imp.SaveAndReimport(); }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(rel);
    }

    // ★★★ [임시 플레이스홀더 — 나중에 FBX 모델로 교체 예정] ★★★
    // 사진 느낌으로 실내를 박스 도형으로 임시 구성: 통로 / 에스컬레이터 / 화분 / 벤치 / 2층 발코니.
    // 사용자가 별마당 실내(에스컬레이터·통로·화분·벤치·발코니 등)를 담은 FBX를 주면 이 메서드를 통째로 대체한다.
    //   교체 방법(FBX 받으면):
    //   1) FBX를 Assets/Games/Library/Models/ 에 넣고
    //   2) 아래 박스 생성 코드를 지우고, 책탑 배치(위 FixToURP/스케일)와 동일하게
    //      PrefabUtility.InstantiatePrefab(InteriorFbx) → FixToURP → 바닥(y=0) 앉히기 → 위치 배치 로 바꾼다.
    //   3) 책탑 위치(오른쪽 앞 사이드)·바닥(woodMat)·유리벽·천장은 그대로 두거나 FBX에 포함되면 제거.
    // ※ 이 박스들은 모두 콜라이더 없는 순수 비주얼이라 지워도 게임 로직(이동/책찾기/F)에 영향 없음.
    private static void BuildDecor(float room, float wallH)
    {
        float half = room * 0.5f;
        var metal = MakeUnlitMat(new Color(0.60f, 0.62f, 0.65f, 1f), false);
        var step  = MakeUnlitMat(new Color(0.32f, 0.33f, 0.35f, 1f), false);
        var plant = MakeUnlitMat(new Color(0.30f, 0.55f, 0.28f, 1f), false);
        var pot   = MakeUnlitMat(new Color(0.30f, 0.22f, 0.18f, 1f), false);
        var bench = MakeUnlitMat(new Color(0.46f, 0.31f, 0.19f, 1f), false);
        var path  = MakeUnlitMat(new Color(0.84f, 0.81f, 0.74f, 1f), false);
        var rail  = MakeUnlitMat(new Color(0.30f, 0.22f, 0.15f, 1f), false);

        // 밝은 통로(길) — 입구 앞에서 책탑(오른쪽 앞)으로
        CreateBox("Path_Main",  new Vector3(0f, 0.02f, -6f), new Vector3(9f, 0.04f, room * 0.8f), Vector3.zero, path);
        CreateBox("Path_Right", new Vector3(16f, 0.02f, 2f), new Vector3(18f, 0.04f, 9f), Vector3.zero, path);

        // 에스컬레이터(왼쪽 앞) — 사진처럼 비스듬히 올라감
        CreateBox("Esc_Ramp",  new Vector3(-26f, 4.2f, -8f), new Vector3(6f, 0.6f, 20f), new Vector3(-28f, 0f, 0f), step);
        CreateBox("Esc_RailL", new Vector3(-29f, 5.2f, -8f), new Vector3(0.4f, 1.6f, 20f), new Vector3(-28f, 0f, 0f), metal);
        CreateBox("Esc_RailR", new Vector3(-23f, 5.2f, -8f), new Vector3(0.4f, 1.6f, 20f), new Vector3(-28f, 0f, 0f), metal);
        CreateBox("Esc_Top",   new Vector3(-26f, 8.6f, 2.5f), new Vector3(6f, 0.6f, 6f), Vector3.zero, step);

        // 화분 4개(코너)
        for (int i = 0; i < 4; i++)
        {
            float sx = (i % 2 == 0) ? -1f : 1f, sz = (i < 2) ? -1f : 1f;
            Vector3 pp = new Vector3(sx * (half - 8f), 0f, sz * (half - 8f));
            CreateBox($"Pot_{i}",   pp + Vector3.up * 0.8f, new Vector3(3f, 1.6f, 3f), Vector3.zero, pot);
            CreateBox($"Plant_{i}", pp + Vector3.up * 2.6f, new Vector3(3.4f, 2.4f, 3.4f), Vector3.zero, plant);
        }

        // 벤치(앞쪽 열린 공간)
        CreateBox("Bench_1", new Vector3(-12f, 0.6f, -20f), new Vector3(7f, 1.2f, 2.5f), Vector3.zero, bench);
        CreateBox("Bench_2", new Vector3(10f, 0.6f, -22f), new Vector3(7f, 1.2f, 2.5f), Vector3.zero, bench);

        // 2층 발코니(뒤쪽) — 사진 상부 링 느낌
        CreateBox("Balcony",      new Vector3(0f, 16f, half - 6f), new Vector3(room, 1f, 10f), Vector3.zero, bench);
        CreateBox("Balcony_Rail", new Vector3(0f, 17.6f, half - 11f), new Vector3(room, 2f, 0.4f), Vector3.zero, rail);
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

    // 위치 지정 텍스트(둥근 카드 안 제목/본문용)
    private static TextMeshProUGUI MakeLabel(string name, Transform parent, string text, float size, TextAlignmentOptions align,
        Vector2 aMin, Vector2 aMax, Vector2 pivot, Vector2 pos, Vector2 sizeD, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        SetRect(go, aMin, aMax, pivot, pos, sizeD);
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = text; t.fontSize = size; t.alignment = align; t.color = color;
        t.raycastTarget = false; t.richText = true;
        return t;
    }

    // 둥근 9-슬라이스 이미지(카드/알약)
    private static Image MakeSliced(string name, Transform parent, Sprite sprite, Color color,
        Vector2 aMin, Vector2 aMax, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        SetRect(go, aMin, aMax, pivot, pos, size);
        var img = go.GetComponent<Image>();
        img.sprite = sprite; img.type = Image.Type.Sliced; img.color = color;
        return img;
    }

    // 둥근 버튼(라벨 포함)
    private static GameObject MakePrettyButton(string name, Transform parent, Sprite sprite, string label, Color color,
        Vector2 aMin, Vector2 aMax, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        SetRect(go, aMin, aMax, pivot, pos, size);
        var img = go.GetComponent<Image>();
        img.sprite = sprite; img.type = Image.Type.Sliced; img.color = color;
        var btn = go.GetComponent<Button>();
        var cb = btn.colors;
        cb.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f);
        cb.pressedColor = new Color(0.82f, 0.82f, 0.82f, 1f);
        cb.fadeDuration = 0.08f;
        btn.colors = cb;
        MakeLabel("Label", go.transform, label, 36, TextAlignmentOptions.Center,
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, Color.white);
        return go;
    }

    // 둥근 모서리 9-슬라이스 스프라이트 PNG 생성
    private static Sprite MakeRoundedSprite(string name, int size, int radius)
    {
        string path = $"{GameDir}/Sprites/{name}.png";
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var px = new Color[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                px[y * size + x] = RoundedInside(x, y, size, radius) ? Color.white : new Color(1f, 1f, 1f, 0f);
        tex.SetPixels(px); tex.Apply();
        File.WriteAllBytes(Path.Combine(Application.dataPath, $"Games/Library/Sprites/{name}.png"), tex.EncodeToPNG());
        Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        var imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (imp != null)
        {
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
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
