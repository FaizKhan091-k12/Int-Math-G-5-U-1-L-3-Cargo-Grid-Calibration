using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.ProceduralImage;
using TMPro;
using DG.Tweening;

public class CargoGridGameManager : MonoBehaviour
{
    public static CargoGridGameManager instance;

    [System.Serializable]
    public class ActivityItem
    {
        public string stageTag;
        public string stageTitle;
        public string conceptName;
        [TextArea(2, 4)]
        public string screenPrompt;
        [TextArea(2, 4)]
        public string taskPrompt;
        public string botCheerPrompt;
        public string[] options;
        public string[] optionSubtitles;
        public int correctOptionIndex;
        [TextArea(2, 4)]
        public string voiceoverCorrect;
        [TextArea(2, 4)]
        public string voiceoverWrong;
        public Sprite visualSprite;
    }

    [Header("Screens")]
    public GameObject screenInstructions;
    public GameObject screenActivity;
    public GameObject screenVictory;

    [Header("Instruction Screen UI")]
    public Button startCalibrationButton;
    public RectTransform instructionCard;
    public RectTransform botCharacterTransform;
    public RectTransform titleLogoTransform;
    public TextMeshProUGUI botGreetingTMP;
    public TextMeshProUGUI botInstructionTMP;

    [Header("Typewriter & Intro Staging Settings")]
    [Tooltip("Delay in seconds per character typed during instructions. Lower is faster. Tweakable in Inspector.")]
    [Range(0.001f, 0.08f)]
    public float typingSpeed = 0.012f;

    [Tooltip("Delay in seconds per character typed during robot greeting.")]
    [Range(0.001f, 0.08f)]
    public float greetingTypingSpeed = 0.015f;

    private Coroutine typewriterCoroutine;
    private bool isTypewriterFinished = false;

    [Header("Activity Screen HUD")]
    public TextMeshProUGUI topTitleTMP;
    public TextMeshProUGUI stageIndicatorTMP;
    public Image[] stageDots;
    public TextMeshProUGUI[] stageDotLabels;
    public Image energyBarFill;
    public TextMeshProUGUI energyPercentTMP;
    public Button muteButton;

    [Header("Center Bay Viewscreen")]
    public Image visualDisplayImage;
    public Image botCompanionImage;
    public TextMeshProUGUI stageTitleTMP;
    public TextMeshProUGUI stageSubtitleTMP;
    public TextMeshProUGUI screenPromptTMP;
    public TextMeshProUGUI taskPromptTMP;
    public TextMeshProUGUI botDialogueTMP;

    [Header("Bottom Conveyor Sliding Tray")]
    public Button[] optionButtons;
    public TextMeshProUGUI[] optionTexts;
    public TextMeshProUGUI[] optionSubtitles;
    public Image[] optionBgImages;

    [Header("Hologram Communicator Feedback")]
    public GameObject feedbackPanel;
    public TextMeshProUGUI feedbackStatusTMP;
    public TextMeshProUGUI feedbackVoiceoverTMP;
    public Image feedbackStatusBg;
    public Button nextStageButton;

    [Header("Victory Screen UI")]
    public RectTransform victoryCard;
    public Button playAgainButton;

    [Header("Juice & Lighting FX")]
    public Image screenFlashOverlay;
    public Image bayLightingGlow;

    [Header("Activity Data")]
    public List<ActivityItem> activityItems = new List<ActivityItem>();
    private int currentItemIndex = 0;
    private bool isAnsweringLocked = false;

    [Header("Scoring System")]
    public int currentScore = 0;
    public int currentStreak = 0;
    public TextMeshProUGUI scoreTMP;
    public TextMeshProUGUI streakTMP;
    public TextMeshProUGUI floatingBonusTMP;
    public TextMeshProUGUI victoryScoreTMP;

    // Vibrant Kid-Friendly Color Palette
    private readonly Color colorNeonCyan = new Color(0f, 0.95f, 1f, 1f);
    private readonly Color colorNeonGreen = new Color(0.15f, 1f, 0.45f, 1f);
    private readonly Color colorNeonAmber = new Color(1f, 0.72f, 0.1f, 1f);
    private readonly Color colorCorrectBg = new Color(0.05f, 0.32f, 0.16f, 0.98f);
    private readonly Color colorWrongBg = new Color(0.42f, 0.12f, 0.08f, 0.98f);
    private readonly Color colorDefaultCrateBg = new Color(0.08f, 0.14f, 0.26f, 0.96f);

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(instance.gameObject);
        }
        instance = this;
        EnsureActivityItems();
    }

    private void Start()
    {
        EnsureActivityItems();
        SetupButtonListeners();
        ShowScreenInstructions();
    }

    public void EnsureActivityItems()
    {
        if (activityItems != null && activityItems.Count > 0) return;

        Sprite crateSprite = Resources.Load<Sprite>("CargoGrid/Sprites/crate_item");
        Sprite conveyorTraySprite = Resources.Load<Sprite>("CargoGrid/Sprites/conveyor_tray_bg");
        Sprite generatorSprite = Resources.Load<Sprite>("CargoGrid/Sprites/generator_item");
        Sprite sensorSprite = Resources.Load<Sprite>("CargoGrid/Sprites/sensor_scanner");
        Sprite landingSprite = Resources.Load<Sprite>("CargoGrid/Sprites/landing_pads");

        activityItems = new List<ActivityItem>
        {
            new ActivityItem
            {
                stageTag = "[ SECTOR 01 ]",
                stageTitle = "The Popular Package",
                conceptName = "Mode • Most Frequent Number",
                screenPrompt = "Incoming supply load:\n<color=#2AFFA2>14 -> 22 -> 14 -> 19 -> 14 -> 35 -> 22</color>",
                taskPrompt = ">>> TASK: Tap the cargo container number that appears most often.",
                botCheerPrompt = "\"Count the boxes! Which number appears the most times in the supply load?\"",
                options = new string[] { "14", "22", "19" },
                optionSubtitles = new string[] { "Count: 3 Boxes (MODE)", "Count: 2 Boxes", "Count: 1 Box" },
                correctOptionIndex = 0,
                voiceoverCorrect = "Excellent! Number 14 is the mode because it appears 3 times.",
                voiceoverWrong = "Count again! Which container number shows up the most?",
                visualSprite = crateSprite
            },
            new ActivityItem
            {
                stageTag = "[ SECTOR 02 ]",
                stageTitle = "The Center Conveyor",
                conceptName = "Median • Exact Center Value",
                screenPrompt = "Line up the cargo sizes: 41, 18, 33, 50, 25\n<color=#2AFFA2>Ordered Track: 18 -> 25 -> [ 33 ] -> 41 -> 50</color>",
                taskPrompt = ">>> TASK: Put weights in order from least to greatest and select the exact center.",
                botCheerPrompt = "\"We lined them up from least to greatest! Pick the container right in the center!\"",
                options = new string[] { "18", "25", "33 (Center)", "41", "50" },
                optionSubtitles = new string[] { "Smallest", "2nd", "MEDIAN (Center)", "4th", "Largest" },
                correctOptionIndex = 2,
                voiceoverCorrect = "Perfect! 33 is the median because it is exactly in the middle.",
                voiceoverWrong = "Sort from smallest to largest first, then pick the container right in the center.",
                visualSprite = conveyorTraySprite != null ? conveyorTraySprite : crateSprite
            },
            new ActivityItem
            {
                stageTag = "[ SECTOR 03 ]",
                stageTitle = "The Fair Share Batteries",
                conceptName = "Mean • Equal Power Balance",
                screenPrompt = "Four backup generators have: 3, 7, 2, and 4 power rods.\n<color=#2AFFA2>Total Energy: 16 Rods across 4 Generators (16 / 4 = ?)</color>",
                taskPrompt = ">>> TASK: Distribute rods equally. What is the fair share (mean) per generator?",
                botCheerPrompt = "\"Total of 16 rods divided across 4 generators! What's the equal share?\"",
                options = new string[] { "4 Rods Each", "3 Rods Each", "5 Rods Each" },
                optionSubtitles = new string[] { "Balanced (Mean: 4)", "Too Low (12 Rods)", "Too High (20 Rods)" },
                correctOptionIndex = 0,
                voiceoverCorrect = "Great job! Every single generator now has an equal share of 4 rods.",
                voiceoverWrong = "Add all rods together (16) and divide equally among the 4 generators.",
                visualSprite = generatorSprite
            },
            new ActivityItem
            {
                stageTag = "[ SECTOR 04 ]",
                stageTitle = "The Distance Gap",
                conceptName = "Range • Telemetry Spread",
                screenPrompt = "Sensor distances: 12 km, 45 km, 28 km, 60 km.\n<color=#2AFFA2>Closest: 12 km  |  Furthest: 60 km  (60 - 12 = ?)</color>",
                taskPrompt = ">>> TASK: Find total gap (range) between closest and furthest: 60 - 12 = ?",
                botCheerPrompt = "\"Subtract the smallest number from the biggest number to find the range!\"",
                options = new string[] { "48 km Spread", "33 km Spread", "16 km Spread" },
                optionSubtitles = new string[] { "60 - 12 = 48 (RANGE)", "Calculation Error", "Distance Error" },
                correctOptionIndex = 0,
                voiceoverCorrect = "Spot on! The range between furthest and closest is 48 km.",
                voiceoverWrong = "Subtract the minimum number (12) from the maximum number (60).",
                visualSprite = sensorSprite
            },
            new ActivityItem
            {
                stageTag = "[ SECTOR 05 ]",
                stageTitle = "The Safe Landing",
                conceptName = "Consistency • Data Group Stability",
                screenPrompt = "Zone A winds: 15, 16, 15, 17, 15 (Narrow spread: 2 km/h)\n<color=#FFB800>Zone B winds: 5, 25, 12, 30, 8 (Wild spread: 25 km/h)</color>",
                taskPrompt = ">>> TASK: Select the landing zone with the most steady weather pattern for takeoff.",
                botCheerPrompt = "\"Look at the numbers! Zone A stays between 15-17. Zone B is all over the place!\"",
                options = new string[] { "ZONE A (Steady)", "ZONE B (Turbulent)" },
                optionSubtitles = new string[] { "Safe & Predictable", "Wild Fluctuations" },
                correctOptionIndex = 0,
                voiceoverCorrect = "Smart choice! Zone A has very steady, predictable numbers. Clear for launch!",
                voiceoverWrong = "Look out! Zone B wind speeds jump around way too wildly.",
                visualSprite = landingSprite
            }
        };
    }

    public void SetupButtonListeners()
    {
        if (startCalibrationButton != null)
        {
            startCalibrationButton.onClick.RemoveAllListeners();
            startCalibrationButton.onClick.AddListener(OnStartCalibrationClicked);
        }

        if (nextStageButton != null)
        {
            nextStageButton.onClick.RemoveAllListeners();
            nextStageButton.onClick.AddListener(OnNextStageClicked);
        }

        if (playAgainButton != null)
        {
            playAgainButton.onClick.RemoveAllListeners();
            playAgainButton.onClick.AddListener(OnPlayAgainClicked);
        }

        if (muteButton != null)
        {
            muteButton.onClick.RemoveAllListeners();
            muteButton.onClick.AddListener(() =>
            {
                if (AudioManager.instance != null)
                {
                    AudioManager.instance.ToggleMute();
                    AudioManager.instance.PlayClick();
                }
            });
        }

        if (optionButtons != null)
        {
            for (int i = 0; i < optionButtons.Length; i++)
            {
                int index = i;
                optionButtons[i].onClick.RemoveAllListeners();
                optionButtons[i].onClick.AddListener(() => OnOptionSelected(index));
            }
        }
    }

    private void Update()
    {
        // Allow player to tap/click or press space to quickly skip the intro typing sequence
        if (screenInstructions != null && screenInstructions.activeSelf && !isTypewriterFinished)
        {
            if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
            {
                SkipTypewriterInstructions();
            }
        }
    }

    public void AutoDiscoverInstructionReferences()
    {
        if (screenInstructions == null) return;

        // Auto-discover Title Logo if null
        if (titleLogoTransform == null)
        {
            Transform t = screenInstructions.transform.Find("BigTitleLogo");
            if (t == null)
            {
                foreach (Transform child in screenInstructions.transform)
                {
                    if (child.name.ToLower().Contains("title") && !child.name.ToLower().Contains("glow"))
                    {
                        t = child;
                        break;
                    }
                }
            }
            if (t != null) titleLogoTransform = t.GetComponent<RectTransform>();
        }

        // Auto-discover Mascot Bot if null
        if (botCharacterTransform == null)
        {
            foreach (var rt in screenInstructions.GetComponentsInChildren<RectTransform>(true))
            {
                if (rt.gameObject.name == "MascotBot" || rt.gameObject.name.Contains("Bot"))
                {
                    botCharacterTransform = rt;
                    break;
                }
            }
        }

        // Auto-discover Speech Card / Dialogue Box if null
        if (instructionCard == null)
        {
            foreach (var rt in screenInstructions.GetComponentsInChildren<RectTransform>(true))
            {
                if (rt.gameObject.name == "MascotSpeechCard" || rt.gameObject.name.Contains("SpeechCard"))
                {
                    instructionCard = rt;
                    break;
                }
            }
        }

        // Auto-discover Start Button if null
        if (startCalibrationButton == null)
        {
            foreach (var btn in screenInstructions.GetComponentsInChildren<Button>(true))
            {
                if (btn.gameObject.name.Contains("Start"))
                {
                    startCalibrationButton = btn;
                    break;
                }
            }
        }

        // Auto-discover text components if null
        if ((botGreetingTMP == null || botInstructionTMP == null) && screenInstructions != null)
        {
            var tmps = screenInstructions.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var t in tmps)
            {
                if (t.gameObject.name == "BotGreeting") botGreetingTMP = t;
                else if (t.gameObject.name == "MissionList") botInstructionTMP = t;
            }
        }
    }

    public void ShowScreenInstructions()
    {
        if (screenInstructions != null) screenInstructions.SetActive(true);
        if (screenActivity != null) screenActivity.SetActive(false);
        if (screenVictory != null) screenVictory.SetActive(false);
        if (feedbackPanel != null) feedbackPanel.SetActive(false);

        AutoDiscoverInstructionReferences();

        // Wire up Start Button click listener cleanly
        if (startCalibrationButton != null)
        {
            startCalibrationButton.onClick.RemoveAllListeners();
            startCalibrationButton.onClick.AddListener(OnStartCalibrationClicked);
        }

        StartTypewriterInstructions();
    }

    public void StartTypewriterInstructions()
    {
        if (typewriterCoroutine != null)
        {
            StopCoroutine(typewriterCoroutine);
        }
        typewriterCoroutine = StartCoroutine(TypewriterInstructionRoutine());
    }

    private IEnumerator TypewriterInstructionRoutine()
    {
        isTypewriterFinished = false;

        AutoDiscoverInstructionReferences();

        // Reset scales to zero for the choreographed entrance sequence
        if (titleLogoTransform != null)
        {
            titleLogoTransform.DOKill();
            titleLogoTransform.localScale = Vector3.zero;
        }

        if (botCharacterTransform != null)
        {
            botCharacterTransform.DOKill();
            botCharacterTransform.localScale = Vector3.zero;
        }

        if (instructionCard != null)
        {
            instructionCard.DOKill();
            instructionCard.localScale = Vector3.zero;
        }

        if (startCalibrationButton != null)
        {
            startCalibrationButton.transform.DOKill();
            startCalibrationButton.transform.localScale = Vector3.zero;
            startCalibrationButton.gameObject.SetActive(false);
        }

        string greetingText = "<color=#FFD700><b>ROBOT CARGO BUDDY LOAD-E SAYS:</b></color> <color=#00F0FF>\"HEY CADET! HELP ME BALANCE THE SHIP!\"</color>";
        string missionText = 
            "<b><color=#00F0FF>SECTOR 1: THE POPULAR PACKAGE (MODE)</color></b>\n" +
            "<color=#E2E8F4>Find the cargo number that appears most often in the supply load!</color>\n\n" +
            "<b><color=#2AFFA2>SECTOR 2: THE CENTER CONVEYOR (MEDIAN)</color></b>\n" +
            "<color=#E2E8F4>Line up cargo weights in order and tap the container right in the exact center!</color>\n\n" +
            "<b><color=#FFB800>SECTOR 3: FAIR SHARE BATTERIES (MEAN)</color></b>\n" +
            "<color=#E2E8F4>Share energy rods equally until all four generator towers hold the exact same power!</color>\n\n" +
            "<b><color=#99B4FF>SECTOR 4: DISTANCE GAP (RANGE)</color></b>  -  <b><color=#00F0FF>SECTOR 5: SAFE LANDING</color></b>\n" +
            "<color=#E2E8F4>Measure sensor distance spreads and pick the calmest landing pad for takeoff!</color>";

        // Pre-fill text with 0 characters visible
        if (botGreetingTMP != null)
        {
            botGreetingTMP.text = greetingText;
            botGreetingTMP.maxVisibleCharacters = 0;
            botGreetingTMP.ForceMeshUpdate();
        }

        if (botInstructionTMP != null)
        {
            botInstructionTMP.text = missionText;
            botInstructionTMP.maxVisibleCharacters = 0;
            botInstructionTMP.ForceMeshUpdate();
        }

        // =====================================================================
        // STEP 1: TITLE LOGO POPS UP FIRST
        // =====================================================================
        yield return new WaitForSeconds(0.1f);
        if (titleLogoTransform != null)
        {
            titleLogoTransform.localScale = Vector3.zero;
            titleLogoTransform.DOScale(1f, 0.45f).SetEase(Ease.OutBack);
        }
        yield return new WaitForSeconds(0.38f);

        // =====================================================================
        // STEP 2: THEN ROBOT MASCOT "LOAD-E" POPS UP
        // =====================================================================
        if (botCharacterTransform != null)
        {
            botCharacterTransform.localScale = Vector3.zero;
            botCharacterTransform.DOScale(1f, 0.45f).SetEase(Ease.OutBack);
            // Initiate gentle hover once popped
            botCharacterTransform.anchoredPosition = new Vector2(botCharacterTransform.anchoredPosition.x, -50f);
            botCharacterTransform.DOAnchorPosY(-30f, 1.4f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine).SetDelay(0.45f);
        }
        yield return new WaitForSeconds(0.38f);

        // =====================================================================
        // STEP 3: THEN DIALOGUE BOX / SPEECH CARD POPS UP
        // =====================================================================
        if (instructionCard != null)
        {
            instructionCard.localScale = Vector3.zero;
            instructionCard.DOScale(1f, 0.45f).SetEase(Ease.OutBack);
        }
        yield return new WaitForSeconds(0.42f);

        // =====================================================================
        // STEP 4: THEN START TYPING (Controlled Typing Speed)
        // =====================================================================
        // 4A. Type Header Greeting
        if (botGreetingTMP != null)
        {
            int totalGreetingChars = botGreetingTMP.textInfo.characterCount;
            for (int i = 0; i <= totalGreetingChars; i++)
            {
                botGreetingTMP.maxVisibleCharacters = i;
                yield return new WaitForSeconds(Mathf.Max(0.001f, greetingTypingSpeed));
            }
        }

        // Mascot cheer bounce after greeting
        if (botCharacterTransform != null)
        {
            botCharacterTransform.DOPunchScale(Vector3.one * 0.08f, 0.25f, 5, 1f);
        }
        yield return new WaitForSeconds(0.12f);

        // 4B. Type Mission Instructions
        if (botInstructionTMP != null)
        {
            int totalMissionChars = botInstructionTMP.textInfo.characterCount;
            for (int i = 0; i <= totalMissionChars; i++)
            {
                botInstructionTMP.maxVisibleCharacters = i;

                // Subtle robotic speaking gestures during dialogue typing
                if (i % 25 == 0 && botCharacterTransform != null)
                {
                    botCharacterTransform.DOPunchPosition(new Vector3(0, 3f, 0), 0.12f);
                }

                yield return new WaitForSeconds(Mathf.Max(0.001f, typingSpeed));
            }
        }

        isTypewriterFinished = true;
        yield return new WaitForSeconds(0.15f);

        // =====================================================================
        // STEP 5: AFTER TYPING, SHOW START BUTTON POP
        // =====================================================================
        if (startCalibrationButton != null)
        {
            startCalibrationButton.gameObject.SetActive(true);
            startCalibrationButton.transform.DOKill();
            startCalibrationButton.transform.localScale = Vector3.zero;
            startCalibrationButton.transform.DOScale(1f, 0.45f).SetEase(Ease.OutBack);
        }
    }

    public void SkipTypewriterInstructions()
    {
        if (isTypewriterFinished) return;
        if (typewriterCoroutine != null) StopCoroutine(typewriterCoroutine);
        isTypewriterFinished = true;

        AutoDiscoverInstructionReferences();

        // Immediately expand Title, Mascot, Speech Card, and Start Button to 1
        if (titleLogoTransform != null)
        {
            titleLogoTransform.DOKill();
            titleLogoTransform.localScale = Vector3.one;
        }

        if (botCharacterTransform != null)
        {
            botCharacterTransform.DOKill();
            botCharacterTransform.localScale = Vector3.one;
            botCharacterTransform.anchoredPosition = new Vector2(botCharacterTransform.anchoredPosition.x, -50f);
            botCharacterTransform.DOAnchorPosY(-30f, 1.4f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
        }

        if (instructionCard != null)
        {
            instructionCard.DOKill();
            instructionCard.localScale = Vector3.one;
        }

        if (botGreetingTMP != null)
        {
            botGreetingTMP.maxVisibleCharacters = botGreetingTMP.textInfo.characterCount;
        }

        if (botInstructionTMP != null)
        {
            botInstructionTMP.maxVisibleCharacters = botInstructionTMP.textInfo.characterCount;
        }

        if (startCalibrationButton != null)
        {
            startCalibrationButton.gameObject.SetActive(true);
            startCalibrationButton.transform.DOKill();
            startCalibrationButton.transform.localScale = Vector3.one;
        }
    }

    public void OnStartCalibrationClicked()
    {
        Debug.Log("[CargoGrid] Start Mission Clicked! Transitioning to Level 1...");
        SkipTypewriterInstructions();
        EnsureActivityItems();
        if (AudioManager.instance != null) AudioManager.instance.PlayClick();

        if (screenInstructions != null) screenInstructions.SetActive(false);
        if (screenActivity != null) screenActivity.SetActive(true);
        if (screenVictory != null) screenVictory.SetActive(false);

        currentItemIndex = 0;
        LoadStage(currentItemIndex);
    }

    public void LoadStage(int index)
    {
        if (index < 0 || index >= activityItems.Count)
        {
            ShowVictoryScreen();
            return;
        }

        currentItemIndex = index;
        isAnsweringLocked = false;
        ActivityItem item = activityItems[index];

        if (feedbackPanel != null) feedbackPanel.SetActive(false);

        // Update Top HUD
        if (stageIndicatorTMP != null)
            stageIndicatorTMP.text = $"SECTOR 0{index + 1} / 0{activityItems.Count}";

        UpdateStageDots(index);
        UpdateEnergyBar(index);

        // Update Stage Header
        if (stageTitleTMP != null) stageTitleTMP.text = $"{item.stageTag} • {item.stageTitle.ToUpper()}";
        if (stageSubtitleTMP != null) stageSubtitleTMP.text = $"CONCEPT: {item.conceptName.ToUpper()}";
        if (screenPromptTMP != null) screenPromptTMP.text = item.screenPrompt;
        if (taskPromptTMP != null) taskPromptTMP.text = item.taskPrompt;
        if (botDialogueTMP != null) botDialogueTMP.text = item.botCheerPrompt;

        // Visual Display Sprite inside Viewscreen
        if (visualDisplayImage != null && item.visualSprite != null)
        {
            visualDisplayImage.sprite = item.visualSprite;
            visualDisplayImage.gameObject.SetActive(true);
            visualDisplayImage.transform.DOKill();
            visualDisplayImage.transform.localScale = new Vector3(0.92f, 0.92f, 1f);
            visualDisplayImage.transform.DOScale(1f, 0.35f).SetEase(Ease.OutBack);
        }

        // Animate Prompts
        if (screenPromptTMP != null)
        {
            screenPromptTMP.transform.DOKill();
            screenPromptTMP.transform.localScale = new Vector3(0.95f, 0.95f, 1f);
            screenPromptTMP.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack);
        }

        // Setup Options inside Conveyor Tray
        int optCount = item.options.Length;
        float podWidth = (optCount <= 2) ? 520f : (optCount == 3) ? 420f : (optCount == 4) ? 350f : 300f;
        float podHeight = 230f;

        for (int i = 0; i < optionButtons.Length; i++)
        {
            if (i < item.options.Length)
            {
                optionButtons[i].gameObject.SetActive(true);
                optionButtons[i].interactable = true;

                LayoutElement le = optionButtons[i].GetComponent<LayoutElement>();
                if (le != null)
                {
                    le.preferredWidth = podWidth;
                    le.minWidth = podWidth;
                    le.preferredHeight = podHeight;
                    le.minHeight = podHeight;
                }

                RectTransform rt = optionButtons[i].GetComponent<RectTransform>();
                if (rt != null) rt.sizeDelta = new Vector2(podWidth, podHeight);

                if (optionTexts[i] != null) optionTexts[i].text = item.options[i];
                if (optionSubtitles[i] != null)
                {
                    bool hasSub = item.optionSubtitles != null && i < item.optionSubtitles.Length && !string.IsNullOrEmpty(item.optionSubtitles[i]);
                    if (optionSubtitles[i].transform.parent != null && optionSubtitles[i].transform.parent != optionButtons[i].transform)
                    {
                        optionSubtitles[i].transform.parent.gameObject.SetActive(hasSub);
                    }
                    optionSubtitles[i].gameObject.SetActive(hasSub);
                    if (hasSub) optionSubtitles[i].text = item.optionSubtitles[i];
                }

                if (optionBgImages[i] != null)
                {
                    optionBgImages[i].DOKill();
                    optionBgImages[i].color = colorDefaultCrateBg;
                }

                optionButtons[i].transform.DOKill();
                optionButtons[i].transform.localScale = Vector3.zero;
                optionButtons[i].transform.DOScale(1f, 0.3f).SetDelay(0.06f * i).SetEase(Ease.OutBack);
            }
            else
            {
                optionButtons[i].gameObject.SetActive(false);
            }
        }
    }

    private void OnOptionSelected(int optionIndex)
    {
        if (isAnsweringLocked) return;

        ActivityItem item = activityItems[currentItemIndex];
        bool isCorrect = (optionIndex == item.correctOptionIndex);

        if (isCorrect)
        {
            isAnsweringLocked = true;
            if (AudioManager.instance != null) AudioManager.instance.PlayCorrect();

            currentStreak++;
            int basePoints = 500;
            int streakBonus = (currentStreak > 1) ? (currentStreak - 1) * 150 : 0;
            int totalAward = basePoints + streakBonus;

            int oldScore = currentScore;
            currentScore += totalAward;

            if (scoreTMP != null)
            {
                scoreTMP.transform.DOKill();
                scoreTMP.transform.DOPunchScale(Vector3.one * 0.25f, 0.35f, 8, 1f);
                DOVirtual.Int(oldScore, currentScore, 0.5f, v => {
                    scoreTMP.text = $"SCORE: {v:N0}";
                    // Dial sound removed per user request
                });
            }

            if (streakTMP != null)
            {
                streakTMP.text = currentStreak > 1 ? $"STREAK x{currentStreak}!" : "STREAK x1";
                streakTMP.color = currentStreak > 1 ? colorNeonAmber : colorNeonCyan;
                streakTMP.transform.DOKill();
                streakTMP.transform.DOPunchScale(Vector3.one * 0.2f, 0.3f);
            }

            if (floatingBonusTMP != null)
            {
                floatingBonusTMP.text = $"+{totalAward} PTS!" + (currentStreak > 1 ? $" (STREAK x{currentStreak}!)" : "");
                floatingBonusTMP.gameObject.SetActive(true);
                floatingBonusTMP.transform.DOKill();
                floatingBonusTMP.transform.position = optionButtons[optionIndex].transform.position + new Vector3(0, 110, 0);
                floatingBonusTMP.transform.localScale = Vector3.zero;
                floatingBonusTMP.alpha = 1f;
                floatingBonusTMP.transform.DOScale(1.2f, 0.25f).SetEase(Ease.OutBack);
                floatingBonusTMP.transform.DOMoveY(floatingBonusTMP.transform.position.y + 50f, 0.8f).SetEase(Ease.OutCubic);
                floatingBonusTMP.DOFade(0f, 0.8f).SetDelay(0.35f).OnComplete(() => floatingBonusTMP.gameObject.SetActive(false));
            }

            if (optionBgImages[optionIndex] != null)
                optionBgImages[optionIndex].color = colorCorrectBg;

            optionButtons[optionIndex].transform.DOPunchScale(new Vector3(0.12f, 0.12f, 0f), 0.3f, 8, 1f);

            FlashScreen(colorNeonGreen, 0.4f);
            if (bayLightingGlow != null)
            {
                bayLightingGlow.color = new Color(0.1f, 0.95f, 0.5f, 0.4f);
                bayLightingGlow.DOFade(0.05f, 1f);
            }

            ShowFeedback(true, $"🎉 AWESOME! SECTOR BALANCED! (+{totalAward} PTS)", item.voiceoverCorrect);
        }
        else
        {
            if (AudioManager.instance != null) AudioManager.instance.PlayWrong();

            currentStreak = 0;
            if (streakTMP != null)
            {
                streakTMP.text = "STREAK x1";
                streakTMP.color = new Color(0.6f, 0.7f, 0.85f, 0.8f);
            }

            // Wrong button flashes red, shakes, and reverts back after 0.9s
            if (optionBgImages[optionIndex] != null)
            {
                optionBgImages[optionIndex].DOKill();
                optionBgImages[optionIndex].color = colorWrongBg;
                optionBgImages[optionIndex].DOColor(colorDefaultCrateBg, 0.35f).SetDelay(0.9f);
            }

            optionButtons[optionIndex].transform.DOShakePosition(0.4f, 12f, 16, 90f);

            FlashScreen(colorNeonAmber, 0.35f);
            if (bayLightingGlow != null)
            {
                bayLightingGlow.color = new Color(1f, 0.65f, 0.1f, 0.35f);
                bayLightingGlow.DOFade(0.05f, 0.8f);
            }

            // Keep robot hint updated right on screen
            if (botDialogueTMP != null)
            {
                botDialogueTMP.text = $"<color=#FFAA00>LOAD-E: \"{item.voiceoverWrong}\"</color>";
                botDialogueTMP.transform.DOKill();
                botDialogueTMP.transform.DOPunchScale(Vector3.one * 0.12f, 0.3f);
            }

            ShowFeedback(false, "🤖 OOPS! LET'S CHECK AGAIN!", item.voiceoverWrong);

            // Auto-hide the feedback panel after 1.6s so player can easily read question again!
            DOVirtual.DelayedCall(1.6f, () => {
                if (feedbackPanel != null && !isAnsweringLocked)
                {
                    feedbackPanel.transform.DOKill();
                    feedbackPanel.transform.DOScale(0f, 0.25f).SetEase(Ease.InBack).OnComplete(() => {
                        feedbackPanel.SetActive(false);
                    });
                }
            });
        }
    }

    private void ShowFeedback(bool isCorrect, string statusTitle, string voiceover)
    {
        if (feedbackPanel == null) return;

        feedbackPanel.SetActive(true);
        feedbackPanel.transform.DOKill();
        feedbackPanel.transform.localScale = Vector3.zero;
        feedbackPanel.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack);

        if (feedbackStatusTMP != null)
        {
            feedbackStatusTMP.text = statusTitle;
            feedbackStatusTMP.color = isCorrect ? colorNeonGreen : colorNeonAmber;
        }

        if (feedbackVoiceoverTMP != null)
        {
            feedbackVoiceoverTMP.text = $"\"{voiceover}\"";
        }

        if (feedbackStatusBg != null)
        {
            feedbackStatusBg.color = isCorrect ? colorCorrectBg : colorWrongBg;
        }

        if (nextStageButton != null)
        {
            nextStageButton.gameObject.SetActive(isCorrect);
            TextMeshProUGUI nextBtnText = nextStageButton.GetComponentInChildren<TextMeshProUGUI>();
            if (nextBtnText != null)
            {
                nextBtnText.text = (currentItemIndex == activityItems.Count - 1) ? "BLAST OFF! (FINALIZE) ►" : "NEXT SECTOR ►";
            }
        }
    }

    public void OnNextStageClicked()
    {
        if (AudioManager.instance != null) AudioManager.instance.PlayClick();

        currentItemIndex++;
        if (currentItemIndex < activityItems.Count)
        {
            LoadStage(currentItemIndex);
        }
        else
        {
            ShowVictoryScreen();
        }
    }

    public void ShowVictoryScreen()
    {
        if (AudioManager.instance != null) AudioManager.instance.PlayCorrect();

        if (screenActivity != null) screenActivity.SetActive(false);
        if (screenVictory != null) screenVictory.SetActive(true);

        if (energyBarFill != null)
        {
            energyBarFill.DOFillAmount(1f, 0.8f).SetEase(Ease.OutCubic);
            if (energyPercentTMP != null)
            {
                energyPercentTMP.text = "100% STABILITY";
                energyPercentTMP.color = new Color(0.02f, 0.08f, 0.16f, 1f);
            }
        }

        if (victoryScoreTMP != null)
        {
            victoryScoreTMP.text = $"MISSION SCORE: {currentScore:N0} PTS";
        }

        if (victoryCard != null)
        {
            victoryCard.localScale = Vector3.zero;
            victoryCard.DOScale(1f, 0.5f).SetEase(Ease.OutBack);
        }
    }

    public void OnPlayAgainClicked()
    {
        if (AudioManager.instance != null) AudioManager.instance.PlayClick();
        currentScore = 0;
        currentStreak = 0;
        if (scoreTMP != null) scoreTMP.text = "SCORE: 0";
        if (streakTMP != null) streakTMP.text = "STREAK x1";
        ShowScreenInstructions();
    }

    private void UpdateStageDots(int activeIndex)
    {
        if (stageDots == null) return;
        for (int i = 0; i < stageDots.Length; i++)
        {
            if (i < activeIndex)
            {
                stageDots[i].color = colorNeonGreen;
                if (stageDotLabels != null && i < stageDotLabels.Length)
                    stageDotLabels[i].color = new Color(0.02f, 0.08f, 0.16f, 1f);
            }
            else if (i == activeIndex)
            {
                stageDots[i].color = colorNeonCyan;
                if (stageDotLabels != null && i < stageDotLabels.Length)
                    stageDotLabels[i].color = new Color(0.02f, 0.08f, 0.16f, 1f);
            }
            else
            {
                stageDots[i].color = new Color(0.1f, 0.16f, 0.28f, 0.85f);
                if (stageDotLabels != null && i < stageDotLabels.Length)
                    stageDotLabels[i].color = new Color(0.6f, 0.72f, 0.88f, 0.7f);
            }
        }
    }

    private void UpdateEnergyBar(int stageIndex)
    {
        if (energyBarFill != null)
        {
            float targetFill = (float)(stageIndex + 1) / activityItems.Count;
            energyBarFill.DOFillAmount(targetFill, 0.5f).SetEase(Ease.OutCubic);
            if (energyPercentTMP != null)
            {
                int pct = Mathf.RoundToInt(targetFill * 100f);
                energyPercentTMP.text = $"{pct}% STABILITY";
                energyPercentTMP.color = new Color(0.02f, 0.08f, 0.16f, 1f);
            }
        }
    }

    private void FlashScreen(Color flashColor, float duration)
    {
        if (screenFlashOverlay == null) return;
        screenFlashOverlay.DOKill();
        screenFlashOverlay.color = flashColor;
        screenFlashOverlay.DOFade(0f, duration).SetEase(Ease.OutQuad);
    }
}
