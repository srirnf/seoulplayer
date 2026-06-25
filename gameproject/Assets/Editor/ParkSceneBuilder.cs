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
        // 록온 선택 링 + 충전 게이지 + 타이밍 바 (3배 해상도 @ PPU 300 → 월드 크기 동일, 선명하게)
        Sprite ringS = MakeRing("sel_ring", 384, 36, "#ffce2e", 300f);
        Sprite chargeTrackS = MakeRoundedFill("charge_track", 450, 72, "#222831", SpriteAlignment.Center, 300f);
        Sprite chargeFillS = MakeRoundedFill("charge_fill", 450, 72, "#ffcf3f", SpriteAlignment.LeftCenter, 300f);
        Sprite trackS = MakeRoundedFill("timing_track", 660, 102, "#222831", SpriteAlignment.Center, 300f);
        Sprite zoneS = MakeRoundedFill("timing_zone", 144, 102, "#5fd06a", SpriteAlignment.Center, 300f);
        Sprite markerS = MakeRoundedFill("timing_marker", 42, 150, "#ff5a5a", SpriteAlignment.Center, 300f);
        Sprite mashFillS = MakeRoundedFill("mash_fill", 450, 72, "#ff9a3c", SpriteAlignment.LeftCenter, 300f);
        Sprite arrowS = MakeTriangle("arrow_tri", 96, "#ffffff", 280f);

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
            BuildAnimal(animals[i].name, new Vector3(x, playY, 0f), body, ringS, chargeTrackS, chargeFillS, trackS, zoneS, markerS, mashFillS, arrowS, leftEdge, rightEdge);
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

        // ===== 게임방법 안내 시작 화면 (슬라이드) =====
        var howTo = MakePanel("HowToPanel", canvasGO.transform, new Color(0f, 0f, 0f, 0.62f),
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        var htCard = MakeSliced("Card", howTo.transform, round, new Color(0.99f, 0.97f, 0.90f, 1f),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1000, 720));
        Color brown = new Color(0.27f, 0.23f, 0.18f);
        Color green = new Color(0.16f, 0.46f, 0.26f);
        Vector2 C = new Vector2(0.5f, 0.5f);

        var slideList = new Object[5];

        // 0) 개요
        var s0 = MakeSlide(htCard.transform, "동물꼬시기", green,
            "서울어린이대공원에서 탈출한 동물들을 다시 우리로 보내자!\n\n" +
            "· ← → (또는 A / D) 로 좌우 이동\n" +
            "· 동물 가까이서 Space 를 길게 눌러 충전 → 록온\n" +
            "· 떼면 미니게임이 랜덤으로 등장 (▶ 로 넘겨보세요)\n" +
            "· 성공하면 따라오고, 실패하면 잠깐 도망가요\n" +
            "· 동물을 모을수록 미니게임이 어려워져요\n\n" +
            "모든 동물을 모으면 클리어! (걸린 시간 기록)", brown);
        slideList[0] = s0;

        // 1) 타이밍
        var s1 = MakeSlide(htCard.transform, "① 타이밍", green,
            "빨간 마커가 좌우로 움직여요.\n초록 구간에 왔을 때 Space 를 누르세요!", brown);
        IllustBar(s1.transform, round);
        MakeSliced("Zone", s1.transform, round, new Color(0.37f, 0.81f, 0.45f), C, C, C, new Vector2(0, -70), new Vector2(150, 66));
        MakeSliced("Marker", s1.transform, round, new Color(1f, 0.35f, 0.35f), C, C, C, new Vector2(0, -70), new Vector2(18, 96));
        slideList[1] = s1;

        // 2) 움직이는 초록칸
        var s2 = MakeSlide(htCard.transform, "② 움직이는 초록칸", green,
            "마커는 가운데 고정! 초록칸이 움직여요.\n초록칸이 가운데 마커에 올 때 Space!", brown);
        IllustBar(s2.transform, round);
        MakeSliced("Zone", s2.transform, round, new Color(0.37f, 0.81f, 0.45f), C, C, C, new Vector2(-150, -70), new Vector2(150, 66));
        MakeSliced("Marker", s2.transform, round, new Color(1f, 0.35f, 0.35f), C, C, C, new Vector2(0, -70), new Vector2(18, 96));
        slideList[2] = s2;

        // 3) 연타
        var s3 = MakeSlide(htCard.transform, "③ 연타", green,
            "Space 를 빠르게 연타해서\n주황 게이지를 가득 채우세요! (안 누르면 줄어들어요)", brown);
        IllustBar(s3.transform, round);
        MakeSliced("Fill", s3.transform, round, new Color(1f, 0.6f, 0.24f), C, C, C, new Vector2(-130, -70), new Vector2(320, 66));
        slideList[3] = s3;

        // 4) 방향키 순서
        var s4 = MakeSlide(htCard.transform, "④ 방향키 순서", green,
            "화면에 뜬 화살표 순서대로\n방향키(↑ ↓ ← →)를 눌러요!", brown);
        MakeText("Arrows", s4.transform, "↑   →   ↓   ←", 70, TextAlignmentOptions.Center,
            C, C, C, new Vector2(0, -70), new Vector2(740, 130), green);
        slideList[4] = s4;

        // 카운터 + 네비게이션 버튼
        var counter = MakeText("Counter", htCard.transform, "1 / 5", 30, TextAlignmentOptions.Center,
            C, C, C, new Vector2(0, -228), new Vector2(220, 50), brown);
        var prevBtn = MakePrettyButton("PrevButton", htCard.transform, round, "이전",
            new Color(0.55f, 0.55f, 0.58f, 1f), C, C, C, new Vector2(-410, -30), new Vector2(130, 84));
        var nextBtn = MakePrettyButton("NextButton", htCard.transform, round, "다음",
            new Color(0.32f, 0.56f, 0.86f, 1f), C, C, C, new Vector2(410, -30), new Vector2(130, 84));
        var startBtn = MakePrettyButton("StartButton", htCard.transform, round, "게임 시작",
            new Color(0.30f, 0.62f, 0.36f, 1f), C, C, C, new Vector2(0, -300), new Vector2(340, 92));

        var slidesComp = howTo.AddComponent<HowToSlides>();
        SetArray(slidesComp, "slides", slideList);
        SetRef(slidesComp, "counter", counter);
        SetRef(slidesComp, "prevButton", prevBtn);
        SetRef(slidesComp, "nextButton", nextBtn);
        UnityEventTools.AddPersistentListener(prevBtn.GetComponent<Button>().onClick, slidesComp.Prev);
        UnityEventTools.AddPersistentListener(nextBtn.GetComponent<Button>().onClick, slidesComp.Next);
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
        Sprite chargeTrackS, Sprite chargeFillS, Sprite trackS, Sprite zoneS, Sprite markerS,
        Sprite mashFillS, Sprite arrowS, float minBound, float maxBound)
    {
        var go = new GameObject("Animal_" + name);
        go.transform.position = pos;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = body;
        sr.sortingOrder = 0;
        var animal = go.AddComponent<Animal>();
        animal.SetBounds(minBound, maxBound);

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

        // 연타 게임 바(머리 위): 트랙 + 채워지는 막대
        var mash = new GameObject("MashBar");
        mash.transform.SetParent(go.transform, false);
        mash.transform.localPosition = new Vector3(0f, h + 0.22f, 0f);
        var mtrack = new GameObject("Track");
        mtrack.transform.SetParent(mash.transform, false);
        var mtsr = mtrack.AddComponent<SpriteRenderer>();
        mtsr.sprite = chargeTrackS; mtsr.sortingOrder = 5;
        var mfill = new GameObject("Fill");
        mfill.transform.SetParent(mash.transform, false);
        mfill.transform.localPosition = new Vector3(-0.75f, 0f, 0.01f);
        var mfsr = mfill.AddComponent<SpriteRenderer>();
        mfsr.sprite = mashFillS; mfsr.sortingOrder = 6;
        mash.SetActive(false);

        // 방향키 게임: 화살표 4슬롯(머리 위)
        var arrows = new GameObject("ArrowRow");
        arrows.transform.SetParent(go.transform, false);
        arrows.transform.localPosition = new Vector3(0f, h + 0.32f, 0f);
        var slots = new Object[4];
        for (int i = 0; i < 4; i++)
        {
            var slot = new GameObject("Arrow" + i);
            slot.transform.SetParent(arrows.transform, false);
            slot.transform.localPosition = new Vector3((i - 1.5f) * 0.45f, 0f, 0f);
            var asr = slot.AddComponent<SpriteRenderer>();
            asr.sprite = arrowS;
            asr.sortingOrder = 6;
            slots[i] = asr;
        }
        arrows.SetActive(false);

        SetRef(animal, "selectionRing", ring);
        SetRef(animal, "chargeRoot", charge);
        SetRef(animal, "chargeFill", cfill.transform);
        SetRef(animal, "timingBar", bar);
        SetRef(animal, "zoneTransform", zone.transform);
        SetRef(animal, "marker", marker.transform);
        SetRef(animal, "mashRoot", mash);
        SetRef(animal, "mashFill", mfill.transform);
        SetRef(animal, "arrowRoot", arrows);
        SetArray(animal, "arrowSlots", slots);
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

    // 슬라이드 한 장(제목 + 설명). 그림은 호출 측에서 추가.
    private static GameObject MakeSlide(Transform parent, string title, Color titleColor, string body, Color bodyColor)
    {
        Vector2 C = new Vector2(0.5f, 0.5f);
        var s = new GameObject("Slide", typeof(RectTransform));
        s.transform.SetParent(parent, false);
        SetRect(s, C, C, C, Vector2.zero, new Vector2(1000, 720));
        MakeText("Title", s.transform, title, 50, TextAlignmentOptions.Center,
            C, C, C, new Vector2(0, 288), new Vector2(900, 90), titleColor);
        MakeText("Body", s.transform, body, 28, TextAlignmentOptions.Center,
            C, C, C, new Vector2(0, 130), new Vector2(880, 240), bodyColor);
        return s;
    }

    private static void IllustBar(Transform slide, Sprite round)
    {
        Vector2 C = new Vector2(0.5f, 0.5f);
        MakeSliced("Bar", slide, round, new Color(0.16f, 0.17f, 0.21f, 1f),
            C, C, C, new Vector2(0, -70), new Vector2(560, 66));
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
    private static Sprite MakeRoundedFill(string name, int w, int h, string fillHex, SpriteAlignment align, float ppu)
    {
        string path = $"{SpriteDir}/{name}.png";
        ColorUtility.TryParseHtmlString(fillHex, out Color fill);
        float r = Mathf.Min(w, h) * 0.5f;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        var px = new Color[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float cov = 0f; // 2x2 슈퍼샘플 AA
                for (int sy = 0; sy < 2; sy++)
                    for (int sx = 0; sx < 2; sx++)
                    {
                        float px2 = x + 0.25f + sx * 0.5f;
                        float py2 = y + 0.25f + sy * 0.5f;
                        float cx = Mathf.Clamp(px2, r, w - r);
                        float cy = Mathf.Clamp(py2, r, h - r);
                        float dx = px2 - cx, dy = py2 - cy;
                        if (dx * dx + dy * dy <= r * r) cov += 0.25f;
                    }
                px[y * w + x] = new Color(fill.r, fill.g, fill.b, fill.a * cov);
            }
        tex.SetPixels(px);
        tex.Apply();
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        ApplySpriteImport(path, align, ppu);
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    // 링(도넛) 월드 스프라이트 (록온 표시용)
    private static Sprite MakeRing(string name, int size, int thickness, string fillHex, float ppu)
    {
        string path = $"{SpriteDir}/{name}.png";
        ColorUtility.TryParseHtmlString(fillHex, out Color fill);
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var px = new Color[size * size];
        float c = (size - 1) * 0.5f;
        float outer = size * 0.5f - 1f;
        float inner = outer - thickness;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float cov = 0f; // 2x2 슈퍼샘플 AA
                for (int sy = 0; sy < 2; sy++)
                    for (int sx = 0; sx < 2; sx++)
                    {
                        float dx = (x + 0.25f + sx * 0.5f) - c;
                        float dy = (y + 0.25f + sy * 0.5f) - c;
                        float d = Mathf.Sqrt(dx * dx + dy * dy);
                        if (d <= outer && d >= inner) cov += 0.25f;
                    }
                px[y * size + x] = new Color(fill.r, fill.g, fill.b, fill.a * cov);
            }
        tex.SetPixels(px);
        tex.Apply();
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        ApplySpriteImport(path, SpriteAlignment.Center, ppu);
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    // 위쪽을 향한 삼각형(화살표용) - 회전해서 4방향으로 사용
    private static Sprite MakeTriangle(string name, int size, string fillHex, float ppu)
    {
        string path = $"{SpriteDir}/{name}.png";
        ColorUtility.TryParseHtmlString(fillHex, out Color fill);
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var px = new Color[size * size];
        float ax = size * 0.5f, ay = size - 2f; // top
        float bx = 2f, by = 2f;                  // bottom-left
        float cx = size - 2f, cy = 2f;           // bottom-right
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float cov = 0f;
                for (int sy = 0; sy < 2; sy++)
                    for (int sx = 0; sx < 2; sx++)
                        if (PointInTri(x + 0.25f + sx * 0.5f, y + 0.25f + sy * 0.5f, ax, ay, bx, by, cx, cy)) cov += 0.25f;
                px[y * size + x] = new Color(fill.r, fill.g, fill.b, cov);
            }
        tex.SetPixels(px);
        tex.Apply();
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        ApplySpriteImport(path, SpriteAlignment.Center, ppu);
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static bool PointInTri(float px, float py, float ax, float ay, float bx, float by, float cx, float cy)
    {
        float d1 = (px - bx) * (ay - by) - (ax - bx) * (py - by);
        float d2 = (px - cx) * (by - cy) - (bx - cx) * (py - cy);
        float d3 = (px - ax) * (cy - ay) - (cx - ax) * (py - ay);
        bool hasNeg = d1 < 0f || d2 < 0f || d3 < 0f;
        bool hasPos = d1 > 0f || d2 > 0f || d3 > 0f;
        return !(hasNeg && hasPos);
    }

    private static void ApplySpriteImport(string path, SpriteAlignment align, float ppu)
    {
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        var imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (imp == null) return;
        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = SpriteImportMode.Single;
        imp.spritePixelsPerUnit = ppu;
        imp.filterMode = FilterMode.Bilinear;
        imp.mipmapEnabled = false;
        imp.textureCompression = TextureImporterCompression.Uncompressed; // 압축 아티팩트 방지
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
