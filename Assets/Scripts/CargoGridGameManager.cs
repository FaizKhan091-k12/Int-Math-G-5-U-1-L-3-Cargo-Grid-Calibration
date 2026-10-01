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

    [Header("Robot Speech & Idle Motion Settings")]
    [Tooltip("Height in pixels for the robot Y-axis hover motion while speaking and idling.")]
    [Range(0f, 40f)]
    public float speechVibrationHeight = 14f;

    [Tooltip("Duration in seconds for one up/down cycle of the robot hover motion.")]
    [Range(0.15f, 2.0f)]
    public float speechVibrationSpeed = 0.65f;

    private Coroutine typewriterCoroutine;
    private bool isTypewriterFinished = false;
    private Vector2 cachedBotAnchoredPos;
    private bool hasCachedBotPos = false;

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

    [Header("Player Health (Hearts) Settings")]
    [Tooltip("Maximum player lives (hearts). Default 3.")]
    public int maxLives = 3;
    public int currentLives = 3;
    public Transform heartsContainer;
    public Image[] heartFullImages;
    public Image[] heartEmptyImages;
    public Sprite heartFullSprite;
    public Sprite heartEmptySprite;

    [Header("Try Again (Game Over) Panel")]
    public GameObject tryAgainPanel;
    public RectTransform tryAgainCard;
    public Button tryAgainButton;
    public TextMeshProUGUI tryAgainTitleTMP;
    public TextMeshProUGUI tryAgainMessageTMP;
    [Tooltip("If true, clicking Try Again restarts the mission from Sector 01. If false, retries the current Sector.")]
    public bool restartFromFirstSectorOnGameOver = true;

    [Header("Randomization Settings")]
    [Tooltip("If enabled, the order of the questions/sectors is shuffled when starting or replaying the mission.")]
    public bool randomizeQuestions = true;

    [Tooltip("If enabled, the order of option buttons for each question will be shuffled.")]
    public bool randomizeOptions = true;

    private List<ActivityItem> originalActivityItems;
    private int currentCorrectOptionIndex = 0;

    [Header("Activity Data")]
    public List<ActivityItem> activityItems = new List<ActivityItem>();
    private int currentItemIndex = 0;
    private bool isAnsweringLocked = false;

    [Header("Scoring System")]
    [Tooltip("Base score awarded for each correct question.")]
    public int baseScorePerQuestion = 500;

    [Tooltip("Bonus score added for every consecutive streak level (e.g. Streak 2 = +200, Streak 3 = +400, etc.).")]
    public int streakBonusPerCount = 200;

    [Tooltip("Total bonus points earned purely from streaks during the current mission.")]
    public int totalStreakBonusEarned = 0;

    public int currentScore = 0;
    public int currentStreak = 0;
    public int highestStreak = 0;
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

    private Vector3 authoredVisualDisplayScale = Vector3.one;
    private Vector3 authoredPromptScale = Vector3.one;
    private Vector3[] authoredOptionScales;
    private bool hasCachedActivityScales = false;

    private void CacheActivityAuthoredScales()
    {
        if (hasCachedActivityScales) return;

        if (visualDisplayImage != null)
            authoredVisualDisplayScale = visualDisplayImage.transform.localScale;

        if (screenPromptTMP != null)
            authoredPromptScale = screenPromptTMP.transform.localScale;

        if (optionButtons != null)
        {
            authoredOptionScales = new Vector3[optionButtons.Length];
            for (int i = 0; i < optionButtons.Length; i++)
            {
                if (optionButtons[i] != null)
                    authoredOptionScales[i] = optionButtons[i].transform.localScale;
                else
                    authoredOptionScales[i] = Vector3.one;
            }
        }

        hasCachedActivityScales = true;
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(instance.gameObject);
        }
        instance = this;
        CacheActivityAuthoredScales();
        EnsureActivityItems();
        PrepareQuestionList();
    }

    private void Start()
    {
        EnsureActivityItems();
        PrepareQuestionList();
        AutoSetupHeartsUI();
        AutoSetupTryAgainUI();
        ResetHeartsUI();
        SetupButtonListeners();
        ShowScreenInstructions();
    }

    public void EnsureActivityItems()
    {
        if (activityItems != null && activityItems.Count > 0)
        {
            if (originalActivityItems == null || originalActivityItems.Count == 0)
            {
                originalActivityItems = new List<ActivityItem>(activityItems);
            }
            return;
        }

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

        if (originalActivityItems == null || originalActivityItems.Count == 0)
        {
            originalActivityItems = new List<ActivityItem>(activityItems);
        }
    }

    public void PrepareQuestionList()
    {
        EnsureActivityItems();

        if (originalActivityItems != null && originalActivityItems.Count > 0)
        {
            activityItems = new List<ActivityItem>(originalActivityItems);
        }

        if (randomizeQuestions && activityItems != null && activityItems.Count > 1)
        {
            for (int i = activityItems.Count - 1; i > 0; i--)
            {
                int rnd = UnityEngine.Random.Range(0, i + 1);
                ActivityItem temp = activityItems[i];
                activityItems[i] = activityItems[rnd];
                activityItems[rnd] = temp;
            }
        }
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

        if (tryAgainButton != null)
        {
            tryAgainButton.onClick.RemoveAllListeners();
            tryAgainButton.onClick.AddListener(OnTryAgainClicked);
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

        if (botCharacterTransform != null && !hasCachedBotPos)
        {
            cachedBotAnchoredPos = botCharacterTransform.anchoredPosition;
            hasCachedBotPos = true;
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
        if (tryAgainPanel != null) tryAgainPanel.SetActive(false);
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

        // Initial State: Scale elements to 0 without touching their anchored positions!
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

        // Hide text characters initially (preserves authored text from unplay mode!)
        if (botGreetingTMP != null)
        {
            if (string.IsNullOrEmpty(botGreetingTMP.text))
            {
                botGreetingTMP.text = "<color=#FFD700><b>ROBOT CARGO BUDDY LOAD-E SAYS:</b></color> <color=#00F0FF>\"HEY CADET! HELP ME BALANCE THE SHIP!\"</color>";
            }
            botGreetingTMP.maxVisibleCharacters = 0;
            botGreetingTMP.ForceMeshUpdate();
        }

        if (botInstructionTMP != null)
        {
            if (string.IsNullOrEmpty(botInstructionTMP.text))
            {
                botInstructionTMP.text = 
                    "<b><color=#00F0FF>SECTOR 1: THE POPULAR PACKAGE (MODE)</color></b>\n" +
                    "<color=#E2E8F4>Find the cargo number that appears most often in the supply load!</color>\n\n" +
                    "<b><color=#2AFFA2>SECTOR 2: THE CENTER CONVEYOR (MEDIAN)</color></b>\n" +
                    "<color=#E2E8F4>Line up cargo weights in order and tap the container right in the exact center!</color>\n\n" +
                    "<b><color=#FFB800>SECTOR 3: FAIR SHARE BATTERIES (MEAN)</color></b>\n" +
                    "<color=#E2E8F4>Share energy rods equally until all four generator towers hold the exact same power!</color>\n\n" +
                    "<b><color=#99B4FF>SECTOR 4: DISTANCE GAP (RANGE)</color></b>  -  <b><color=#00F0FF>SECTOR 5: SAFE LANDING</color></b>\n" +
                    "<color=#E2E8F4>Measure sensor distance spreads and pick the calmest landing pad for takeoff!</color>";
            }
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
            titleLogoTransform.DOScale(Vector3.one, 0.45f).SetEase(Ease.OutBack);
        }
        yield return new WaitForSeconds(0.38f);

        // =====================================================================
        // STEP 2: THEN ROBOT MASCOT "LOAD-E" POPS UP (At exact authored position!)
        // =====================================================================
        if (botCharacterTransform != null)
        {
            botCharacterTransform.localScale = Vector3.zero;
            botCharacterTransform.DOScale(Vector3.one, 0.45f).SetEase(Ease.OutBack);
        }
        yield return new WaitForSeconds(0.38f);

        // =====================================================================
        // STEP 3: THEN DIALOGUE BOX / SPEECH CARD POPS UP
        // =====================================================================
        if (instructionCard != null)
        {
            instructionCard.localScale = Vector3.zero;
            instructionCard.DOScale(Vector3.one, 0.45f).SetEase(Ease.OutBack);
        }
        yield return new WaitForSeconds(0.42f);

        // =====================================================================
        // STEP 4: THEN START TYPING (Smooth Conversational Speech Motion)
        // =====================================================================
        // Start smooth, natural conversational speech floating on the robot along the Y axis
        if (botCharacterTransform != null)
        {
            botCharacterTransform.DOKill();
            botCharacterTransform.anchoredPosition = cachedBotAnchoredPos;
            botCharacterTransform.localEulerAngles = Vector3.zero;
            botCharacterTransform.localScale = Vector3.one;
            botCharacterTransform.DOAnchorPosY(cachedBotAnchoredPos.y + speechVibrationHeight, speechVibrationSpeed)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine);
        }

        // 4A. Type Header Greeting
        if (botGreetingTMP != null)
        {
            botGreetingTMP.ForceMeshUpdate();
            int totalGreetingChars = botGreetingTMP.textInfo.characterCount;
            for (int i = 0; i <= totalGreetingChars; i++)
            {
                botGreetingTMP.maxVisibleCharacters = i;
                yield return new WaitForSeconds(Mathf.Max(0.001f, greetingTypingSpeed));
            }
        }

        // Conversational scale nod transition between greeting and instructions
        if (botCharacterTransform != null)
        {
            botCharacterTransform.DOPunchScale(Vector3.one * 0.05f, 0.2f, 3, 1f);
        }
        yield return new WaitForSeconds(0.12f);

        // 4B. Type Mission Instructions
        if (botInstructionTMP != null)
        {
            botInstructionTMP.ForceMeshUpdate();
            int totalMissionChars = botInstructionTMP.textInfo.characterCount;
            for (int i = 0; i <= totalMissionChars; i++)
            {
                botInstructionTMP.maxVisibleCharacters = i;
                yield return new WaitForSeconds(Mathf.Max(0.001f, typingSpeed));
            }
        }

        isTypewriterFinished = true;

        // When dialogue is finished, KEEP the up-and-down hovering motion running continuously!
        if (botCharacterTransform != null && !DOTween.IsTweening(botCharacterTransform))
        {
            botCharacterTransform.DOAnchorPosY(cachedBotAnchoredPos.y + speechVibrationHeight, speechVibrationSpeed)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine);
        }

        yield return new WaitForSeconds(0.1f);

        // =====================================================================
        // STEP 5: AFTER TYPING, SHOW START BUTTON POP
        // =====================================================================
        if (startCalibrationButton != null)
        {
            startCalibrationButton.gameObject.SetActive(true);
            startCalibrationButton.transform.DOKill();
            startCalibrationButton.transform.localScale = Vector3.zero;
            startCalibrationButton.transform.DOScale(Vector3.one, 0.45f).SetEase(Ease.OutBack);
        }
    }

    public void SkipTypewriterInstructions()
    {
        if (isTypewriterFinished) return;
        if (typewriterCoroutine != null) StopCoroutine(typewriterCoroutine);
        isTypewriterFinished = true;

        AutoDiscoverInstructionReferences();

        if (titleLogoTransform != null)
        {
            titleLogoTransform.DOKill();
            titleLogoTransform.localScale = Vector3.one;
        }

        if (botCharacterTransform != null)
        {
            botCharacterTransform.DOKill();
            botCharacterTransform.anchoredPosition = cachedBotAnchoredPos;
            botCharacterTransform.localEulerAngles = Vector3.zero;
            botCharacterTransform.localScale = Vector3.one;
            botCharacterTransform.DOAnchorPosY(cachedBotAnchoredPos.y + speechVibrationHeight, speechVibrationSpeed)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine);
        }

        if (instructionCard != null)
        {
            instructionCard.DOKill();
            instructionCard.localScale = Vector3.one;
        }

        if (botGreetingTMP != null)
        {
            botGreetingTMP.maxVisibleCharacters = int.MaxValue;
        }

        if (botInstructionTMP != null)
        {
            botInstructionTMP.maxVisibleCharacters = int.MaxValue;
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
        PrepareQuestionList();
        currentLives = maxLives;
        ResetHeartsUI();
        if (AudioManager.instance != null) AudioManager.instance.PlayClick();

        if (screenInstructions != null) screenInstructions.SetActive(false);
        if (screenActivity != null) screenActivity.SetActive(true);
        if (screenVictory != null) screenVictory.SetActive(false);
        if (tryAgainPanel != null) tryAgainPanel.SetActive(false);

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
        if (tryAgainPanel != null) tryAgainPanel.SetActive(false);
        AutoSetupHeartsUI();

        // Update Top HUD
        if (stageIndicatorTMP != null)
            stageIndicatorTMP.text = $"SECTOR 0{index + 1} / 0{activityItems.Count}";

        UpdateStageDots(index);
        UpdateEnergyBar(index);

        // Update Stage Header
        if (stageTitleTMP != null) stageTitleTMP.text = $"[ SECTOR 0{index + 1} ] • {item.stageTitle.ToUpper()}";
        if (stageSubtitleTMP != null) stageSubtitleTMP.text = $"CONCEPT: {item.conceptName.ToUpper()}";
        if (screenPromptTMP != null) screenPromptTMP.text = item.screenPrompt;
        if (taskPromptTMP != null) taskPromptTMP.text = item.taskPrompt;
        if (botDialogueTMP != null) botDialogueTMP.text = item.botCheerPrompt;

        CacheActivityAuthoredScales();

        // Visual Display Sprite inside Viewscreen (Preserves user authored scale)
        if (visualDisplayImage != null && item.visualSprite != null)
        {
            visualDisplayImage.sprite = item.visualSprite;
            visualDisplayImage.gameObject.SetActive(true);
            visualDisplayImage.transform.DOKill();
            visualDisplayImage.transform.localScale = authoredVisualDisplayScale;
        }

        // Prompts (Preserves user authored scale)
        if (screenPromptTMP != null)
        {
            screenPromptTMP.transform.DOKill();
            screenPromptTMP.transform.localScale = authoredPromptScale;
        }

        // Setup Options ordering (shuffled if randomizeOptions is enabled)
        int optCount = (item.options != null) ? item.options.Length : 0;
        int[] displayIndices = new int[optCount];
        for (int k = 0; k < optCount; k++) displayIndices[k] = k;

        if (randomizeOptions && optCount > 1)
        {
            for (int k = optCount - 1; k > 0; k--)
            {
                int rnd = UnityEngine.Random.Range(0, k + 1);
                int temp = displayIndices[k];
                displayIndices[k] = displayIndices[rnd];
                displayIndices[rnd] = temp;
            }
        }

        // Determine which button displays the correct answer
        currentCorrectOptionIndex = item.correctOptionIndex;
        for (int k = 0; k < optCount; k++)
        {
            if (displayIndices[k] == item.correctOptionIndex)
            {
                currentCorrectOptionIndex = k;
                break;
            }
        }

        // Setup Options inside Conveyor Tray - All user-configured button sizes & scales strictly preserved!
        for (int i = 0; i < optionButtons.Length; i++)
        {
            if (i < optCount)
            {
                int origIdx = displayIndices[i];
                optionButtons[i].gameObject.SetActive(true);
                optionButtons[i].interactable = true;

                // User manual sizes (sizeDelta and LayoutElement preferred/min width & height) are 100% preserved! Zero overrides!

                if (optionTexts[i] != null && item.options != null && origIdx < item.options.Length)
                    optionTexts[i].text = item.options[origIdx];

                if (optionSubtitles[i] != null)
                {
                    bool hasSub = item.optionSubtitles != null && origIdx < item.optionSubtitles.Length && !string.IsNullOrEmpty(item.optionSubtitles[origIdx]);
                    if (optionSubtitles[i].transform.parent != null && optionSubtitles[i].transform.parent != optionButtons[i].transform)
                    {
                        optionSubtitles[i].transform.parent.gameObject.SetActive(hasSub);
                    }
                    optionSubtitles[i].gameObject.SetActive(hasSub);
                    if (hasSub) optionSubtitles[i].text = item.optionSubtitles[origIdx];
                }

                if (optionBgImages[i] != null)
                {
                    optionBgImages[i].DOKill();
                    optionBgImages[i].color = colorDefaultCrateBg;
                }

                Vector3 targetScale = (authoredOptionScales != null && i < authoredOptionScales.Length) 
                    ? authoredOptionScales[i] 
                    : Vector3.one;

                optionButtons[i].transform.DOKill();
                optionButtons[i].transform.localScale = targetScale;
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
        bool isCorrect = (optionIndex == currentCorrectOptionIndex);

        if (isCorrect)
        {
            isAnsweringLocked = true;
            if (AudioManager.instance != null) AudioManager.instance.PlayCorrect();

            currentStreak++;
            if (currentStreak > highestStreak) highestStreak = currentStreak;

            int basePoints = baseScorePerQuestion;
            int streakBonus = (currentStreak > 1) ? (currentStreak - 1) * streakBonusPerCount : 0;
            totalStreakBonusEarned += streakBonus;
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
                if (currentStreak > 1)
                {
                    streakTMP.text = $"STREAK x{currentStreak}! (+{streakBonus} BONUS)";
                    streakTMP.color = colorNeonAmber;
                }
                else
                {
                    streakTMP.text = "STREAK x1";
                    streakTMP.color = colorNeonCyan;
                }
                streakTMP.transform.DOKill();
                streakTMP.transform.DOPunchScale(Vector3.one * 0.25f, 0.35f, 8, 1f);
            }

            if (floatingBonusTMP != null)
            {
                if (streakBonus > 0)
                {
                    floatingBonusTMP.text = $"<color=#00E5FF>+{basePoints} PTS</color>\n<size=80%><color=#FFB800> +{streakBonus} STREAK BONUS! (x{currentStreak})</color></size>";
                }
                else
                {
                    floatingBonusTMP.text = $"+{basePoints} PTS";
                }
                floatingBonusTMP.gameObject.SetActive(true);
                floatingBonusTMP.transform.DOKill();
                floatingBonusTMP.transform.position = optionButtons[optionIndex].transform.position + new Vector3(0, 110, 0);
                floatingBonusTMP.transform.localScale = Vector3.zero;
                floatingBonusTMP.alpha = 1f;
                floatingBonusTMP.transform.DOScale(streakBonus > 0 ? 1.35f : 1.15f, 0.25f).SetEase(Ease.OutBack);
                floatingBonusTMP.transform.DOMoveY(floatingBonusTMP.transform.position.y + 60f, 0.85f).SetEase(Ease.OutCubic);
                floatingBonusTMP.DOFade(0f, 0.85f).SetDelay(0.4f).OnComplete(() => floatingBonusTMP.gameObject.SetActive(false));
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

            string feedbackTitle = (streakBonus > 0)
                ? $" AWESOME! SECTOR BALANCED! (+{totalAward} PTS)\n<color=#FFAA00> STREAK x{currentStreak}! (+{streakBonus} BONUS PTS)</color>"
                : $" AWESOME! SECTOR BALANCED! (+{totalAward} PTS)";
            ShowFeedback(true, feedbackTitle, item.voiceoverCorrect);
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

            // Deduct one heart / life
            currentLives--;
            if (currentLives < 0) currentLives = 0;
            UpdateHeartsUI(true);

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

            if (currentLives <= 0)
            {
                isAnsweringLocked = true;
                ShowFeedback(false, "OUT OF SHIELDS!", "All 3 cargo integrity shields depleted! Recalibrating...");
                DOVirtual.DelayedCall(1.0f, () => {
                    ShowTryAgainScreen();
                });
                return;
            }

            ShowFeedback(false, "OOPS! LET'S CHECK AGAIN!", item.voiceoverWrong);

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
        if (tryAgainPanel != null) tryAgainPanel.SetActive(false);

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
            victoryScoreTMP.text = "MISSION SCORE: 0 PTS";
        }

        if (victoryCard != null)
        {
            victoryCard.localScale = Vector3.zero;
            victoryCard.DOScale(1f, 0.5f).SetEase(Ease.OutBack).OnComplete(() =>
            {
                AnimateVictoryScoreRollup();
            });
        }
        else
        {
            AnimateVictoryScoreRollup();
        }
    }

    public void OnPlayAgainClicked()
    {
        if (AudioManager.instance != null) AudioManager.instance.PlayClick();
        currentScore = 0;
        currentStreak = 0;
        highestStreak = 0;
        totalStreakBonusEarned = 0;
        currentLives = maxLives;
        ResetHeartsUI();
        if (scoreTMP != null) scoreTMP.text = "SCORE: 0";
        if (streakTMP != null) streakTMP.text = "STREAK x1";
        if (tryAgainPanel != null) tryAgainPanel.SetActive(false);
        PrepareQuestionList();
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

    // =========================================================================
    // HEALTH (3 HEARTS) SYSTEM
    // =========================================================================
    public void AutoSetupHeartsUI()
    {
        if (heartFullImages != null && heartFullImages.Length == maxLives && heartFullImages[0] != null)
        {
            return;
        }

        // Locate TopBar
        Transform topBarTransform = null;
        if (muteButton != null && muteButton.transform.parent != null)
        {
            topBarTransform = muteButton.transform.parent;
        }
        else if (screenActivity != null)
        {
            topBarTransform = screenActivity.transform.Find("TopBar");
            if (topBarTransform == null)
            {
                foreach (Transform child in screenActivity.transform)
                {
                    if (child.name.ToLower().Contains("topbar") || child.name.ToLower().Contains("top_bar"))
                    {
                        topBarTransform = child;
                        break;
                    }
                }
            }
        }

        if (topBarTransform == null) return;

        // Check if HealthContainer already exists under TopBar
        Transform existingHc = topBarTransform.Find("HealthContainer");
        if (existingHc != null)
        {
            heartsContainer = existingHc;
        }
        else
        {
            GameObject hcObj = new GameObject("HealthContainer", typeof(RectTransform));
            hcObj.transform.SetParent(topBarTransform, false);
            RectTransform rt = hcObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.79f, 0.5f);
            rt.anchorMax = new Vector2(0.79f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(138f, 44f);

            Image bgImg = hcObj.AddComponent<Image>();
            bgImg.color = new Color(0.06f, 0.12f, 0.22f, 0.92f);
            heartsContainer = hcObj.transform;
        }

        if (heartFullSprite == null)
            heartFullSprite = Resources.Load<Sprite>("CargoGrid/Sprites/heart_full");
        if (heartEmptySprite == null)
            heartEmptySprite = Resources.Load<Sprite>("CargoGrid/Sprites/heart_empty");

        if (heartFullSprite == null)
            heartFullSprite = CreateProceduralHeartSprite(true);
        if (heartEmptySprite == null)
            heartEmptySprite = CreateProceduralHeartSprite(false);

        heartFullImages = new Image[maxLives];
        heartEmptyImages = new Image[maxLives];

        float startX = -38f;
        float spacingX = 38f;

        for (int i = 0; i < maxLives; i++)
        {
            string slotName = $"HeartSlot_{i}";
            Transform slotT = heartsContainer.Find(slotName);
            GameObject slotObj;
            if (slotT == null)
            {
                slotObj = new GameObject(slotName, typeof(RectTransform));
                slotObj.transform.SetParent(heartsContainer, false);
                RectTransform slotRt = slotObj.GetComponent<RectTransform>();
                slotRt.anchorMin = new Vector2(0.5f, 0.5f);
                slotRt.anchorMax = new Vector2(0.5f, 0.5f);
                slotRt.pivot = new Vector2(0.5f, 0.5f);
                slotRt.anchoredPosition = new Vector2(startX + i * spacingX, 0f);
                slotRt.sizeDelta = new Vector2(30f, 30f);
            }
            else
            {
                slotObj = slotT.gameObject;
            }

            Transform emptyT = slotObj.transform.Find("Empty");
            GameObject emptyObj;
            if (emptyT == null)
            {
                emptyObj = new GameObject("Empty", typeof(RectTransform), typeof(Image));
                emptyObj.transform.SetParent(slotObj.transform, false);
                RectTransform eRt = emptyObj.GetComponent<RectTransform>();
                eRt.anchorMin = Vector2.zero;
                eRt.anchorMax = Vector2.one;
                eRt.sizeDelta = Vector2.zero;
            }
            else
            {
                emptyObj = emptyT.gameObject;
            }
            Image emptyImg = emptyObj.GetComponent<Image>();
            emptyImg.sprite = heartEmptySprite;
            emptyImg.color = new Color(1f, 1f, 1f, 0.7f);
            emptyImg.preserveAspect = true;
            heartEmptyImages[i] = emptyImg;

            Transform fullT = slotObj.transform.Find("Full");
            GameObject fullObj;
            if (fullT == null)
            {
                fullObj = new GameObject("Full", typeof(RectTransform), typeof(Image));
                fullObj.transform.SetParent(slotObj.transform, false);
                RectTransform fRt = fullObj.GetComponent<RectTransform>();
                fRt.anchorMin = Vector2.zero;
                fRt.anchorMax = Vector2.one;
                fRt.sizeDelta = Vector2.zero;
            }
            else
            {
                fullObj = fullT.gameObject;
            }
            Image fullImg = fullObj.GetComponent<Image>();
            fullImg.sprite = heartFullSprite;
            fullImg.color = Color.white;
            fullImg.preserveAspect = true;
            heartFullImages[i] = fullImg;
        }
    }

    private Sprite CreateProceduralHeartSprite(bool isFilled)
    {
        int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] cols = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float nx = (x - size * 0.5f) / (size * 0.38f);
                float ny = (y - size * 0.42f) / (size * 0.38f);
                float a = nx * nx + ny * ny - 1f;
                float val = a * a * a - nx * nx * ny * ny * ny;

                if (val <= 0.05f)
                {
                    if (isFilled)
                    {
                        cols[y * size + x] = new Color(1f, 0.15f, 0.35f, 1f);
                    }
                    else
                    {
                        bool isEdge = val >= -0.3f;
                        cols[y * size + x] = isEdge ? new Color(0.4f, 0.6f, 0.85f, 0.7f) : new Color(0.08f, 0.14f, 0.22f, 0.5f);
                    }
                }
                else
                {
                    cols[y * size + x] = Color.clear;
                }
            }
        }

        tex.SetPixels(cols);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    public void ResetHeartsUI()
    {
        AutoSetupHeartsUI();
        currentLives = maxLives;

        if (heartFullImages == null) return;

        for (int i = 0; i < heartFullImages.Length; i++)
        {
            if (heartFullImages[i] != null)
            {
                heartFullImages[i].gameObject.SetActive(true);
                heartFullImages[i].transform.DOKill();
                heartFullImages[i].transform.localScale = Vector3.zero;
                heartFullImages[i].transform.DOScale(Vector3.one, 0.35f).SetEase(Ease.OutBack).SetDelay(i * 0.1f);
            }
        }
    }

    public void UpdateHeartsUI(bool animateLoss = false)
    {
        AutoSetupHeartsUI();
        if (heartFullImages == null) return;

        for (int i = 0; i < heartFullImages.Length; i++)
        {
            if (heartFullImages[i] == null) continue;

            if (i < currentLives)
            {
                heartFullImages[i].gameObject.SetActive(true);
                heartFullImages[i].transform.localScale = Vector3.one;
            }
            else
            {
                if (animateLoss && i == currentLives)
                {
                    int lostIdx = i;
                    heartFullImages[lostIdx].transform.DOKill();
                    heartFullImages[lostIdx].transform.DOPunchScale(Vector3.one * 0.45f, 0.2f, 6, 1f).OnComplete(() =>
                    {
                        heartFullImages[lostIdx].transform.DOScale(Vector3.zero, 0.3f).SetEase(Ease.InBack).OnComplete(() =>
                        {
                            heartFullImages[lostIdx].gameObject.SetActive(false);
                        });
                    });
                }
                else
                {
                    heartFullImages[i].transform.DOKill();
                    heartFullImages[i].transform.localScale = Vector3.zero;
                    heartFullImages[i].gameObject.SetActive(false);
                }
            }
        }
    }

    // =========================================================================
    // TRY AGAIN (GAME OVER) PANEL
    // =========================================================================
    public void AutoSetupTryAgainUI()
    {
        if (tryAgainPanel != null) return;

        Canvas canvas = null;
        if (screenActivity != null && screenActivity.transform.parent != null)
        {
            canvas = screenActivity.transform.parent.GetComponent<Canvas>();
        }
        if (canvas == null) canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return;

        Transform existing = canvas.transform.Find("Screen_TryAgain");
        if (existing != null)
        {
            tryAgainPanel = existing.gameObject;
            tryAgainCard = existing.Find("TryAgainCard") as RectTransform;
            tryAgainButton = tryAgainPanel.GetComponentInChildren<Button>(true);
            var tmps = tryAgainPanel.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var t in tmps)
            {
                if (t.gameObject.name.Contains("Title")) tryAgainTitleTMP = t;
                else if (t.gameObject.name.Contains("Message") || t.gameObject.name.Contains("Dialogue")) tryAgainMessageTMP = t;
            }
            if (tryAgainButton != null)
            {
                tryAgainButton.onClick.RemoveAllListeners();
                tryAgainButton.onClick.AddListener(OnTryAgainClicked);
            }
            return;
        }

        GameObject panelObj = new GameObject("Screen_TryAgain", typeof(RectTransform));
        panelObj.transform.SetParent(canvas.transform, false);
        RectTransform panelRt = panelObj.GetComponent<RectTransform>();
        panelRt.anchorMin = Vector2.zero;
        panelRt.anchorMax = Vector2.one;
        panelRt.sizeDelta = Vector2.zero;
        panelRt.anchoredPosition = Vector2.zero;

        Image backdrop = panelObj.AddComponent<Image>();
        backdrop.color = new Color(0.02f, 0.05f, 0.12f, 0.88f);

        GameObject cardObj = new GameObject("TryAgainCard", typeof(RectTransform), typeof(Image));
        cardObj.transform.SetParent(panelObj.transform, false);
        tryAgainCard = cardObj.GetComponent<RectTransform>();
        tryAgainCard.anchorMin = new Vector2(0.5f, 0.5f);
        tryAgainCard.anchorMax = new Vector2(0.5f, 0.5f);
        tryAgainCard.pivot = new Vector2(0.5f, 0.5f);
        tryAgainCard.anchoredPosition = Vector2.zero;
        tryAgainCard.sizeDelta = new Vector2(880f, 520f);

        Image cardImg = cardObj.GetComponent<Image>();
        cardImg.color = new Color(0.06f, 0.1f, 0.18f, 0.98f);

        // Header Title TMP
        GameObject titleObj = new GameObject("TryAgainTitle", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleObj.transform.SetParent(cardObj.transform, false);
        RectTransform titleRt = titleObj.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0.5f, 1f);
        titleRt.anchorMax = new Vector2(0.5f, 1f);
        titleRt.pivot = new Vector2(0.5f, 1f);
        titleRt.anchoredPosition = new Vector2(0f, -45f);
        titleRt.sizeDelta = new Vector2(780f, 70f);

        tryAgainTitleTMP = titleObj.GetComponent<TextMeshProUGUI>();
        tryAgainTitleTMP.text = "CALIBRATION COMPROMISED!";
        tryAgainTitleTMP.fontSize = 38;
        tryAgainTitleTMP.fontStyle = FontStyles.Bold;
        tryAgainTitleTMP.alignment = TextAlignmentOptions.Center;
        tryAgainTitleTMP.color = new Color(1f, 0.28f, 0.35f, 1f);
        if (topTitleTMP != null) tryAgainTitleTMP.font = topTitleTMP.font;

        // Subtitle / Dialogue TMP
        GameObject msgObj = new GameObject("TryAgainMessage", typeof(RectTransform), typeof(TextMeshProUGUI));
        msgObj.transform.SetParent(cardObj.transform, false);
        RectTransform msgRt = msgObj.GetComponent<RectTransform>();
        msgRt.anchorMin = new Vector2(0.5f, 0.5f);
        msgRt.anchorMax = new Vector2(0.5f, 0.5f);
        msgRt.pivot = new Vector2(0.5f, 0.5f);
        msgRt.anchoredPosition = new Vector2(0f, 20f);
        msgRt.sizeDelta = new Vector2(740f, 150f);

        tryAgainMessageTMP = msgObj.GetComponent<TextMeshProUGUI>();
        tryAgainMessageTMP.text = "All 3 cargo integrity shields were depleted!\n\n<color=#00E5FF>LOAD-E: \"Don't give up cadet! Recalibrate the grid and try again!\"</color>";
        tryAgainMessageTMP.fontSize = 24;
        tryAgainMessageTMP.alignment = TextAlignmentOptions.Center;
        tryAgainMessageTMP.color = new Color(0.85f, 0.92f, 1f, 0.95f);
        if (topTitleTMP != null) tryAgainMessageTMP.font = topTitleTMP.font;

        // Try Again Button
        GameObject btnObj = new GameObject("TryAgainButton", typeof(RectTransform), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(cardObj.transform, false);
        RectTransform btnRt = btnObj.GetComponent<RectTransform>();
        btnRt.anchorMin = new Vector2(0.5f, 0f);
        btnRt.anchorMax = new Vector2(0.5f, 0f);
        btnRt.pivot = new Vector2(0.5f, 0f);
        btnRt.anchoredPosition = new Vector2(0f, 50f);
        btnRt.sizeDelta = new Vector2(320f, 68f);

        Image btnImg = btnObj.GetComponent<Image>();
        btnImg.color = colorNeonCyan;

        tryAgainButton = btnObj.GetComponent<Button>();
        tryAgainButton.onClick.RemoveAllListeners();
        tryAgainButton.onClick.AddListener(OnTryAgainClicked);

        GameObject btnTxtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        btnTxtObj.transform.SetParent(btnObj.transform, false);
        RectTransform btnTxtRt = btnTxtObj.GetComponent<RectTransform>();
        btnTxtRt.anchorMin = Vector2.zero;
        btnTxtRt.anchorMax = Vector2.one;
        btnTxtRt.sizeDelta = Vector2.zero;

        TextMeshProUGUI btnTxt = btnTxtObj.GetComponent<TextMeshProUGUI>();
        btnTxt.text = "TRY AGAIN ↺";
        btnTxt.fontSize = 28;
        btnTxt.fontStyle = FontStyles.Bold;
        btnTxt.alignment = TextAlignmentOptions.Center;
        btnTxt.color = new Color(0.02f, 0.08f, 0.16f, 1f);
        if (topTitleTMP != null) btnTxt.font = topTitleTMP.font;

        panelObj.SetActive(false);
        tryAgainPanel = panelObj;
    }

    public void ShowTryAgainScreen()
    {
        AutoSetupTryAgainUI();
        if (tryAgainPanel == null) return;

        if (feedbackPanel != null) feedbackPanel.SetActive(false);
        tryAgainPanel.SetActive(true);

        if (tryAgainCard != null)
        {
            tryAgainCard.DOKill();
            tryAgainCard.localScale = Vector3.zero;
            tryAgainCard.DOScale(Vector3.one, 0.45f).SetEase(Ease.OutBack);
        }

        if (tryAgainMessageTMP != null)
        {
            tryAgainMessageTMP.text = $"All 3 cargo integrity shields were depleted!\nSector Reached: 0{currentItemIndex + 1} / 0{activityItems.Count}  •  Score: {currentScore:N0} PTS\n\n<color=#00E5FF>LOAD-E: \"Don't give up cadet! Recalibrate the grid and try again!\"</color>";
        }
    }

    public void OnTryAgainClicked()
    {
        if (AudioManager.instance != null) AudioManager.instance.PlayClick();

        if (tryAgainCard != null)
        {
            tryAgainCard.DOKill();
            tryAgainCard.DOScale(Vector3.zero, 0.25f).SetEase(Ease.InBack).OnComplete(() =>
            {
                if (tryAgainPanel != null) tryAgainPanel.SetActive(false);
                ResetMissionAfterTryAgain();
            });
        }
        else
        {
            if (tryAgainPanel != null) tryAgainPanel.SetActive(false);
            ResetMissionAfterTryAgain();
        }
    }

    private void ResetMissionAfterTryAgain()
    {
        currentLives = maxLives;
        ResetHeartsUI();
        isAnsweringLocked = false;

        if (restartFromFirstSectorOnGameOver)
        {
            currentScore = 0;
            currentStreak = 0;
            highestStreak = 0;
            totalStreakBonusEarned = 0;
            if (scoreTMP != null) scoreTMP.text = "SCORE: 0";
            if (streakTMP != null) streakTMP.text = "STREAK x1";
            PrepareQuestionList();
            currentItemIndex = 0;
            LoadStage(0);
        }
        else
        {
            LoadStage(currentItemIndex);
        }
    }

    // =========================================================================
    // VICTORY SCORE CALCULATION ROLLUP
    // =========================================================================
    private void AnimateVictoryScoreRollup()
    {
        if (victoryScoreTMP == null) return;

        int targetScore = currentScore;
        if (targetScore <= 0)
        {
            victoryScoreTMP.text = "MISSION SCORE: 0 PTS";
            return;
        }

        victoryScoreTMP.text = "MISSION SCORE: 0 PTS";
        DOVirtual.Int(0, targetScore, 2.0f, val =>
        {
            victoryScoreTMP.text = $"MISSION SCORE: {val:N0} PTS";
        }).SetEase(Ease.OutCubic).OnComplete(() =>
        {
            if (totalStreakBonusEarned > 0)
            {
                victoryScoreTMP.text = $"MISSION SCORE: {targetScore:N0} PTS\n<size=65%><color=#FFAA00> INCLUDES +{totalStreakBonusEarned:N0} STREAK BONUS PTS! (BEST: x{highestStreak})</color></size>";
            }
            else
            {
                victoryScoreTMP.text = $"MISSION SCORE: {targetScore:N0} PTS";
            }
            victoryScoreTMP.transform.DOKill();
            victoryScoreTMP.transform.DOPunchScale(Vector3.one * 0.22f, 0.35f, 6, 1f);
            if (AudioManager.instance != null) AudioManager.instance.PlayCorrect();
        });
    }
}
