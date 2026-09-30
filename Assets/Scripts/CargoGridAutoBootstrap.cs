using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.ProceduralImage;
using TMPro;
using DG.Tweening;

public class CargoGridAutoBootstrap : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void OnSceneLoaded()
    {
        BuildRuntimeSetup();
    }

    public static void CleanAllOldObjects()
    {
        Canvas[] existingCanvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var c in existingCanvases)
        {
            if (Application.isPlaying) Object.Destroy(c.gameObject);
            else Object.DestroyImmediate(c.gameObject);
        }

        CargoGridGameManager[] oldGMs = Object.FindObjectsByType<CargoGridGameManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var g in oldGMs)
        {
            if (Application.isPlaying) Object.Destroy(g.gameObject);
            else Object.DestroyImmediate(g.gameObject);
        }
        CargoGridGameManager.instance = null;

        GameObject oldMgr = GameObject.Find("GameManager");
        if (oldMgr != null)
        {
            if (Application.isPlaying) Object.Destroy(oldMgr);
            else Object.DestroyImmediate(oldMgr);
        }

        AudioManager[] oldAudios = Object.FindObjectsByType<AudioManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var a in oldAudios)
        {
            if (Application.isPlaying) Object.Destroy(a.gameObject);
            else Object.DestroyImmediate(a.gameObject);
        }

        var existingES = Object.FindObjectsByType<UnityEngine.EventSystems.EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var es in existingES)
        {
            if (Application.isPlaying) Object.Destroy(es.gameObject);
            else Object.DestroyImmediate(es.gameObject);
        }
    }

    public static Sprite LoadSpriteSafe(string resourcePath)
    {
        Sprite sp = Resources.Load<Sprite>(resourcePath);
        if (sp == null)
        {
            Texture2D tex = Resources.Load<Texture2D>(resourcePath);
            if (tex != null)
            {
                sp = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            }
        }
        return sp;
    }

    public static ProceduralImage CreateProceduralCard(GameObject go, Color fillColor, float cornerRadius, float borderWidth = 0f, float falloff = 1.5f)
    {
        ProceduralImage pImg = go.AddComponent<ProceduralImage>();
        pImg.color = fillColor;
        pImg.FalloffDistance = falloff;
        UniformModifier mod = go.AddComponent<UniformModifier>();
        mod.Radius = cornerRadius;
        if (borderWidth > 0f)
        {
            pImg.BorderWidth = borderWidth;
        }
        return pImg;
    }

    public static void BuildRuntimeSetup()
    {
        Debug.Log("[CargoGrid] Bootstrapping Grade 5 Character-Driven Sci-Fi Experience...");

        // 1. Wipe out any old canvases or conflicting objects
        CleanAllOldObjects();

        // 2. Ensure EventSystem with appropriate input module and default actions
        GameObject esObj = new GameObject("EventSystem");
        esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
        var inputModule = esObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        inputModule.AssignDefaultActions();

        // Camera setup
        Camera cam = Camera.main;
        if (cam != null)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.02f, 0.04f, 0.08f, 1f);
            if (cam.GetComponent<AudioListener>() == null)
            {
                cam.gameObject.AddComponent<AudioListener>();
            }
        }

        // Load Sprites
        Sprite bgSprite = LoadSpriteSafe("CargoGrid/Sprites/cargo_bay_bg");
        Sprite conveyorTraySprite = LoadSpriteSafe("CargoGrid/Sprites/conveyor_tray_bg");
        Sprite bigTitleSprite = LoadSpriteSafe("CargoGrid/Sprites/big_title_logo");
        Sprite botSprite = LoadSpriteSafe("CargoGrid/Sprites/bot_character");
        Sprite crateSprite = LoadSpriteSafe("CargoGrid/Sprites/crate_item");
        Sprite generatorSprite = LoadSpriteSafe("CargoGrid/Sprites/generator_item");
        Sprite sensorSprite = LoadSpriteSafe("CargoGrid/Sprites/sensor_scanner");
        Sprite landingSprite = LoadSpriteSafe("CargoGrid/Sprites/landing_pads");
        Sprite victorySprite = LoadSpriteSafe("CargoGrid/Sprites/final_victory");
        Sprite muteSprite = LoadSpriteSafe("CargoGrid/Sprites/Mute");
        Sprite unmuteSprite = LoadSpriteSafe("CargoGrid/Sprites/UnMute");

        // Load Audio Clips
        AudioClip bgmClip = Resources.Load<AudioClip>("CargoGrid/Audio/Up-on-a-Housetop-chosic.com_");
        AudioClip correctClip = Resources.Load<AudioClip>("CargoGrid/Audio/Correct Sound");
        AudioClip wrongClip = Resources.Load<AudioClip>("CargoGrid/Audio/Wrong");

        // Load Font
        TMP_FontAsset fontAsset = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");

        // Create Canvas
        GameObject canvasObj = new GameObject("Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1 | AdditionalCanvasShaderChannels.TexCoord2 | AdditionalCanvasShaderChannels.TexCoord3;
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObj.AddComponent<GraphicRaycaster>();

        // Audio Manager
        GameObject audioMgrObj = new GameObject("AudioManager");
        AudioManager audioMgr = audioMgrObj.AddComponent<AudioManager>();
        AudioSource musicSrc = audioMgrObj.AddComponent<AudioSource>();
        musicSrc.playOnAwake = false;
        AudioSource sfxSrc = audioMgrObj.AddComponent<AudioSource>();
        sfxSrc.playOnAwake = false;

        audioMgr.Initialize(musicSrc, sfxSrc, bgmClip, correctClip, wrongClip);

        // Flash Overlay
        GameObject flashObj = CreateUIElement("ScreenFlashOverlay", canvasObj.transform);
        StretchFull(flashObj.GetComponent<RectTransform>());
        Image flashImg = flashObj.AddComponent<Image>();
        flashImg.color = new Color(1f, 1f, 1f, 0f);
        flashImg.raycastTarget = false;

        // Ambient Lighting Glow
        GameObject bayGlowObj = CreateUIElement("BayLightingGlow", canvasObj.transform);
        StretchFull(bayGlowObj.GetComponent<RectTransform>());
        Image bayGlowImg = bayGlowObj.AddComponent<Image>();
        bayGlowImg.color = new Color(0f, 0.7f, 1f, 0.05f);
        bayGlowImg.raycastTarget = false;

        // =====================================================================
        // SCREEN 1: BRAND NEW VIBRANT GRADE-5 MAIN MENU (30% TITLE + MASCOT ROBOT)
        // =====================================================================
        GameObject screenInstructions = CreateUIElement("Screen_Instructions", canvasObj.transform);
        StretchFull(screenInstructions.GetComponent<RectTransform>());

        GameObject menuBgObj = CreateUIElement("MenuBackground", screenInstructions.transform);
        StretchFull(menuBgObj.GetComponent<RectTransform>());
        Image menuBgImg = menuBgObj.AddComponent<Image>();
        menuBgImg.sprite = bgSprite;
        menuBgImg.color = new Color(0.85f, 0.9f, 1f, 1f);
        menuBgImg.raycastTarget = false;

        GameObject menuDarkOverlay = CreateUIElement("MenuDarkOverlay", screenInstructions.transform);
        StretchFull(menuDarkOverlay.GetComponent<RectTransform>());
        Image menuDarkImg = menuDarkOverlay.AddComponent<Image>();
        menuDarkImg.color = new Color(0.03f, 0.05f, 0.12f, 0.75f);
        menuDarkImg.raycastTarget = false;

        // TITLE BACK GLOW (Soft holographic radial aura so logo blends seamlessly)
        GameObject titleGlow = CreateUIElement("TitleBackGlow", screenInstructions.transform);
        RectTransform titleGlowRT = titleGlow.GetComponent<RectTransform>();
        titleGlowRT.anchorMin = new Vector2(0.5f, 1f);
        titleGlowRT.anchorMax = new Vector2(0.5f, 1f);
        titleGlowRT.pivot = new Vector2(0.5f, 1f);
        titleGlowRT.anchoredPosition = new Vector2(0, -30);
        titleGlowRT.sizeDelta = new Vector2(980, 290);
        Image titleGlowImg = titleGlow.AddComponent<Image>();
        titleGlowImg.color = new Color(0f, 0.75f, 1f, 0.08f);
        titleGlowImg.raycastTarget = false;

        // TITLE LOGO (30% SCREEN HEIGHT WITH GORGEOUS TRANSPARENCY)
        GameObject titleLogoObj = CreateUIElement("BigTitleLogo", screenInstructions.transform);
        RectTransform titleLogoRT = titleLogoObj.GetComponent<RectTransform>();
        titleLogoRT.anchorMin = new Vector2(0.5f, 1f);
        titleLogoRT.anchorMax = new Vector2(0.5f, 1f);
        titleLogoRT.pivot = new Vector2(0.5f, 1f);
        titleLogoRT.anchoredPosition = new Vector2(0, -25);
        titleLogoRT.sizeDelta = new Vector2(960, 310);
        Image titleLogoImg = titleLogoObj.AddComponent<Image>();
        titleLogoImg.sprite = bigTitleSprite;
        titleLogoImg.preserveAspect = true;
        titleLogoImg.raycastTarget = false;

        // Floating Title Hover Pulse
        titleLogoRT.DOAnchorPosY(-35f, 2.0f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);

        // Content Area (Mascot Robot on Left, Mission Briefing Card on Right)
        GameObject menuContentArea = CreateUIElement("MenuContentArea", screenInstructions.transform);
        RectTransform menuContentRT = menuContentArea.GetComponent<RectTransform>();
        menuContentRT.anchorMin = new Vector2(0.5f, 0.5f);
        menuContentRT.anchorMax = new Vector2(0.5f, 0.5f);
        menuContentRT.pivot = new Vector2(0.5f, 0.5f);
        menuContentRT.anchoredPosition = new Vector2(0, -85);
        menuContentRT.sizeDelta = new Vector2(1650, 480);

        // HOLOGRAPHIC PEDESTAL DOCK (Grounds LOAD-E firmly into the cargo bay floor)
        GameObject botPedestal = CreateUIElement("MascotPedestal", menuContentArea.transform);
        RectTransform botPedRT = botPedestal.GetComponent<RectTransform>();
        botPedRT.anchorMin = new Vector2(0f, 0.5f);
        botPedRT.anchorMax = new Vector2(0f, 0.5f);
        botPedRT.pivot = new Vector2(0.5f, 0.5f);
        botPedRT.anchoredPosition = new Vector2(250, -200);
        botPedRT.sizeDelta = new Vector2(400, 60);
        Image botPedImg = botPedestal.AddComponent<Image>();
        botPedImg.color = new Color(0f, 0.9f, 1f, 0.22f);
        botPedImg.raycastTarget = false;

        // LEFT: ADORABLE MASCOT ROBOT "LOAD-E" (Clean Transparent PNG)
        GameObject botObj = CreateUIElement("MascotBot", menuContentArea.transform);
        RectTransform botRT = botObj.GetComponent<RectTransform>();
        botRT.anchorMin = new Vector2(0f, 0.5f);
        botRT.anchorMax = new Vector2(0f, 0.5f);
        botRT.pivot = new Vector2(0.5f, 0.5f);
        botRT.anchoredPosition = new Vector2(250, 0);
        botRT.sizeDelta = new Vector2(460, 460);
        Image botImg = botObj.AddComponent<Image>();
        botImg.sprite = botSprite;
        botImg.preserveAspect = true;
        botImg.raycastTarget = false;

        // RIGHT: SPEECH BUBBLE / MISSION PROTOCOLS
        GameObject speechCard = CreateUIElement("MascotSpeechCard", menuContentArea.transform);
        RectTransform speechCardRT = speechCard.GetComponent<RectTransform>();
        speechCardRT.anchorMin = new Vector2(1f, 0.5f);
        speechCardRT.anchorMax = new Vector2(1f, 0.5f);
        speechCardRT.pivot = new Vector2(1f, 0.5f);
        speechCardRT.anchoredPosition = new Vector2(-20, 0);
        speechCardRT.sizeDelta = new Vector2(1100, 460);
        CreateProceduralCard(speechCard, new Color(0.05f, 0.09f, 0.18f, 0.96f), 20f);
        Button speechCardBtn = speechCard.AddComponent<Button>();

        // Top Cyan Glow Line
        GameObject speechAccent = CreateUIElement("SpeechAccentBar", speechCard.transform);
        RectTransform accentRT = speechAccent.GetComponent<RectTransform>();
        accentRT.anchorMin = new Vector2(0, 1);
        accentRT.anchorMax = new Vector2(1, 1);
        accentRT.pivot = new Vector2(0.5f, 1);
        accentRT.anchoredPosition = Vector2.zero;
        accentRT.sizeDelta = new Vector2(0, 6);
        CreateProceduralCard(speechAccent, new Color(0f, 0.95f, 1f, 0.9f), 3f);

        // Robot Header Greeting
        GameObject botGreetingObj = CreateUIElement("BotGreeting", speechCard.transform);
        RectTransform greetRT = botGreetingObj.GetComponent<RectTransform>();
        greetRT.anchorMin = new Vector2(0.5f, 1f);
        greetRT.anchorMax = new Vector2(0.5f, 1f);
        greetRT.pivot = new Vector2(0.5f, 1f);
        greetRT.anchoredPosition = new Vector2(0, -18);
        greetRT.sizeDelta = new Vector2(1040, 50);
        TextMeshProUGUI greetTMP = botGreetingObj.AddComponent<TextMeshProUGUI>();
        if (fontAsset != null) greetTMP.font = fontAsset;
        greetTMP.text = "<color=#FFD700><b>ROBOT CARGO BUDDY LOAD-E SAYS:</b></color> <color=#00F0FF>\"HEY CADET! HELP ME BALANCE THE SHIP!\"</color>";
        greetTMP.fontSize = 24;
        greetTMP.fontStyle = FontStyles.Bold;
        greetTMP.alignment = TextAlignmentOptions.MidlineLeft;
        greetTMP.raycastTarget = false;

        // Mission Instructions Grid
        GameObject missionListObj = CreateUIElement("MissionList", speechCard.transform);
        RectTransform listRT = missionListObj.GetComponent<RectTransform>();
        listRT.anchorMin = new Vector2(0.5f, 0.5f);
        listRT.anchorMax = new Vector2(0.5f, 0.5f);
        listRT.pivot = new Vector2(0.5f, 0.5f);
        listRT.anchoredPosition = new Vector2(0, -20);
        listRT.sizeDelta = new Vector2(1040, 340);
        TextMeshProUGUI listTMP = missionListObj.AddComponent<TextMeshProUGUI>();
        if (fontAsset != null) listTMP.font = fontAsset;
        listTMP.text = "<b><color=#00F0FF>SECTOR 1: THE POPULAR PACKAGE (MODE)</color></b>\n" +
            "<color=#E2E8F4>Find the cargo number that appears most often in the supply load!</color>\n\n" +
            "<b><color=#2AFFA2>SECTOR 2: THE CENTER CONVEYOR (MEDIAN)</color></b>\n" +
            "<color=#E2E8F4>Line up cargo weights in order and tap the container right in the exact center!</color>\n\n" +
            "<b><color=#FFB800>SECTOR 3: FAIR SHARE BATTERIES (MEAN)</color></b>\n" +
            "<color=#E2E8F4>Share energy rods equally until all four generator towers hold the exact same power!</color>\n\n" +
            "<b><color=#99B4FF>SECTOR 4: DISTANCE GAP (RANGE)</color></b>  -  <b><color=#00F0FF>SECTOR 5: SAFE LANDING</color></b>\n" +
            "<color=#E2E8F4>Measure sensor distance spreads and pick the calmest landing pad for takeoff!</color>";
        listTMP.fontSize = 20;
        listTMP.lineSpacing = 12f;
        listTMP.alignment = TextAlignmentOptions.TopLeft;
        listTMP.raycastTarget = false;

        // HUGE JUICY START BUTTON
        GameObject startBtnObj = CreateUIElement("StartButton", screenInstructions.transform);
        RectTransform startBtnRT = startBtnObj.GetComponent<RectTransform>();
        startBtnRT.anchorMin = new Vector2(0.5f, 0f);
        startBtnRT.anchorMax = new Vector2(0.5f, 0f);
        startBtnRT.pivot = new Vector2(0.5f, 0f);
        startBtnRT.anchoredPosition = new Vector2(0, 35);
        startBtnRT.sizeDelta = new Vector2(660, 95);
        ProceduralImage startBtnPImg = CreateProceduralCard(startBtnObj, new Color(0.08f, 0.94f, 0.48f, 1f), 24f);
        startBtnPImg.raycastTarget = true;

        Button startBtn = startBtnObj.AddComponent<Button>();
        ColorBlock btnColors = startBtn.colors;
        btnColors.normalColor = new Color(0.08f, 0.94f, 0.48f, 1f);
        btnColors.highlightedColor = new Color(0.2f, 1f, 0.6f, 1f);
        btnColors.pressedColor = new Color(0.04f, 0.72f, 0.35f, 1f);
        startBtn.colors = btnColors;

        UIHoverClickEffect hover = startBtnObj.AddComponent<UIHoverClickEffect>();
        hover.hoverScale = 1.05f;
        hover.clickScale = 0.95f;
        hover.hoverDuration = 0.1f;
        hover.enableIdlePulse = false;

        GameObject startBtnText = CreateUIElement("Text", startBtnObj.transform);
        StretchFull(startBtnText.GetComponent<RectTransform>());
        TextMeshProUGUI startTMP = startBtnText.AddComponent<TextMeshProUGUI>();
        if (fontAsset != null) startTMP.font = fontAsset;
        startTMP.text = "START CALIBRATION MISSION!  >";
        startTMP.fontSize = 28;
        startTMP.fontStyle = FontStyles.Bold;
        startTMP.color = new Color(0.02f, 0.08f, 0.14f, 1f);
        startTMP.alignment = TextAlignmentOptions.Center;
        startTMP.raycastTarget = false; // MUST be false so text never blocks button clicks!

        // =====================================================================
        // SCREEN 2: GAMEPLAY ACTIVITY (BAY VIEWSCREEN + CONVEYOR TRAY)
        // =====================================================================
        GameObject screenActivity = CreateUIElement("Screen_Activity", canvasObj.transform);
        StretchFull(screenActivity.GetComponent<RectTransform>());
        screenActivity.SetActive(false);

        GameObject actBgObj = CreateUIElement("ActivityBackground", screenActivity.transform);
        StretchFull(actBgObj.GetComponent<RectTransform>());
        Image actBgImg = actBgObj.AddComponent<Image>();
        actBgImg.sprite = bgSprite;
        actBgImg.color = new Color(0.85f, 0.9f, 1f, 1f);
        actBgImg.raycastTarget = false;

        GameObject actDarkOverlay = CreateUIElement("ActivityDarkOverlay", screenActivity.transform);
        StretchFull(actDarkOverlay.GetComponent<RectTransform>());
        Image actDarkImg = actDarkOverlay.AddComponent<Image>();
        actDarkImg.color = new Color(0.02f, 0.04f, 0.08f, 0.55f);
        actDarkImg.raycastTarget = false;

        // TOP HUD BAR
        GameObject topBar = CreateUIElement("TopBar", screenActivity.transform);
        RectTransform topBarRT = topBar.GetComponent<RectTransform>();
        topBarRT.anchorMin = new Vector2(0, 1);
        topBarRT.anchorMax = new Vector2(1, 1);
        topBarRT.pivot = new Vector2(0.5f, 1);
        topBarRT.anchoredPosition = Vector2.zero;
        topBarRT.sizeDelta = new Vector2(0, 75);
        CreateProceduralCard(topBar, new Color(0.03f, 0.06f, 0.12f, 0.96f), 0f);

        // Top Title Capsule Badge
        GameObject topTitleCapsule = CreateUIElement("TopTitleCapsule", topBar.transform);
        RectTransform topTitleCapsuleRT = topTitleCapsule.GetComponent<RectTransform>();
        topTitleCapsuleRT.anchorMin = new Vector2(0, 0.5f);
        topTitleCapsuleRT.anchorMax = new Vector2(0, 0.5f);
        topTitleCapsuleRT.pivot = new Vector2(0, 0.5f);
        topTitleCapsuleRT.anchoredPosition = new Vector2(25, 0);
        topTitleCapsuleRT.sizeDelta = new Vector2(420, 48);
        CreateProceduralCard(topTitleCapsule, new Color(0.06f, 0.11f, 0.22f, 0.92f), 12f);

        GameObject topTitleObj = CreateUIElement("TopTitleText", topTitleCapsule.transform);
        StretchFull(topTitleObj.GetComponent<RectTransform>());
        TextMeshProUGUI topTitleTMP = topTitleObj.AddComponent<TextMeshProUGUI>();
        if (fontAsset != null) topTitleTMP.font = fontAsset;
        topTitleTMP.text = "<color=#00F0FF>CARGO CALIBRATION</color>  |  <color=#FFD700>PILOT: CADET</color>";
        topTitleTMP.fontSize = 18;
        topTitleTMP.fontStyle = FontStyles.Bold;
        topTitleTMP.alignment = TextAlignmentOptions.Center;
        topTitleTMP.raycastTarget = false;

        // Sector Progression Pills (FIX: childControlWidth = true prevents overlapping!)
        GameObject stageDotsContainer = CreateUIElement("StageDotsContainer", topBar.transform);
        RectTransform dotsContainerRT = stageDotsContainer.GetComponent<RectTransform>();
        dotsContainerRT.anchorMin = new Vector2(0.35f, 0.5f);
        dotsContainerRT.anchorMax = new Vector2(0.35f, 0.5f);
        dotsContainerRT.pivot = new Vector2(0.5f, 0.5f);
        dotsContainerRT.anchoredPosition = Vector2.zero;
        dotsContainerRT.sizeDelta = new Vector2(430, 46);

        HorizontalLayoutGroup dotsLayout = stageDotsContainer.AddComponent<HorizontalLayoutGroup>();
        dotsLayout.spacing = 10;
        dotsLayout.childAlignment = TextAnchor.MiddleCenter;
        dotsLayout.childControlWidth = true;   // REQUIRED: Ensures children are arranged along X!
        dotsLayout.childControlHeight = true;  // REQUIRED: Ensures children take correct height!
        dotsLayout.childForceExpandWidth = false;
        dotsLayout.childForceExpandHeight = false;

        Image[] stageDots = new Image[5];
        TextMeshProUGUI[] stageDotLabels = new TextMeshProUGUI[5];
        string[] dotTitles = { "SEC 1", "SEC 2", "SEC 3", "SEC 4", "SEC 5" };

        for (int i = 0; i < 5; i++)
        {
            GameObject dotObj = CreateUIElement($"SectorDot_{i + 1}", stageDotsContainer.transform);
            LayoutElement dotLE = dotObj.AddComponent<LayoutElement>();
            dotLE.preferredWidth = 74;
            dotLE.preferredHeight = 36;
            dotLE.minWidth = 74;
            dotLE.minHeight = 36;

            Color dotColor = (i == 0) ? new Color(0f, 0.95f, 1f, 1f) : new Color(0.1f, 0.16f, 0.28f, 0.85f);
            ProceduralImage dotPImg = CreateProceduralCard(dotObj, dotColor, 10f);
            dotPImg.raycastTarget = false;
            stageDots[i] = dotPImg;

            GameObject dotNumObj = CreateUIElement("Label", dotObj.transform);
            StretchFull(dotNumObj.GetComponent<RectTransform>());
            TextMeshProUGUI numTMP = dotNumObj.AddComponent<TextMeshProUGUI>();
            if (fontAsset != null) numTMP.font = fontAsset;
            numTMP.text = dotTitles[i];
            numTMP.fontSize = 13;
            numTMP.fontStyle = FontStyles.Bold;
            // High contrast text: active is dark bold navy so it's readable on neon cyan/green!
            numTMP.color = (i == 0) ? new Color(0.02f, 0.08f, 0.16f, 1f) : new Color(0.6f, 0.72f, 0.88f, 0.7f);
            numTMP.alignment = TextAlignmentOptions.Center;
            numTMP.raycastTarget = false;
            stageDotLabels[i] = numTMP;
        }

        // Score Capsule Badge
        GameObject scoreCapsule = CreateUIElement("ScoreCapsule", topBar.transform);
        RectTransform scoreCapsuleRT = scoreCapsule.GetComponent<RectTransform>();
        scoreCapsuleRT.anchorMin = new Vector2(0.58f, 0.5f);
        scoreCapsuleRT.anchorMax = new Vector2(0.58f, 0.5f);
        scoreCapsuleRT.pivot = new Vector2(0.5f, 0.5f);
        scoreCapsuleRT.anchoredPosition = Vector2.zero;
        scoreCapsuleRT.sizeDelta = new Vector2(175, 46);
        CreateProceduralCard(scoreCapsule, new Color(0.07f, 0.13f, 0.26f, 0.95f), 12f);

        GameObject scoreTextObj = CreateUIElement("ScoreText", scoreCapsule.transform);
        StretchFull(scoreTextObj.GetComponent<RectTransform>());
        TextMeshProUGUI scoreTMP = scoreTextObj.AddComponent<TextMeshProUGUI>();
        if (fontAsset != null) scoreTMP.font = fontAsset;
        scoreTMP.text = "SCORE: 0";
        scoreTMP.fontSize = 19;
        scoreTMP.fontStyle = FontStyles.Bold;
        scoreTMP.color = new Color(1f, 0.84f, 0f, 1f); // Vibrant Gold
        scoreTMP.alignment = TextAlignmentOptions.Center;
        scoreTMP.raycastTarget = false;

        // Streak Capsule Badge
        GameObject streakCapsule = CreateUIElement("StreakCapsule", topBar.transform);
        RectTransform streakCapsuleRT = streakCapsule.GetComponent<RectTransform>();
        streakCapsuleRT.anchorMin = new Vector2(0.70f, 0.5f);
        streakCapsuleRT.anchorMax = new Vector2(0.70f, 0.5f);
        streakCapsuleRT.pivot = new Vector2(0.5f, 0.5f);
        streakCapsuleRT.anchoredPosition = Vector2.zero;
        streakCapsuleRT.sizeDelta = new Vector2(150, 46);
        CreateProceduralCard(streakCapsule, new Color(0.07f, 0.13f, 0.26f, 0.95f), 12f);

        GameObject streakTextObj = CreateUIElement("StreakText", streakCapsule.transform);
        StretchFull(streakTextObj.GetComponent<RectTransform>());
        TextMeshProUGUI streakTMP = streakTextObj.AddComponent<TextMeshProUGUI>();
        if (fontAsset != null) streakTMP.font = fontAsset;
        streakTMP.text = "STREAK x1";
        streakTMP.fontSize = 18;
        streakTMP.fontStyle = FontStyles.Bold;
        streakTMP.color = new Color(0f, 0.95f, 1f, 1f); // Neon Cyan
        streakTMP.alignment = TextAlignmentOptions.Center;
        streakTMP.raycastTarget = false;

        // Energy Stability Meter (High-contrast dark bold text on neon green!)
        GameObject energyBarObj = CreateUIElement("EnergyStabilityMeter", topBar.transform);
        RectTransform energyRT = energyBarObj.GetComponent<RectTransform>();
        energyRT.anchorMin = new Vector2(1, 0.5f);
        energyRT.anchorMax = new Vector2(1, 0.5f);
        energyRT.pivot = new Vector2(1, 0.5f);
        energyRT.anchoredPosition = new Vector2(-85, 0);
        energyRT.sizeDelta = new Vector2(230, 42);
        CreateProceduralCard(energyBarObj, new Color(0.05f, 0.09f, 0.18f, 0.95f), 12f);

        GameObject energyFillObj = CreateUIElement("EnergyFill", energyBarObj.transform);
        RectTransform fillRT = energyFillObj.GetComponent<RectTransform>();
        StretchFull(fillRT);
        fillRT.offsetMin = new Vector2(3, 3);
        fillRT.offsetMax = new Vector2(-3, -3);
        Image energyFillImg = energyFillObj.AddComponent<Image>();
        energyFillImg.type = Image.Type.Filled;
        energyFillImg.fillMethod = Image.FillMethod.Horizontal;
        energyFillImg.fillAmount = 0.2f;
        energyFillImg.color = new Color(0.1f, 0.96f, 0.55f, 1f);
        energyFillImg.raycastTarget = false;

        GameObject energyLabelObj = CreateUIElement("EnergyLabel", energyBarObj.transform);
        StretchFull(energyLabelObj.GetComponent<RectTransform>());
        TextMeshProUGUI energyLabelTMP = energyLabelObj.AddComponent<TextMeshProUGUI>();
        if (fontAsset != null) energyLabelTMP.font = fontAsset;
        energyLabelTMP.text = "20% STABILITY";
        energyLabelTMP.fontSize = 16;
        energyLabelTMP.fontStyle = FontStyles.Bold;
        energyLabelTMP.color = new Color(0.02f, 0.08f, 0.16f, 1f); // 100% VISIBLE DARK NAVY ON BRIGHT GREEN!
        energyLabelTMP.alignment = TextAlignmentOptions.Center;
        energyLabelTMP.raycastTarget = false;

        // Mute Button
        GameObject muteBtnObj = CreateUIElement("MuteButton", topBar.transform);
        RectTransform muteRT = muteBtnObj.GetComponent<RectTransform>();
        muteRT.anchorMin = new Vector2(1, 0.5f);
        muteRT.anchorMax = new Vector2(1, 0.5f);
        muteRT.pivot = new Vector2(1, 0.5f);
        muteRT.anchoredPosition = new Vector2(-22, 0);
        muteRT.sizeDelta = new Vector2(46, 46);
        CreateProceduralCard(muteBtnObj, new Color(0.08f, 0.14f, 0.28f, 0.92f), 12f);

        GameObject muteIconObj = CreateUIElement("MuteIcon", muteBtnObj.transform);
        StretchFull(muteIconObj.GetComponent<RectTransform>());
        muteIconObj.GetComponent<RectTransform>().offsetMin = new Vector2(8, 8);
        muteIconObj.GetComponent<RectTransform>().offsetMax = new Vector2(-8, -8);
        Image muteImg = muteIconObj.AddComponent<Image>();
        muteImg.sprite = unmuteSprite != null ? unmuteSprite : muteSprite;
        muteImg.preserveAspect = true;
        muteImg.raycastTarget = false;

        Button muteBtn = muteBtnObj.AddComponent<Button>();
        UIHoverClickEffect muteHover = muteBtnObj.AddComponent<UIHoverClickEffect>();
        muteHover.enableIdlePulse = false;

        SetField(audioMgr, "muteButtonImage", muteImg);
        SetField(audioMgr, "muteSprite", muteSprite);
        SetField(audioMgr, "unmuteSprite", unmuteSprite);

        // ---------------------------------------------------------------------
        // UPPER CENTER VIEWSCREEN (TOP 56%)
        // ---------------------------------------------------------------------
        GameObject centerViewscreen = CreateUIElement("CenterBayViewscreen", screenActivity.transform);
        RectTransform centerRT = centerViewscreen.GetComponent<RectTransform>();
        centerRT.anchorMin = new Vector2(0.5f, 1f);
        centerRT.anchorMax = new Vector2(0.5f, 1f);
        centerRT.pivot = new Vector2(0.5f, 1f);
        centerRT.anchoredPosition = new Vector2(0, -85);
        centerRT.sizeDelta = new Vector2(1840, 545);

        // Left 33% Visual Frame
        GameObject visualFrame = CreateUIElement("VisualFrame", centerViewscreen.transform);
        RectTransform visFrameRT = visualFrame.GetComponent<RectTransform>();
        visFrameRT.anchorMin = new Vector2(0f, 0f);
        visFrameRT.anchorMax = new Vector2(0.33f, 1f);
        visFrameRT.anchoredPosition = Vector2.zero;
        visFrameRT.sizeDelta = Vector2.zero;
        CreateProceduralCard(visualFrame, new Color(0.04f, 0.08f, 0.16f, 0.96f), 20f);

        GameObject itemImgObj = CreateUIElement("ItemVisualImage", visualFrame.transform);
        RectTransform itemImgRT = itemImgObj.GetComponent<RectTransform>();
        itemImgRT.anchorMin = new Vector2(0.06f, 0.06f);
        itemImgRT.anchorMax = new Vector2(0.94f, 0.94f);
        itemImgRT.anchoredPosition = Vector2.zero;
        itemImgRT.sizeDelta = Vector2.zero;
        Image visualDisplayImg = itemImgObj.AddComponent<Image>();
        visualDisplayImg.sprite = crateSprite;
        visualDisplayImg.preserveAspect = true;
        visualDisplayImg.raycastTarget = false;

        // Gentle idle floating tween for the holographic item
        itemImgRT.DOAnchorPosY(10f, 2.2f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);

        // Right 67% Holographic Telemetry Console
        GameObject telemetryConsole = CreateUIElement("TelemetryConsole", centerViewscreen.transform);
        RectTransform telemRT = telemetryConsole.GetComponent<RectTransform>();
        telemRT.anchorMin = new Vector2(0.35f, 0f);
        telemRT.anchorMax = new Vector2(1f, 1f);
        telemRT.anchoredPosition = Vector2.zero;
        telemRT.sizeDelta = Vector2.zero;
        CreateProceduralCard(telemetryConsole, new Color(0.04f, 0.08f, 0.16f, 0.96f), 20f);

        // Stage Title (Bigger font!)
        GameObject stageTitleObj = CreateUIElement("StageTitleTMP", telemetryConsole.transform);
        RectTransform stageTitleRT = stageTitleObj.GetComponent<RectTransform>();
        stageTitleRT.anchorMin = new Vector2(0.025f, 1f);
        stageTitleRT.anchorMax = new Vector2(0.975f, 1f);
        stageTitleRT.pivot = new Vector2(0.5f, 1f);
        stageTitleRT.anchoredPosition = new Vector2(0, -14);
        stageTitleRT.sizeDelta = new Vector2(0, 38);
        TextMeshProUGUI stageTitleTMP = stageTitleObj.AddComponent<TextMeshProUGUI>();
        if (fontAsset != null) stageTitleTMP.font = fontAsset;
        stageTitleTMP.text = "[ LEVEL 01 ] • THE POPULAR PACKAGE";
        stageTitleTMP.fontSize = 26;
        stageTitleTMP.fontStyle = FontStyles.Bold;
        stageTitleTMP.color = new Color(0f, 0.95f, 1f, 1f);
        stageTitleTMP.alignment = TextAlignmentOptions.MidlineLeft;
        stageTitleTMP.raycastTarget = false;

        // Stage Subtitle
        GameObject stageSubObj = CreateUIElement("StageSubtitleTMP", telemetryConsole.transform);
        RectTransform stageSubRT = stageSubObj.GetComponent<RectTransform>();
        stageSubRT.anchorMin = new Vector2(0.025f, 1f);
        stageSubRT.anchorMax = new Vector2(0.975f, 1f);
        stageSubRT.pivot = new Vector2(0.5f, 1f);
        stageSubRT.anchoredPosition = new Vector2(0, -52);
        stageSubRT.sizeDelta = new Vector2(0, 28);
        TextMeshProUGUI stageSubTMP = stageSubObj.AddComponent<TextMeshProUGUI>();
        if (fontAsset != null) stageSubTMP.font = fontAsset;
        stageSubTMP.text = "TARGET CONCEPT: MODE (MOST FREQUENT VALUE)";
        stageSubTMP.fontSize = 17;
        stageSubTMP.fontStyle = FontStyles.Bold;
        stageSubTMP.color = new Color(1f, 0.8f, 0.2f, 1f);
        stageSubTMP.alignment = TextAlignmentOptions.MidlineLeft;
        stageSubTMP.raycastTarget = false;

        // Prompt Box (Incoming Supply Load - MUCH BIGGER FONT!)
        GameObject promptBoxObj = CreateUIElement("PromptBox", telemetryConsole.transform);
        RectTransform promptBoxRT = promptBoxObj.GetComponent<RectTransform>();
        promptBoxRT.anchorMin = new Vector2(0.025f, 0.46f);
        promptBoxRT.anchorMax = new Vector2(0.975f, 0.88f);
        promptBoxRT.anchoredPosition = Vector2.zero;
        promptBoxRT.sizeDelta = Vector2.zero;
        CreateProceduralCard(promptBoxObj, new Color(0.06f, 0.12f, 0.25f, 0.98f), 16f);

        GameObject promptTextObj = CreateUIElement("ScreenPromptTMP", promptBoxObj.transform);
        StretchFull(promptTextObj.GetComponent<RectTransform>());
        promptTextObj.GetComponent<RectTransform>().offsetMin = new Vector2(20, 8);
        promptTextObj.GetComponent<RectTransform>().offsetMax = new Vector2(-20, -8);
        TextMeshProUGUI screenPromptTMP = promptTextObj.AddComponent<TextMeshProUGUI>();
        if (fontAsset != null) screenPromptTMP.font = fontAsset;
        screenPromptTMP.text = "Incoming supply load:\n<color=#2AFFA2>14 → 22 → 14 → 19 → 14 → 35 → 22</color>";
        screenPromptTMP.fontSize = 32; // INCREASED FROM 25 to 32!
        screenPromptTMP.fontStyle = FontStyles.Bold;
        screenPromptTMP.color = Color.white;
        screenPromptTMP.alignment = TextAlignmentOptions.Center;
        screenPromptTMP.enableWordWrapping = true;
        screenPromptTMP.lineSpacing = 12f;
        screenPromptTMP.raycastTarget = false;

        // Task Directive Box (Bigger font!)
        GameObject taskBoxObj = CreateUIElement("TaskDirectiveBox", telemetryConsole.transform);
        RectTransform taskBoxRT = taskBoxObj.GetComponent<RectTransform>();
        taskBoxRT.anchorMin = new Vector2(0.025f, 0.26f);
        taskBoxRT.anchorMax = new Vector2(0.975f, 0.44f);
        taskBoxRT.anchoredPosition = Vector2.zero;
        taskBoxRT.sizeDelta = Vector2.zero;
        CreateProceduralCard(taskBoxObj, new Color(0.05f, 0.09f, 0.19f, 0.95f), 14f);

        GameObject taskTextObj = CreateUIElement("TaskPromptTMP", taskBoxObj.transform);
        StretchFull(taskTextObj.GetComponent<RectTransform>());
        taskTextObj.GetComponent<RectTransform>().offsetMin = new Vector2(20, 8);
        taskTextObj.GetComponent<RectTransform>().offsetMax = new Vector2(-20, -8);
        TextMeshProUGUI taskPromptTMP = taskTextObj.AddComponent<TextMeshProUGUI>();
        if (fontAsset != null) taskPromptTMP.font = fontAsset;
        taskPromptTMP.text = ">>> TASK: Tap the cargo container number that appears most often.";
        taskPromptTMP.fontSize = 24; // INCREASED FROM 20 to 24!
        taskPromptTMP.fontStyle = FontStyles.Bold;
        taskPromptTMP.color = new Color(1f, 0.88f, 0.25f, 1f);
        taskPromptTMP.alignment = TextAlignmentOptions.MidlineLeft;
        taskPromptTMP.enableWordWrapping = true;
        taskPromptTMP.raycastTarget = false;

        // Robot LOAD-E Speech / Guidance Row (PROMINENT & LARGE ROBOT!)
        GameObject botSpeechRow = CreateUIElement("BotSpeechRow", telemetryConsole.transform);
        RectTransform botSpeechRT = botSpeechRow.GetComponent<RectTransform>();
        botSpeechRT.anchorMin = new Vector2(0.025f, 0.035f);
        botSpeechRT.anchorMax = new Vector2(0.975f, 0.235f);
        botSpeechRT.anchoredPosition = Vector2.zero;
        botSpeechRT.sizeDelta = Vector2.zero;
        CreateProceduralCard(botSpeechRow, new Color(0.03f, 0.07f, 0.15f, 0.94f), 14f);

        // Big Robot Avatar (85x85 instead of tiny 55x55!)
        GameObject miniBotObj = CreateUIElement("MiniBotAvatar", botSpeechRow.transform);
        RectTransform miniBotRT = miniBotObj.GetComponent<RectTransform>();
        miniBotRT.anchorMin = new Vector2(0, 0.5f);
        miniBotRT.anchorMax = new Vector2(0, 0.5f);
        miniBotRT.pivot = new Vector2(0, 0.5f);
        miniBotRT.anchoredPosition = new Vector2(16, 0);
        miniBotRT.sizeDelta = new Vector2(85, 85);
        Image miniBotImg = miniBotObj.AddComponent<Image>();
        miniBotImg.sprite = botSprite;
        miniBotImg.preserveAspect = true;
        miniBotImg.raycastTarget = false;

        // Idle bounce for companion bot
        miniBotRT.DOAnchorPosY(4f, 1.3f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);

        GameObject botSpeechTextObj = CreateUIElement("BotSpeechText", botSpeechRow.transform);
        RectTransform botSpeechTextRT = botSpeechTextObj.GetComponent<RectTransform>();
        botSpeechTextRT.anchorMin = new Vector2(0, 0);
        botSpeechTextRT.anchorMax = new Vector2(1, 1);
        botSpeechTextRT.offsetMin = new Vector2(115, 6);
        botSpeechTextRT.offsetMax = new Vector2(-15, -6);
        TextMeshProUGUI botDialogueTMP = botSpeechTextObj.AddComponent<TextMeshProUGUI>();
        if (fontAsset != null) botDialogueTMP.font = fontAsset;
        botDialogueTMP.text = "LOAD-E: \"Count the boxes! Which number appears the most times in the supply load?\"";
        botDialogueTMP.fontSize = 20; // INCREASED TO 20!
        botDialogueTMP.fontStyle = FontStyles.Bold;
        botDialogueTMP.color = new Color(0.7f, 0.92f, 1f, 1f);
        botDialogueTMP.alignment = TextAlignmentOptions.MidlineLeft;
        botDialogueTMP.enableWordWrapping = true;
        botDialogueTMP.raycastTarget = false;

        // Holographic Feedback Panel (Shown upon answering)
        GameObject feedbackPanelObj = CreateUIElement("FeedbackPanel", telemetryConsole.transform);
        RectTransform fbRT = feedbackPanelObj.GetComponent<RectTransform>();
        fbRT.anchorMin = new Vector2(0.02f, 0.03f);
        fbRT.anchorMax = new Vector2(0.98f, 0.92f);
        fbRT.anchoredPosition = Vector2.zero;
        fbRT.sizeDelta = Vector2.zero;
        ProceduralImage fbBg = CreateProceduralCard(feedbackPanelObj, new Color(0.04f, 0.28f, 0.15f, 0.98f), 18f);
        fbBg.raycastTarget = false;

        GameObject fbStatusObj = CreateUIElement("StatusTitle", feedbackPanelObj.transform);
        RectTransform fbStatusRT = fbStatusObj.GetComponent<RectTransform>();
        fbStatusRT.anchorMin = new Vector2(0.04f, 1f);
        fbStatusRT.anchorMax = new Vector2(0.96f, 1f);
        fbStatusRT.pivot = new Vector2(0.5f, 1f);
        fbStatusRT.anchoredPosition = new Vector2(0, -22);
        fbStatusRT.sizeDelta = new Vector2(0, 48);
        TextMeshProUGUI fbStatusTMP = fbStatusObj.AddComponent<TextMeshProUGUI>();
        if (fontAsset != null) fbStatusTMP.font = fontAsset;
        fbStatusTMP.text = "🎉 AWESOME! SECTOR BALANCED!";
        fbStatusTMP.fontSize = 28;
        fbStatusTMP.fontStyle = FontStyles.Bold;
        fbStatusTMP.color = new Color(0.1f, 1f, 0.55f, 1f);
        fbStatusTMP.alignment = TextAlignmentOptions.MidlineLeft;
        fbStatusTMP.raycastTarget = false;

        GameObject fbVoObj = CreateUIElement("VoiceoverText", feedbackPanelObj.transform);
        RectTransform fbVoRT = fbVoObj.GetComponent<RectTransform>();
        fbVoRT.anchorMin = new Vector2(0.04f, 0.32f);
        fbVoRT.anchorMax = new Vector2(0.96f, 0.85f);
        fbVoRT.anchoredPosition = Vector2.zero;
        fbVoRT.sizeDelta = Vector2.zero;
        TextMeshProUGUI fbVoTMP = fbVoObj.AddComponent<TextMeshProUGUI>();
        if (fontAsset != null) fbVoTMP.font = fontAsset;
        fbVoTMP.text = "\"Excellent. That box number appears most often.\"";
        fbVoTMP.fontSize = 24;
        fbVoTMP.color = new Color(0.92f, 0.97f, 1f, 1f);
        fbVoTMP.alignment = TextAlignmentOptions.TopLeft;
        fbVoTMP.enableWordWrapping = true;
        fbVoTMP.raycastTarget = false;

        GameObject nextBtnObj = CreateUIElement("NextStageButton", feedbackPanelObj.transform);
        RectTransform nextBtnRT = nextBtnObj.GetComponent<RectTransform>();
        nextBtnRT.anchorMin = new Vector2(1f, 0f);
        nextBtnRT.anchorMax = new Vector2(1f, 0f);
        nextBtnRT.pivot = new Vector2(1f, 0f);
        nextBtnRT.anchoredPosition = new Vector2(-25, 20);
        nextBtnRT.sizeDelta = new Vector2(300, 60);
        ProceduralImage nextBtnPImg = CreateProceduralCard(nextBtnObj, new Color(0f, 0.88f, 0.52f, 1f), 16f);
        nextBtnPImg.raycastTarget = true;
        Button nextBtn = nextBtnObj.AddComponent<Button>();
        UIHoverClickEffect nextBtnHover = nextBtnObj.AddComponent<UIHoverClickEffect>();
        nextBtnHover.enableIdlePulse = false;

        GameObject nextBtnText = CreateUIElement("Text", nextBtnObj.transform);
        StretchFull(nextBtnText.GetComponent<RectTransform>());
        TextMeshProUGUI nextBtnTMP = nextBtnText.AddComponent<TextMeshProUGUI>();
        if (fontAsset != null) nextBtnTMP.font = fontAsset;
        nextBtnTMP.text = "NEXT SECTOR ►";
        nextBtnTMP.fontSize = 20;
        nextBtnTMP.fontStyle = FontStyles.Bold;
        nextBtnTMP.color = new Color(0.02f, 0.08f, 0.14f, 1f);
        nextBtnTMP.alignment = TextAlignmentOptions.Center;
        nextBtnTMP.raycastTarget = false;

        feedbackPanelObj.SetActive(false);

        // ---------------------------------------------------------------------
        // LOWER MECHANICAL SLIDING CONVEYOR TRAY (BOTTOM 40%)
        // ---------------------------------------------------------------------
        GameObject bottomConveyorArea = CreateUIElement("BottomConveyorArea", screenActivity.transform);
        RectTransform conveyorRT = bottomConveyorArea.GetComponent<RectTransform>();
        conveyorRT.anchorMin = new Vector2(0.5f, 0f);
        conveyorRT.anchorMax = new Vector2(0.5f, 0f);
        conveyorRT.pivot = new Vector2(0.5f, 0f);
        conveyorRT.anchoredPosition = new Vector2(0, 15);
        conveyorRT.sizeDelta = new Vector2(1840, 395);

        GameObject conveyorBgObj = CreateUIElement("ConveyorBackground", bottomConveyorArea.transform);
        StretchFull(conveyorBgObj.GetComponent<RectTransform>());
        Image conveyorBgImg = conveyorBgObj.AddComponent<Image>();
        conveyorBgImg.sprite = conveyorTraySprite != null ? conveyorTraySprite : bgSprite;
        conveyorBgImg.color = new Color(0.95f, 0.95f, 1f, 1f);
        conveyorBgImg.raycastTarget = false;

        GameObject conveyorOverlay = CreateUIElement("ConveyorOverlay", bottomConveyorArea.transform);
        StretchFull(conveyorOverlay.GetComponent<RectTransform>());
        Image conveyorOverlayImg = conveyorOverlay.AddComponent<Image>();
        conveyorOverlayImg.color = new Color(0.03f, 0.06f, 0.12f, 0.45f);
        conveyorOverlayImg.raycastTarget = false;

        GameObject trackBannerObj = CreateUIElement("TrackBanner", bottomConveyorArea.transform);
        RectTransform trackBannerRT = trackBannerObj.GetComponent<RectTransform>();
        trackBannerRT.anchorMin = new Vector2(0.5f, 1f);
        trackBannerRT.anchorMax = new Vector2(0.5f, 1f);
        trackBannerRT.pivot = new Vector2(0.5f, 1f);
        trackBannerRT.anchoredPosition = new Vector2(0, -10);
        trackBannerRT.sizeDelta = new Vector2(1800, 38);
        CreateProceduralCard(trackBannerObj, new Color(0.04f, 0.08f, 0.16f, 0.92f), 10f);

        GameObject trackTextObj = CreateUIElement("Text", trackBannerObj.transform);
        StretchFull(trackTextObj.GetComponent<RectTransform>());
        TextMeshProUGUI trackTMP = trackTextObj.AddComponent<TextMeshProUGUI>();
        if (fontAsset != null) trackTMP.font = fontAsset;
        trackTMP.text = "<color=#FFB800>CALIBRATION POD CONVEYOR</color>  //  <color=#00F0FF>SELECT MATCHING CONTAINER POD</color>";
        trackTMP.fontSize = 17;
        trackTMP.fontStyle = FontStyles.Bold;
        trackTMP.alignment = TextAlignmentOptions.Center;
        trackTMP.raycastTarget = false;

        GameObject optionsTray = CreateUIElement("OptionsTray", bottomConveyorArea.transform);
        RectTransform optionsTrayRT = optionsTray.GetComponent<RectTransform>();
        optionsTrayRT.anchorMin = new Vector2(0.5f, 0.5f);
        optionsTrayRT.anchorMax = new Vector2(0.5f, 0.5f);
        optionsTrayRT.pivot = new Vector2(0.5f, 0.5f);
        optionsTrayRT.anchoredPosition = new Vector2(0, -15);
        optionsTrayRT.sizeDelta = new Vector2(1780, 255);

        HorizontalLayoutGroup optLayout = optionsTray.AddComponent<HorizontalLayoutGroup>();
        optLayout.spacing = 32;
        optLayout.childAlignment = TextAnchor.MiddleCenter;
        optLayout.childControlWidth = true;   // LayoutElement controls pod widths dynamically!
        optLayout.childControlHeight = true;
        optLayout.childForceExpandWidth = false;
        optLayout.childForceExpandHeight = false;

        Button[] optionButtons = new Button[5];
        TextMeshProUGUI[] optionTexts = new TextMeshProUGUI[5];
        TextMeshProUGUI[] optionSubtitles = new TextMeshProUGUI[5];
        Image[] optionBgs = new Image[5];

        for (int i = 0; i < 5; i++)
        {
            GameObject optBtnObj = CreateUIElement($"CratePod_{i}", optionsTray.transform);
            LayoutElement podLE = optBtnObj.AddComponent<LayoutElement>();
            podLE.preferredWidth = 420;
            podLE.minWidth = 280;
            podLE.preferredHeight = 230;
            podLE.minHeight = 220;

            ProceduralImage optBg = CreateProceduralCard(optBtnObj, new Color(0.07f, 0.13f, 0.26f, 0.98f), 18f);
            optBg.raycastTarget = true;
            Button optBtn = optBtnObj.AddComponent<Button>();
            UIHoverClickEffect optHover = optBtnObj.AddComponent<UIHoverClickEffect>();
            optHover.hoverScale = 1.05f;
            optHover.clickScale = 0.95f;
            optHover.hoverDuration = 0.1f;
            optHover.enableIdlePulse = false;

            optionButtons[i] = optBtn;
            optionBgs[i] = optBg;

            // Top Header Tag
            GameObject headerPill = CreateUIElement("HeaderPill", optBtnObj.transform);
            RectTransform headerPillRT = headerPill.GetComponent<RectTransform>();
            headerPillRT.anchorMin = new Vector2(0.5f, 1f);
            headerPillRT.anchorMax = new Vector2(0.5f, 1f);
            headerPillRT.pivot = new Vector2(0.5f, 1f);
            headerPillRT.anchoredPosition = new Vector2(0, -12);
            headerPillRT.sizeDelta = new Vector2(240, 28);
            CreateProceduralCard(headerPill, new Color(0f, 0.9f, 1f, 0.16f), 8f);

            GameObject headerTextObj = CreateUIElement("Text", headerPill.transform);
            StretchFull(headerTextObj.GetComponent<RectTransform>());
            TextMeshProUGUI headerTMP = headerTextObj.AddComponent<TextMeshProUGUI>();
            if (fontAsset != null) headerTMP.font = fontAsset;
            headerTMP.text = $"[ CARGO POD {i + 1:D2} ]";
            headerTMP.fontSize = 13;
            headerTMP.fontStyle = FontStyles.Bold;
            headerTMP.color = new Color(0f, 0.95f, 1f, 1f);
            headerTMP.alignment = TextAlignmentOptions.Center;
            headerTMP.raycastTarget = false;

            // Center Big Number Value (INCREASED FROM 44 to 56!)
            GameObject optTextObj = CreateUIElement("ValueText", optBtnObj.transform);
            RectTransform optValRT = optTextObj.GetComponent<RectTransform>();
            optValRT.anchorMin = new Vector2(0.5f, 0.5f);
            optValRT.anchorMax = new Vector2(0.5f, 0.5f);
            optValRT.pivot = new Vector2(0.5f, 0.5f);
            optValRT.anchoredPosition = new Vector2(0, 10);
            optValRT.sizeDelta = new Vector2(360, 80);
            TextMeshProUGUI optTMP = optTextObj.AddComponent<TextMeshProUGUI>();
            if (fontAsset != null) optTMP.font = fontAsset;
            optTMP.text = "14";
            optTMP.fontSize = 56; // HUGE AND PUNCHY!
            optTMP.enableAutoSizing = true;
            optTMP.fontSizeMin = 24;
            optTMP.fontSizeMax = 56;
            optTMP.enableWordWrapping = true;
            optTMP.fontStyle = FontStyles.Bold;
            optTMP.color = Color.white;
            optTMP.alignment = TextAlignmentOptions.Center;
            optTMP.raycastTarget = false;
            optionTexts[i] = optTMP;

            // Bottom Subtitle Pill
            GameObject subPill = CreateUIElement("SubPill", optBtnObj.transform);
            RectTransform subPillRT = subPill.GetComponent<RectTransform>();
            subPillRT.anchorMin = new Vector2(0.5f, 0f);
            subPillRT.anchorMax = new Vector2(0.5f, 0f);
            subPillRT.pivot = new Vector2(0.5f, 0f);
            subPillRT.anchoredPosition = new Vector2(0, 15);
            subPillRT.sizeDelta = new Vector2(360, 40);
            CreateProceduralCard(subPill, new Color(0.03f, 0.07f, 0.15f, 0.9f), 10f);

            GameObject optSubObj = CreateUIElement("Text", subPill.transform);
            StretchFull(optSubObj.GetComponent<RectTransform>());
            TextMeshProUGUI optSubTMP = optSubObj.AddComponent<TextMeshProUGUI>();
            if (fontAsset != null) optSubTMP.font = fontAsset;
            optSubTMP.text = "Count: 3 Boxes (MODE)";
            optSubTMP.fontSize = 16;
            optSubTMP.enableAutoSizing = true;
            optSubTMP.fontSizeMin = 12;
            optSubTMP.fontSizeMax = 17;
            optSubTMP.enableWordWrapping = false;
            optSubTMP.fontStyle = FontStyles.Bold;
            optSubTMP.color = new Color(1f, 0.85f, 0.25f, 1f);
            optSubTMP.alignment = TextAlignmentOptions.Center;
            optSubTMP.raycastTarget = false;
            optionSubtitles[i] = optSubTMP;
        }

        // Floating Bonus Points Popup Text
        GameObject floatBonusObj = CreateUIElement("FloatingBonusTMP", screenActivity.transform);
        RectTransform floatBonusRT = floatBonusObj.GetComponent<RectTransform>();
        floatBonusRT.sizeDelta = new Vector2(500, 80);
        TextMeshProUGUI floatingBonusTMP = floatBonusObj.AddComponent<TextMeshProUGUI>();
        if (fontAsset != null) floatingBonusTMP.font = fontAsset;
        floatingBonusTMP.text = "+500 PTS!";
        floatingBonusTMP.fontSize = 44;
        floatingBonusTMP.fontStyle = FontStyles.Bold;
        floatingBonusTMP.color = new Color(1f, 0.85f, 0.1f, 1f);
        floatingBonusTMP.alignment = TextAlignmentOptions.Center;
        floatingBonusTMP.raycastTarget = false;
        floatBonusObj.SetActive(false);

        // =====================================================================
        // SCREEN 3: VICTORY SCREEN
        // =====================================================================
        GameObject screenVictory = CreateUIElement("Screen_Victory", canvasObj.transform);
        StretchFull(screenVictory.GetComponent<RectTransform>());
        screenVictory.SetActive(false);

        GameObject vicBgObj = CreateUIElement("VictoryBg", screenVictory.transform);
        StretchFull(vicBgObj.GetComponent<RectTransform>());
        Image vicBgImg = vicBgObj.AddComponent<Image>();
        vicBgImg.sprite = victorySprite;
        vicBgImg.color = new Color(0.95f, 0.95f, 1f, 1f);
        vicBgImg.raycastTarget = false;

        GameObject vicOverlay = CreateUIElement("VictoryDarkOverlay", screenVictory.transform);
        StretchFull(vicOverlay.GetComponent<RectTransform>());
        Image vicOverlayImg = vicOverlay.AddComponent<Image>();
        vicOverlayImg.color = new Color(0.02f, 0.04f, 0.09f, 0.65f);
        vicOverlayImg.raycastTarget = false;

        GameObject vicCard = CreateUIElement("VictoryCard", screenVictory.transform);
        RectTransform vicCardRT = vicCard.GetComponent<RectTransform>();
        vicCardRT.anchorMin = new Vector2(0.5f, 0.5f);
        vicCardRT.anchorMax = new Vector2(0.5f, 0.5f);
        vicCardRT.pivot = new Vector2(0.5f, 0.5f);
        vicCardRT.sizeDelta = new Vector2(1200, 740);
        vicCardRT.anchoredPosition = Vector2.zero;
        CreateProceduralCard(vicCard, new Color(0.04f, 0.08f, 0.18f, 0.98f), 24f);

        GameObject vicHeadObj = CreateUIElement("Headline", vicCard.transform);
        RectTransform vicHeadRT = vicHeadObj.GetComponent<RectTransform>();
        vicHeadRT.anchorMin = new Vector2(0.5f, 1f);
        vicHeadRT.anchorMax = new Vector2(0.5f, 1f);
        vicHeadRT.pivot = new Vector2(0.5f, 1f);
        vicHeadRT.anchoredPosition = new Vector2(0, -55);
        vicHeadRT.sizeDelta = new Vector2(1100, 60);
        TextMeshProUGUI vicHeadTMP = vicHeadObj.AddComponent<TextMeshProUGUI>();
        if (fontAsset != null) vicHeadTMP.font = fontAsset;
        vicHeadTMP.text = "SHIP BALANCED AND READY!";
        vicHeadTMP.fontSize = 44;
        vicHeadTMP.fontStyle = FontStyles.Bold;
        vicHeadTMP.color = new Color(0f, 0.95f, 1f, 1f);
        vicHeadTMP.alignment = TextAlignmentOptions.Center;
        vicHeadTMP.raycastTarget = false;

        GameObject vicSubObj = CreateUIElement("Subtitle", vicCard.transform);
        RectTransform vicSubRT = vicSubObj.GetComponent<RectTransform>();
        vicSubRT.anchorMin = new Vector2(0.5f, 1f);
        vicSubRT.anchorMax = new Vector2(0.5f, 1f);
        vicSubRT.pivot = new Vector2(0.5f, 1f);
        vicSubRT.anchoredPosition = new Vector2(0, -125);
        vicSubRT.sizeDelta = new Vector2(1100, 45);
        TextMeshProUGUI vicSubTMP = vicSubObj.AddComponent<TextMeshProUGUI>();
        if (fontAsset != null) vicSubTMP.font = fontAsset;
        vicSubTMP.text = "ALL CARGO SECTORS FULLY CALIBRATED & BALANCED";
        vicSubTMP.fontSize = 23;
        vicSubTMP.fontStyle = FontStyles.Bold;
        vicSubTMP.color = new Color(1f, 0.85f, 0.25f, 1f);
        vicSubTMP.alignment = TextAlignmentOptions.Center;
        vicSubTMP.raycastTarget = false;

        // Final Score Pill on Victory Screen
        GameObject vicScorePill = CreateUIElement("VictoryScorePill", vicCard.transform);
        RectTransform vicScoreRT = vicScorePill.GetComponent<RectTransform>();
        vicScoreRT.anchorMin = new Vector2(0.5f, 1f);
        vicScoreRT.anchorMax = new Vector2(0.5f, 1f);
        vicScoreRT.pivot = new Vector2(0.5f, 1f);
        vicScoreRT.anchoredPosition = new Vector2(0, -195);
        vicScoreRT.sizeDelta = new Vector2(600, 65);
        CreateProceduralCard(vicScorePill, new Color(0.02f, 0.05f, 0.12f, 0.95f), 16f);

        GameObject vicScoreTextObj = CreateUIElement("Text", vicScorePill.transform);
        StretchFull(vicScoreTextObj.GetComponent<RectTransform>());
        TextMeshProUGUI victoryScoreTMP = vicScoreTextObj.AddComponent<TextMeshProUGUI>();
        if (fontAsset != null) victoryScoreTMP.font = fontAsset;
        victoryScoreTMP.text = "MISSION SCORE: 2,500 PTS";
        victoryScoreTMP.fontSize = 30;
        victoryScoreTMP.fontStyle = FontStyles.Bold;
        victoryScoreTMP.color = new Color(1f, 0.84f, 0f, 1f);
        victoryScoreTMP.alignment = TextAlignmentOptions.Center;
        victoryScoreTMP.raycastTarget = false;

        GameObject vicBodyObj = CreateUIElement("BodyText", vicCard.transform);
        RectTransform vicBodyRT = vicBodyObj.GetComponent<RectTransform>();
        vicBodyRT.anchorMin = new Vector2(0.5f, 1f);
        vicBodyRT.anchorMax = new Vector2(0.5f, 1f);
        vicBodyRT.pivot = new Vector2(0.5f, 1f);
        vicBodyRT.anchoredPosition = new Vector2(0, -280);
        vicBodyRT.sizeDelta = new Vector2(1050, 150);
        TextMeshProUGUI vicBodyTMP = vicBodyObj.AddComponent<TextMeshProUGUI>();
        if (fontAsset != null) vicBodyTMP.font = fontAsset;
        vicBodyTMP.text = "\"Incredible work! You organized the cargo, leveled the power, and cleared the ship for takeoff.\"\n\nMode, Median, Mean, Range, and Weather Stability are all verified. The ship is cleared for warp launch!";
        vicBodyTMP.fontSize = 22;
        vicBodyTMP.color = new Color(0.9f, 0.95f, 1f, 1f);
        vicBodyTMP.alignment = TextAlignmentOptions.Center;
        vicBodyTMP.enableWordWrapping = true;
        vicBodyTMP.raycastTarget = false;

        GameObject playAgainBtnObj = CreateUIElement("PlayAgainButton", vicCard.transform);
        RectTransform playAgainRT = playAgainBtnObj.GetComponent<RectTransform>();
        playAgainRT.anchorMin = new Vector2(0.5f, 0f);
        playAgainRT.anchorMax = new Vector2(0.5f, 0f);
        playAgainRT.pivot = new Vector2(0.5f, 0f);
        playAgainRT.anchoredPosition = new Vector2(0, 45);
        playAgainRT.sizeDelta = new Vector2(440, 75);
        ProceduralImage playAgainPImg = CreateProceduralCard(playAgainBtnObj, new Color(0f, 0.88f, 0.52f, 1f), 20f);
        playAgainPImg.raycastTarget = true;
        Button playAgainBtn = playAgainBtnObj.AddComponent<Button>();
        UIHoverClickEffect playAgainHover = playAgainBtnObj.AddComponent<UIHoverClickEffect>();
        playAgainHover.enableIdlePulse = false;

        GameObject playAgainTextObj = CreateUIElement("Text", playAgainBtnObj.transform);
        StretchFull(playAgainTextObj.GetComponent<RectTransform>());
        TextMeshProUGUI playAgainTMP = playAgainTextObj.AddComponent<TextMeshProUGUI>();
        if (fontAsset != null) playAgainTMP.font = fontAsset;
        playAgainTMP.text = "REPLAY CALIBRATION (RESTART)";
        playAgainTMP.fontSize = 22;
        playAgainTMP.fontStyle = FontStyles.Bold;
        playAgainTMP.color = new Color(0.02f, 0.07f, 0.14f, 1f);
        playAgainTMP.alignment = TextAlignmentOptions.Center;
        playAgainTMP.raycastTarget = false;

        // =====================================================================
        // WIRE UP GAME MANAGER WITH DIRECT LISTENERS
        // =====================================================================
        GameObject gmObj = new GameObject("CargoGridGameManager");
        CargoGridGameManager gm = gmObj.AddComponent<CargoGridGameManager>();

        gm.screenInstructions = screenInstructions;
        gm.screenActivity = screenActivity;
        gm.screenVictory = screenVictory;
        gm.titleLogoTransform = titleLogoRT;
        gm.instructionCard = speechCardRT;
        gm.startCalibrationButton = startBtn;
        gm.botCharacterTransform = botRT;
        gm.botGreetingTMP = greetTMP;
        gm.botInstructionTMP = listTMP;
        speechCardBtn.onClick.AddListener(() => gm.SkipTypewriterInstructions());
        gm.topTitleTMP = topTitleTMP;
        gm.stageIndicatorTMP = null;
        gm.stageDots = stageDots;
        gm.stageDotLabels = stageDotLabels;
        gm.scoreTMP = scoreTMP;
        gm.streakTMP = streakTMP;
        gm.floatingBonusTMP = floatingBonusTMP;
        gm.victoryScoreTMP = victoryScoreTMP;
        gm.energyBarFill = energyFillImg;
        gm.energyPercentTMP = energyLabelTMP;
        gm.muteButton = muteBtn;
        gm.visualDisplayImage = visualDisplayImg;
        gm.stageTitleTMP = stageTitleTMP;
        gm.stageSubtitleTMP = stageSubTMP;
        gm.screenPromptTMP = screenPromptTMP;
        gm.taskPromptTMP = taskPromptTMP;
        gm.botDialogueTMP = botDialogueTMP;
        gm.optionButtons = optionButtons;
        gm.optionTexts = optionTexts;
        gm.optionSubtitles = optionSubtitles;
        gm.optionBgImages = optionBgs;
        gm.feedbackPanel = feedbackPanelObj;
        gm.feedbackStatusTMP = fbStatusTMP;
        gm.feedbackVoiceoverTMP = fbVoTMP;
        gm.feedbackStatusBg = fbBg;
        gm.nextStageButton = nextBtn;
        gm.victoryCard = vicCardRT;
        gm.playAgainButton = playAgainBtn;
        gm.screenFlashOverlay = flashImg;
        gm.bayLightingGlow = bayGlowImg;

        // DIRECT BULLETPROOF BUTTON WIRING
        startBtn.onClick.RemoveAllListeners();
        startBtn.onClick.AddListener(() => {
            Debug.Log("[CargoGrid] Start Button Fired!");
            gm.OnStartCalibrationClicked();
        });

        nextBtn.onClick.RemoveAllListeners();
        nextBtn.onClick.AddListener(() => {
            gm.OnNextStageClicked();
        });

        playAgainBtn.onClick.RemoveAllListeners();
        playAgainBtn.onClick.AddListener(() => {
            gm.OnPlayAgainClicked();
        });

        // Activity Items List
        List<CargoGridGameManager.ActivityItem> items = new List<CargoGridGameManager.ActivityItem>
        {
            new CargoGridGameManager.ActivityItem
            {
                stageTag = "[ SECTOR 01 ]",
                stageTitle = "The Popular Package",
                conceptName = "Mode • Most Frequent Number",
                screenPrompt = "Incoming supply load:\n<color=#2AFFA2>14 → 22 → 14 → 19 → 14 → 35 → 22</color>",
                taskPrompt = ">>> TASK: Tap the item number that appears most often.",
                botCheerPrompt = "\"Count the boxes! Which number appears the most times in the load?\"",
                options = new string[] { "14", "22", "19" },
                optionSubtitles = new string[] { "Count: 3 Boxes! (MODE)", "Count: 2 Boxes", "Count: 1 Box" },
                correctOptionIndex = 0,
                voiceoverCorrect = "Excellent! That box number appears most often.",
                voiceoverWrong = "Count again! Which specific number shows up the most?",
                visualSprite = crateSprite
            },
            new CargoGridGameManager.ActivityItem
            {
                stageTag = "[ SECTOR 02 ]",
                stageTitle = "The Center Conveyor",
                conceptName = "Median • Exact Center Value",
                screenPrompt = "Line up the cargo sizes: 41, 18, 33, 50, 25\n<color=#2AFFA2>Ordered Track: 18 → 25 → [ 33 ] → 41 → 50</color>",
                taskPrompt = ">>> TASK: Put weights in order and select the exact center.",
                botCheerPrompt = "\"We lined them up from smallest to largest! Pick the one in the middle!\"",
                options = new string[] { "18", "25", "33 (Center)", "41", "50" },
                optionSubtitles = new string[] { "Smallest", "2nd", "MEDIAN (Center)", "4th", "Largest" },
                correctOptionIndex = 2,
                voiceoverCorrect = "Perfect! You found the exact middle cargo item.",
                voiceoverWrong = "Sort from smallest to largest first, then pick the container right in the center.",
                visualSprite = conveyorTraySprite != null ? conveyorTraySprite : crateSprite
            },
            new CargoGridGameManager.ActivityItem
            {
                stageTag = "[ SECTOR 03 ]",
                stageTitle = "The Fair Share Batteries",
                conceptName = "Mean • Equal Power Balance",
                screenPrompt = "Four backup generators have: 3, 7, 2, and 4 power rods.\n<color=#2AFFA2>Total Energy: 16 Rods across 4 Generators (16 ÷ 4 = ?)</color>",
                taskPrompt = ">>> TASK: Distribute rods equally. What is the fair share per generator?",
                botCheerPrompt = "\"Total of 16 rods divided across 4 generators! What's the equal share?\"",
                options = new string[] { "4 Rods Each", "3 Rods Each", "5 Rods Each" },
                optionSubtitles = new string[] { "Balanced (Mean: 4)", "Too Low (12 Rods)", "Too High (20 Rods)" },
                correctOptionIndex = 0,
                voiceoverCorrect = "Great job! Every single generator now has an equal share.",
                voiceoverWrong = "Keep moving the rods around until all piles are the same height.",
                visualSprite = generatorSprite
            },
            new CargoGridGameManager.ActivityItem
            {
                stageTag = "[ SECTOR 04 ]",
                stageTitle = "The Distance Gap",
                conceptName = "Range • Telemetry Spread",
                screenPrompt = "Sensor distances: 12 km, 45 km, 28 km, 60 km.\n<color=#2AFFA2>Closest Scan: 12 km  |  Furthest Scan: 60 km</color>",
                taskPrompt = ">>> TASK: Find total gap between closest and furthest: 60 - 12 = ?",
                botCheerPrompt = "\"Subtract the smallest number from the biggest number to find the range!\"",
                options = new string[] { "48 km Spread", "33 km Spread", "16 km Spread" },
                optionSubtitles = new string[] { "60 - 12 = 48 (RANGE)", "Calculation Error", "Distance Error" },
                correctOptionIndex = 0,
                voiceoverCorrect = "Spot on! The total gap between the two limits is forty-eight.",
                voiceoverWrong = "Find the absolute smallest and largest numbers to see the total spread.",
                visualSprite = sensorSprite
            },
            new CargoGridGameManager.ActivityItem
            {
                stageTag = "[ SECTOR 05 ]",
                stageTitle = "The Safe Landing",
                conceptName = "Consistency • Data Group Stability",
                screenPrompt = "Zone A winds: 15, 16, 15, 17, 15 (Narrow spread: 2 km/h)\n<color=#FFB800>Zone B winds: 5, 25, 12, 30, 8 (Wild spread: 25 km/h)</color>",
                taskPrompt = ">>> TASK: Select the landing zone with the most steady weather pattern.",
                botCheerPrompt = "\"Look at the numbers! Zone A stays between 15-17. Zone B is all over the place!\"",
                options = new string[] { "ZONE A (Steady)", "ZONE B (Turbulent)" },
                optionSubtitles = new string[] { "Safe & Predictable", "Wild Fluctuations" },
                correctOptionIndex = 0,
                voiceoverCorrect = "Smart choice! Zone A has very steady, predictable numbers.",
                voiceoverWrong = "Look out! Zone B wind speeds jump around way too wildly.",
                visualSprite = landingSprite
            }
        };

        gm.activityItems = items;

        // Set up all button listeners on GameManager directly
        gm.SetupButtonListeners();

        Debug.Log("[CargoGrid] Grade 5 Character-Driven Sci-Fi Setup Complete!");
    }

    private static void SetField(object target, string fieldName, object value)
    {
        var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
        if (field != null)
        {
            field.SetValue(target, value);
        }
    }

    private static GameObject CreateUIElement(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    private static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;
    }
}
