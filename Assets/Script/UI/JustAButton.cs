using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class JustAButton : MonoBehaviour
{
    public static JustAButton Instance { get; private set; }

    [Header("Buttons")]
    [SerializeField] private List<Button> buttons = new List<Button>();
    [SerializeField] private Button hostGameButton;
    [SerializeField] private Button joinGameButton;
    [SerializeField] private Button optionsButton;
    [SerializeField] private Button leaderboardButton;
    [SerializeField] private int firstSelectedIndex = 0;
    [SerializeField] private bool wrapSelection = true;

    [Header("Animator")]
    [SerializeField] private Animator anim;
    [SerializeField] private Animator carAnimator;
    [SerializeField] private Animator panelAnim;
    [SerializeField] private string selectedBoolPrefix = "isSelected";
    [SerializeField] private AnimationClip playAnimation;
    [SerializeField] private AnimationClip stopAnimation;

    [Header("Input")]
    [SerializeField] private float moveRepeatDelay = 0.25f;
    [SerializeField] private float gamepadDeadzone = 0.5f;
    [SerializeField] private InputSystemUIInputModule uiInputModule;

    [Header("Options Menu & Volume Sliders")]
    [SerializeField] private GameObject OptionMenu;
    [SerializeField] private Button optionBackButton;
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private TMPro.TextMeshProUGUI musicLabelText;
    [SerializeField] private TMPro.TextMeshProUGUI sfxLabelText;

    [Header("Player Name Modal & Global Leaderboard")]
    [SerializeField] private GameObject nameInputModal;
    [SerializeField] private TMPro.TMP_InputField nameInputField;
    [SerializeField] private Button nameConfirmButton;
    [SerializeField] private Button nameCancelButton;
    [SerializeField] private GameObject globalLeaderboardModal;
    [SerializeField] private TMPro.TextMeshProUGUI globalLeaderboardText;
    [SerializeField] private ScrollRect globalLeaderboardScrollRect;
    [SerializeField] private Scrollbar globalLeaderboardScrollbar;
    [SerializeField] private Button globalLeaderboardBackButton;

    private int selectedIndex = 0;
    private int optionsFocusIndex = 0; // 0 = Music, 1 = SFX, 2 = Back Button
    private int nameModalFocusIndex = 0; // 0 = Confirm, 1 = Cancel
    private float nextMoveTime;
    private float nextOptionsMoveTime;
    private float nextNameModalMoveTime;
    private bool isBusy;
    private bool isOptionMenuOpen;
    private bool isNameModalOpen;
    private bool isLeaderboardModalOpen;
    private System.Action pendingNameAction;

    private readonly Dictionary<Transform, Vector3> initialButtonScales = new Dictionary<Transform, Vector3>();
    private readonly Dictionary<Transform, Coroutine> activePunchCoroutines = new Dictionary<Transform, Coroutine>();

    private void Awake()
    {
        Instance = this;
        DisableUIControllerSubmit();
        CacheInitialButtonScales();
    }

    private void CacheInitialButtonScales()
    {
        initialButtonScales.Clear();
        foreach (Button b in buttons)
        {
            if (b != null && !initialButtonScales.ContainsKey(b.transform))
            {
                initialButtonScales[b.transform] = b.transform.localScale;
            }
        }

        if (hostGameButton != null && !initialButtonScales.ContainsKey(hostGameButton.transform))
            initialButtonScales[hostGameButton.transform] = hostGameButton.transform.localScale;

        if (joinGameButton != null && !initialButtonScales.ContainsKey(joinGameButton.transform))
            initialButtonScales[joinGameButton.transform] = joinGameButton.transform.localScale;

        if (optionsButton != null && !initialButtonScales.ContainsKey(optionsButton.transform))
            initialButtonScales[optionsButton.transform] = optionsButton.transform.localScale;

        if (optionBackButton != null && !initialButtonScales.ContainsKey(optionBackButton.transform))
            initialButtonScales[optionBackButton.transform] = optionBackButton.transform.localScale;
    }

    private void OnEnable()
    {
        isOptionMenuOpen = false;
        isBusy = false;
        optionsFocusIndex = 0;

        if (OptionMenu != null)
        {
            OptionMenu.SetActive(false);
        }

        EnableMainButtons();

        selectedIndex = Mathf.Clamp(firstSelectedIndex, 0, Mathf.Max(0, buttons.Count - 1));
        SelectButton(selectedIndex);

        SetupVolumeSliders();
    }

    private void SetupVolumeSliders()
    {
        if (OptionMenu != null)
        {
            Slider[] foundSliders = OptionMenu.GetComponentsInChildren<Slider>(true);
            if (foundSliders != null)
            {
                foreach (Slider s in foundSliders)
                {
                    string sName = s.gameObject.name.ToLower();
                    if ((sName.Contains("sfx") || sName.Contains("sound") || sName.Contains("effect")) && sfxVolumeSlider == null)
                    {
                        sfxVolumeSlider = s;
                    }
                    else if ((sName.Contains("music") || sName.Contains("bgm")) && musicVolumeSlider == null)
                    {
                        musicVolumeSlider = s;
                    }
                }

                if (musicVolumeSlider == null && foundSliders.Length > 0)
                {
                    musicVolumeSlider = foundSliders[0];
                }
                if (sfxVolumeSlider == null && foundSliders.Length > 1)
                {
                    sfxVolumeSlider = foundSliders[1];
                }
            }

            if (musicLabelText == null || sfxLabelText == null)
            {
                TMPro.TextMeshProUGUI[] foundTexts = OptionMenu.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true);
                if (foundTexts != null)
                {
                    foreach (var t in foundTexts)
                    {
                        string tName = (t.gameObject.name + " " + t.text).ToLower();
                        if ((tName.Contains("music") || tName.Contains("bgm")) && musicLabelText == null)
                        {
                            musicLabelText = t;
                        }
                        else if ((tName.Contains("sfx") || tName.Contains("sound") || tName.Contains("effect")) && sfxLabelText == null)
                        {
                            sfxLabelText = t;
                        }
                    }
                }
            }
        }

        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.onValueChanged.RemoveAllListeners();
            if (AudioManager.Instance != null)
            {
                musicVolumeSlider.value = AudioManager.Instance.GetMusicVolume();
            }
            musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
            AddPointerEnterTrigger(musicVolumeSlider.gameObject, () => { optionsFocusIndex = 0; });
        }

        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.onValueChanged.RemoveAllListeners();
            if (AudioManager.Instance != null)
            {
                sfxVolumeSlider.value = AudioManager.Instance.GetSfxVolume();
            }
            sfxVolumeSlider.onValueChanged.AddListener(OnSfxVolumeChanged);
            AddPointerEnterTrigger(sfxVolumeSlider.gameObject, () => { optionsFocusIndex = 1; });
        }

        if (optionBackButton != null)
        {
            AddPointerEnterTrigger(optionBackButton.gameObject, () => { optionsFocusIndex = 2; });
        }
    }

    private void OnMusicVolumeChanged(float val)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMusicVolume(val);
        }
    }

    private void OnSfxVolumeChanged(float val)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetSfxVolume(val);
        }
    }

    private void Start()
    {
        selectedIndex = Mathf.Clamp(firstSelectedIndex, 0, Mathf.Max(0, buttons.Count - 1));
        SelectButton(selectedIndex);
    }

    private void Update()
    {
        if (isBusy)
        {
            return;
        }

        if (isNameModalOpen)
        {
            ReadNameModalInput();
            return;
        }

        if (isLeaderboardModalOpen)
        {
            ReadLeaderboardModalInput();
            return;
        }

        if (isOptionMenuOpen)
        {
            ReadOptionsInput();
            return;
        }

        Gamepad activeGamepad = GetGamepad();
        if ((Keyboard.current != null && (Keyboard.current.lKey.wasPressedThisFrame || Keyboard.current.tabKey.wasPressedThisFrame)) ||
            (activeGamepad != null && activeGamepad.buttonWest.wasPressedThisFrame))
        {
            OpenGlobalLeaderboard();
            return;
        }

        if (buttons.Count == 0)
        {
            return;
        }

        ReadSelectionInput();
        ReadSubmitInput();
    }

    public void HostGame()
    {
        if (hostGameButton != null) AnimateButtonPress(hostGameButton);
        PromptNameModal(() =>
        {
            if (LobbyUI.Instance != null)
            {
                LobbyUI.Instance.CreateRoom();
            }
        });
    }

    public void JoinGame()
    {
        if (joinGameButton != null) AnimateButtonPress(joinGameButton);
        PromptNameModal(() =>
        {
            if (SceneTransitionManager.Instance != null)
            {
                SceneTransitionManager.Instance.TriggerTransition(() =>
                {
                    if (LobbyUI.Instance != null) LobbyUI.Instance.OpenJoinUI();
                });
            }
            else if (LobbyUI.Instance != null)
            {
                LobbyUI.Instance.OpenJoinUI();
            }
        });
    }

    public void PromptNameModal(System.Action onConfirmed)
    {
        pendingNameAction = onConfirmed;
        isNameModalOpen = true;
        nameModalFocusIndex = 0;
        DisableMainButtons();
        EnsureNameModalBuilt();

        if (nameConfirmButton != null)
        {
            nameConfirmButton.transform.localScale = Vector3.one;
            AddPointerEnterTrigger(nameConfirmButton.gameObject, () => { nameModalFocusIndex = 0; });
        }
        if (nameCancelButton != null)
        {
            nameCancelButton.transform.localScale = Vector3.one;
            AddPointerEnterTrigger(nameCancelButton.gameObject, () => { nameModalFocusIndex = 1; });
        }

        if (nameInputModal != null)
        {
            nameInputModal.SetActive(true);
        }

        if (nameInputField != null)
        {
            nameInputField.text = PlayerPrefs.GetString("PlayerName", "Player");
            nameInputField.Select();
            nameInputField.ActivateInputField();
        }

        if (CursorManager.Instance != null)
        {
            CursorManager.Instance.SetCursorVisibility(true);
        }
        else
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }

    public void OnNameConfirmClicked()
    {
        string name = nameInputField != null ? nameInputField.text.Trim() : "Player";
        if (string.IsNullOrEmpty(name)) name = "Player";
        PlayerPrefs.SetString("PlayerName", name);
        PlayerPrefs.Save();

        CloseNameModal();
        EnableMainButtons();
        var act = pendingNameAction;
        pendingNameAction = null;
        act?.Invoke();
    }

    public void OnNameCancelClicked()
    {
        CloseNameModal();
        pendingNameAction = null;
        EnableMainButtons();
        SelectButton(selectedIndex);
    }

    public void CloseNameModal()
    {
        isNameModalOpen = false;
        if (nameConfirmButton != null) nameConfirmButton.transform.localScale = Vector3.one;
        if (nameCancelButton != null) nameCancelButton.transform.localScale = Vector3.one;

        if (nameInputModal != null)
        {
            nameInputModal.SetActive(false);
        }

        if (CursorManager.Instance != null)
        {
            CursorManager.Instance.SetCursorVisibility(false);
        }
        else
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    private void ReadNameModalInput()
    {
        // Toggle focus strictly with Arrow keys or Gamepad D-pad (and mouse hover)
        if (Time.unscaledTime >= nextNameModalMoveTime)
        {
            int horizontal = 0;
            if (Keyboard.current != null)
            {
                if (Keyboard.current.leftArrowKey.wasPressedThisFrame || Keyboard.current.upArrowKey.wasPressedThisFrame)
                    horizontal = -1;
                else if (Keyboard.current.rightArrowKey.wasPressedThisFrame || Keyboard.current.downArrowKey.wasPressedThisFrame)
                    horizontal = 1;
            }

            Gamepad gamepad = GetGamepad();
            if (gamepad != null)
            {
                if (gamepad.dpad.left.wasPressedThisFrame || gamepad.dpad.up.wasPressedThisFrame)
                    horizontal = -1;
                else if (gamepad.dpad.right.wasPressedThisFrame || gamepad.dpad.down.wasPressedThisFrame)
                    horizontal = 1;
            }

            if (horizontal != 0)
            {
                nameModalFocusIndex = (nameModalFocusIndex + 1) % 2;
                nextNameModalMoveTime = Time.unscaledTime + 0.2f;
            }
        }

        if (Keyboard.current != null)
        {
            if (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.numpadEnterKey.wasPressedThisFrame)
            {
                if (nameModalFocusIndex == 0) OnNameConfirmClicked();
                else OnNameCancelClicked();
                return;
            }
            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                OnNameCancelClicked();
                return;
            }
        }

        Gamepad activeGamepad = GetGamepad();
        if (activeGamepad != null)
        {
            if (activeGamepad.buttonSouth.wasPressedThisFrame)
            {
                if (nameModalFocusIndex == 0) OnNameConfirmClicked();
                else OnNameCancelClicked();
                return;
            }
            if (activeGamepad.buttonEast.wasPressedThisFrame)
            {
                OnNameCancelClicked();
                return;
            }
        }

        AnimateNameModalButtons();
    }

    private void AnimateNameModalButtons()
    {
        float targetConfirmScale = (nameModalFocusIndex == 0) ? 1.15f : 1.0f;
        float targetCancelScale = (nameModalFocusIndex == 1) ? 1.15f : 1.0f;

        if (nameConfirmButton != null)
        {
            nameConfirmButton.transform.localScale = Vector3.Lerp(
                nameConfirmButton.transform.localScale,
                Vector3.one * targetConfirmScale,
                Time.unscaledDeltaTime * 14f
            );
        }

        if (nameCancelButton != null)
        {
            nameCancelButton.transform.localScale = Vector3.Lerp(
                nameCancelButton.transform.localScale,
                Vector3.one * targetCancelScale,
                Time.unscaledDeltaTime * 14f
            );
        }
    }

    private void AddPointerEnterTrigger(GameObject obj, System.Action onEnter)
    {
        if (obj == null) return;
        EventTrigger trigger = obj.GetComponent<EventTrigger>();
        if (trigger == null) trigger = obj.AddComponent<EventTrigger>();

        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = EventTriggerType.PointerEnter;
        entry.callback.AddListener((data) => { onEnter?.Invoke(); });
        trigger.triggers.Add(entry);
    }

    public void OpenGlobalLeaderboard()
    {
        if (isBusy || isOptionMenuOpen || isNameModalOpen) return;

        isLeaderboardModalOpen = true;
        DisableMainButtons();
        EnsureLeaderboardModalBuilt();

        if (globalLeaderboardModal != null)
        {
            globalLeaderboardModal.SetActive(true);
        }

        SetupLeaderboardUI();
        PopulateGlobalLeaderboardUI();

        if (CursorManager.Instance != null)
        {
            CursorManager.Instance.SetCursorVisibility(true);
        }
        else
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        if (globalLeaderboardBackButton != null)
        {
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(globalLeaderboardBackButton.gameObject);
            }
            globalLeaderboardBackButton.Select();
        }
    }

    public void CloseGlobalLeaderboard()
    {
        isLeaderboardModalOpen = false;
        if (globalLeaderboardModal != null)
        {
            globalLeaderboardModal.SetActive(false);
        }

        if (CursorManager.Instance != null)
        {
            CursorManager.Instance.SetCursorVisibility(false);
        }
        else
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        EnableMainButtons();
        SelectButton(selectedIndex);
    }

    private void ReadLeaderboardModalInput()
    {
        // Smooth scrolling strictly via Keyboard Arrow keys / PageUp/PageDown, Gamepad D-Pad, or Mouse Wheel
        float scrollDelta = 0f;
        if (Keyboard.current != null)
        {
            if (Keyboard.current.upArrowKey.isPressed)
            {
                scrollDelta += 2.0f * Time.unscaledDeltaTime;
            }
            else if (Keyboard.current.downArrowKey.isPressed)
            {
                scrollDelta -= 2.0f * Time.unscaledDeltaTime;
            }

            if (Keyboard.current.pageUpKey.wasPressedThisFrame)
            {
                scrollDelta += 0.35f;
            }
            else if (Keyboard.current.pageDownKey.wasPressedThisFrame)
            {
                scrollDelta -= 0.35f;
            }

            if (Keyboard.current.escapeKey.wasPressedThisFrame ||
                Keyboard.current.enterKey.wasPressedThisFrame ||
                Keyboard.current.numpadEnterKey.wasPressedThisFrame ||
                Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                CloseGlobalLeaderboard();
                return;
            }
        }

        Gamepad gamepad = GetGamepad();
        if (gamepad != null)
        {
            Vector2 dpadVec = gamepad.dpad.ReadValue();

            if (dpadVec.y > 0.3f || gamepad.dpad.up.isPressed)
            {
                scrollDelta += 2.0f * Time.unscaledDeltaTime;
            }
            else if (dpadVec.y < -0.3f || gamepad.dpad.down.isPressed)
            {
                scrollDelta -= 2.0f * Time.unscaledDeltaTime;
            }

            if (gamepad.buttonEast.wasPressedThisFrame ||
                gamepad.buttonWest.wasPressedThisFrame ||
                gamepad.buttonSouth.wasPressedThisFrame)
            {
                CloseGlobalLeaderboard();
                return;
            }
        }

        if (Mouse.current != null)
        {
            float mouseWheel = Mouse.current.scroll.ReadValue().y;
            if (Mathf.Abs(mouseWheel) > 0.01f)
            {
                scrollDelta += Mathf.Sign(mouseWheel) * 0.15f;
            }
        }

        if (Mathf.Abs(scrollDelta) > 0.0001f)
        {
            if (globalLeaderboardScrollRect != null)
            {
                globalLeaderboardScrollRect.verticalNormalizedPosition = Mathf.Clamp01(globalLeaderboardScrollRect.verticalNormalizedPosition + scrollDelta);
            }
            if (globalLeaderboardScrollbar != null)
            {
                globalLeaderboardScrollbar.value = Mathf.Clamp01(globalLeaderboardScrollbar.value + scrollDelta);
            }
        }
    }

    private void SetupLeaderboardUI()
    {
        if (globalLeaderboardModal == null) return;

        // 1. Maintain the full wooden board display size (~720px height)
        if (globalLeaderboardScrollRect != null)
        {
            RectTransform scrollRectRt = globalLeaderboardScrollRect.GetComponent<RectTransform>();
            scrollRectRt.anchorMin = new Vector2(0f, 0.5f);
            scrollRectRt.anchorMax = new Vector2(1f, 0.5f);
            scrollRectRt.anchoredPosition = new Vector2(0f, -10f);
            scrollRectRt.sizeDelta = new Vector2(-160f, 720f); // 720px height matches original large board area

            // Add RectMask2D on the viewport so overflowing rows are clipped cleanly
            RectMask2D mask = globalLeaderboardScrollRect.GetComponent<RectMask2D>();
            if (mask == null)
            {
                mask = globalLeaderboardScrollRect.gameObject.AddComponent<RectMask2D>();
            }

            // Move Text to a dedicated child Content GameObject if it was placed on the ScrollRect itself
            Transform existingContent = globalLeaderboardScrollRect.transform.Find("LeaderboardContent");
            GameObject contentObj;
            TMPro.TextMeshProUGUI childTmp;

            if (existingContent != null)
            {
                contentObj = existingContent.gameObject;
                childTmp = contentObj.GetComponent<TMPro.TextMeshProUGUI>();
            }
            else
            {
                contentObj = new GameObject("LeaderboardContent", typeof(RectTransform), typeof(CanvasRenderer), typeof(TMPro.TextMeshProUGUI));
                contentObj.transform.SetParent(globalLeaderboardScrollRect.transform, false);
                childTmp = contentObj.GetComponent<TMPro.TextMeshProUGUI>();

                if (globalLeaderboardText != null && globalLeaderboardText.font != null)
                {
                    childTmp.font = globalLeaderboardText.font;
                }
                childTmp.color = Color.white;
                childTmp.richText = true;

                if (globalLeaderboardText != null && globalLeaderboardText.gameObject == globalLeaderboardScrollRect.gameObject)
                {
                    globalLeaderboardText.text = "";
                    globalLeaderboardText.enabled = false;
                }
            }

            RectTransform contentRt = contentObj.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.anchoredPosition = Vector2.zero;
            contentRt.sizeDelta = new Vector2(-60f, 1500f); // Generous height for large text entries

            // Large 62px arcade font size so exactly 5 names fill the 720px board window
            childTmp.fontSize = 62f;
            childTmp.enableAutoSizing = false;
            childTmp.enableWordWrapping = false;
            childTmp.overflowMode = TMPro.TextOverflowModes.Overflow;
            childTmp.alignment = TMPro.TextAlignmentOptions.TopLeft;

            globalLeaderboardText = childTmp;

            globalLeaderboardScrollRect.viewport = scrollRectRt;
            globalLeaderboardScrollRect.content = contentRt;
            globalLeaderboardScrollRect.horizontal = false;
            globalLeaderboardScrollRect.vertical = true;
            globalLeaderboardScrollRect.movementType = ScrollRect.MovementType.Clamped;
            globalLeaderboardScrollRect.scrollSensitivity = 50f;

            if (globalLeaderboardScrollbar != null)
            {
                RectTransform scrollbarRt = globalLeaderboardScrollbar.GetComponent<RectTransform>();
                scrollbarRt.anchorMin = new Vector2(1f, 0f);
                scrollbarRt.anchorMax = new Vector2(1f, 1f);
                scrollbarRt.anchoredPosition = new Vector2(-15f, 0f);
                scrollbarRt.sizeDelta = new Vector2(14f, -20f);

                Image trackImg = globalLeaderboardScrollbar.GetComponent<Image>();
                if (trackImg != null)
                {
                    trackImg.color = new Color(0.12f, 0.15f, 0.20f, 0.65f);
                }

                if (globalLeaderboardScrollbar.targetGraphic != null)
                {
                    globalLeaderboardScrollbar.targetGraphic.color = new Color(0f, 1f, 0.64f, 0.9f);
                }

                globalLeaderboardScrollbar.direction = Scrollbar.Direction.BottomToTop;
                globalLeaderboardScrollbar.size = 0.45f;
                globalLeaderboardScrollRect.verticalScrollbar = globalLeaderboardScrollbar;
                globalLeaderboardScrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
            }
        }
    }

    private void PopulateGlobalLeaderboardUI()
    {
        if (globalLeaderboardText == null && globalLeaderboardModal == null) return;

        SetupLeaderboardUI();

        if (globalLeaderboardText == null) return;

        if (LeaderboardManager.Instance == null)
        {
            globalLeaderboardText.alignment = TMPro.TextAlignmentOptions.Center;
            globalLeaderboardText.text = "<color=#6B7C93>Leaderboard unavailable.</color>";
            return;
        }

        List<LeaderboardEntry> entries = LeaderboardManager.Instance.GetTopEntries();
        if (entries == null || entries.Count == 0)
        {
            globalLeaderboardText.alignment = TMPro.TextAlignmentOptions.Center;
            globalLeaderboardText.text = "<size=110%><color=#8E9BAE>NO CLEAN RUNS REGISTERED YET</color></size>\n\n<size=85%><color=#6B7C93>Clear all 6 stages without timing out to qualify for the Global Leaderboard!</color></size>";
            return;
        }

        globalLeaderboardText.alignment = TMPro.TextAlignmentOptions.TopLeft;

        string table = "<size=95%><b><color=#8E9BAE>" +
                       "<pos=4%>RANK" +
                       "<pos=19%>DRIVER" +
                       "<pos=50%>TIME" +
                       "<pos=71%>DEATHS" +
                       "<pos=87%>GRADE" +
                       "</color></b></size>\n\n";

        for (int i = 0; i < entries.Count; i++)
        {
            var e = entries[i];
            System.TimeSpan tSpan = System.TimeSpan.FromSeconds(e.totalTimeSeconds);
            string tStr = string.Format("{0:D2}:{1:D2}", tSpan.Minutes, tSpan.Seconds);
            string rankMedal = i switch
            {
                0 => "<color=#FFD700>#1</color>",
                1 => "<color=#E2E8F0>#2</color>",
                2 => "<color=#CD7F32>#3</color>",
                _ => $"<color=#8E9BAE>#{(i + 1)}</color>"
            };
            string gradeColor = e.grade switch
            {
                "S" => "#FFD700",
                "A" => "#00FFA3",
                "B" => "#00D2FF",
                "C" => "#FF9900",
                _   => "#FF4D6D"
            };
            string deathColor = e.totalDeaths == 0 ? "#00FFA3" : "#FF6B6B";
            string nameTruncated = (e.playerName.Length > 12) ? e.playerName.Substring(0, 12) : e.playerName;

            string deathsStr = e.totalDeaths.ToString();
            string deathPos = (deathsStr.Length > 1) ? "<pos=72.5%>" : "<pos=73.5%>";

            table += $"<pos=4%><b>{rankMedal}</b>" +
                     $"<pos=19%><color=#FFFFFF>{nameTruncated}</color>" +
                     $"<pos=50%><b><color=#00FFA3>{tStr}</color></b>" +
                     $"{deathPos}<color={deathColor}>{deathsStr}</color>" +
                     $"<pos=88%><color={gradeColor}>[{e.grade}]</color>\n\n";
        }
        globalLeaderboardText.text = table;
        globalLeaderboardText.ForceMeshUpdate();

        // Calculate preferred height and size Content RectTransform so scrolling activates properly
        if (globalLeaderboardScrollRect != null && globalLeaderboardScrollRect.content != null)
        {
            RectTransform contentRt = globalLeaderboardScrollRect.content;
            float textPrefHeight = globalLeaderboardText.preferredHeight;
            float viewportHeight = (globalLeaderboardScrollRect.viewport != null) ? globalLeaderboardScrollRect.viewport.rect.height : 720f;

            float targetHeight = Mathf.Max(textPrefHeight + 80f, viewportHeight + 150f);
            contentRt.sizeDelta = new Vector2(contentRt.sizeDelta.x, targetHeight);
        }

        Canvas.ForceUpdateCanvases();

        if (globalLeaderboardScrollbar != null)
        {
            globalLeaderboardScrollbar.value = 1f;
        }
        if (globalLeaderboardScrollRect != null)
        {
            globalLeaderboardScrollRect.verticalNormalizedPosition = 1f;
        }
    }

    private void EnsureNameModalBuilt()
    {
        if (nameInputModal != null) return;

        Canvas canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

        GameObject overlay = new GameObject("NameInputModalOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        overlay.transform.SetParent(canvas.transform, false);
        RectTransform rt = overlay.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        Image img = overlay.GetComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0.8f);

        GameObject box = new GameObject("Box", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        box.transform.SetParent(overlay.transform, false);
        RectTransform boxRt = box.GetComponent<RectTransform>();
        boxRt.sizeDelta = new Vector2(520f, 260f);
        Image boxImg = box.GetComponent<Image>();
        boxImg.color = new Color(0.09f, 0.11f, 0.14f, 0.98f);

        // Title
        GameObject titleObj = new GameObject("Title", typeof(RectTransform), typeof(CanvasRenderer), typeof(TMPro.TextMeshProUGUI));
        titleObj.transform.SetParent(box.transform, false);
        RectTransform titleRt = titleObj.GetComponent<RectTransform>();
        titleRt.anchoredPosition = new Vector2(0f, 80f);
        titleRt.sizeDelta = new Vector2(480f, 40f);
        TMPro.TextMeshProUGUI titleTmp = titleObj.GetComponent<TMPro.TextMeshProUGUI>();
        titleTmp.text = "<b><color=#00FFA3>ENTER YOUR</color> <color=#FFFFFF>NAME</color></b>";
        titleTmp.fontSize = 24;
        titleTmp.alignment = TMPro.TextAlignmentOptions.Center;

        // Subtitle
        GameObject subObj = new GameObject("Subtitle", typeof(RectTransform), typeof(CanvasRenderer), typeof(TMPro.TextMeshProUGUI));
        subObj.transform.SetParent(box.transform, false);
        RectTransform subRt = subObj.GetComponent<RectTransform>();
        subRt.anchoredPosition = new Vector2(0f, 48f);
        subRt.sizeDelta = new Vector2(480f, 30f);
        TMPro.TextMeshProUGUI subTmp = subObj.GetComponent<TMPro.TextMeshProUGUI>();
        subTmp.text = "<color=#8E9BAE>Name will be recorded on the Global Leaderboard</color>";
        subTmp.fontSize = 14;
        subTmp.alignment = TMPro.TextAlignmentOptions.Center;

        // InputField Box
        GameObject inputObj = new GameObject("InputField", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(TMPro.TMP_InputField));
        inputObj.transform.SetParent(box.transform, false);
        RectTransform inputRt = inputObj.GetComponent<RectTransform>();
        inputRt.anchoredPosition = new Vector2(0f, 0f);
        inputRt.sizeDelta = new Vector2(360f, 45f);
        Image inputImg = inputObj.GetComponent<Image>();
        inputImg.color = new Color(0.05f, 0.07f, 0.09f, 1f);
        nameInputField = inputObj.GetComponent<TMPro.TMP_InputField>();

        // Text Component
        GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TMPro.TextMeshProUGUI));
        textObj.transform.SetParent(inputObj.transform, false);
        RectTransform textRt = textObj.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = new Vector2(10f, 0f);
        textRt.offsetMax = new Vector2(-10f, 0f);
        TMPro.TextMeshProUGUI tmp = textObj.GetComponent<TMPro.TextMeshProUGUI>();
        tmp.fontSize = 18;
        tmp.color = Color.white;
        tmp.alignment = TMPro.TextAlignmentOptions.MidlineLeft;
        nameInputField.textComponent = tmp;

        // Confirm Button
        GameObject confirmBtnObj = new GameObject("ConfirmBtn", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        confirmBtnObj.transform.SetParent(box.transform, false);
        RectTransform confirmRt = confirmBtnObj.GetComponent<RectTransform>();
        confirmRt.anchoredPosition = new Vector2(-90f, -70f);
        confirmRt.sizeDelta = new Vector2(150f, 40f);
        Image confirmImg = confirmBtnObj.GetComponent<Image>();
        confirmImg.color = new Color(0.14f, 0.52f, 0.21f, 1f);
        nameConfirmButton = confirmBtnObj.GetComponent<Button>();
        nameConfirmButton.onClick.AddListener(OnNameConfirmClicked);

        GameObject confirmTextObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TMPro.TextMeshProUGUI));
        confirmTextObj.transform.SetParent(confirmBtnObj.transform, false);
        RectTransform ctRt = confirmTextObj.GetComponent<RectTransform>();
        ctRt.anchorMin = Vector2.zero;
        ctRt.anchorMax = Vector2.one;
        ctRt.offsetMin = Vector2.zero;
        ctRt.offsetMax = Vector2.zero;
        TMPro.TextMeshProUGUI ctTmp = confirmTextObj.GetComponent<TMPro.TextMeshProUGUI>();
        ctTmp.text = "<b>CONTINUE</b>";
        ctTmp.fontSize = 16;
        ctTmp.alignment = TMPro.TextAlignmentOptions.Center;
        ctTmp.color = Color.white;

        // Cancel Button
        GameObject cancelBtnObj = new GameObject("CancelBtn", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        cancelBtnObj.transform.SetParent(box.transform, false);
        RectTransform cancelRt = cancelBtnObj.GetComponent<RectTransform>();
        cancelRt.anchoredPosition = new Vector2(90f, -70f);
        cancelRt.sizeDelta = new Vector2(150f, 40f);
        Image cancelImg = cancelBtnObj.GetComponent<Image>();
        cancelImg.color = new Color(0.2f, 0.23f, 0.27f, 1f);
        nameCancelButton = cancelBtnObj.GetComponent<Button>();
        nameCancelButton.onClick.AddListener(OnNameCancelClicked);

        GameObject cancelTextObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TMPro.TextMeshProUGUI));
        cancelTextObj.transform.SetParent(cancelBtnObj.transform, false);
        RectTransform cancelTRt = cancelTextObj.GetComponent<RectTransform>();
        cancelTRt.anchorMin = Vector2.zero;
        cancelTRt.anchorMax = Vector2.one;
        cancelTRt.offsetMin = Vector2.zero;
        cancelTRt.offsetMax = Vector2.zero;
        TMPro.TextMeshProUGUI cancelTmp = cancelTextObj.GetComponent<TMPro.TextMeshProUGUI>();
        cancelTmp.text = "<b>CANCEL</b>";
        cancelTmp.fontSize = 16;
        cancelTmp.alignment = TMPro.TextAlignmentOptions.Center;
        cancelTmp.color = Color.white;

        nameInputModal = overlay;
    }

    private void EnsureLeaderboardModalBuilt()
    {
        if (globalLeaderboardModal != null) return;

        Canvas canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

        GameObject overlay = new GameObject("GlobalLeaderboardModalOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        overlay.transform.SetParent(canvas.transform, false);
        RectTransform rt = overlay.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        Image img = overlay.GetComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0.88f);

        GameObject box = new GameObject("Box", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        box.transform.SetParent(overlay.transform, false);
        RectTransform boxRt = box.GetComponent<RectTransform>();
        boxRt.sizeDelta = new Vector2(700f, 480f);
        Image boxImg = box.GetComponent<Image>();
        boxImg.color = new Color(0.09f, 0.11f, 0.14f, 0.98f);

        // Title
        GameObject titleObj = new GameObject("Title", typeof(RectTransform), typeof(CanvasRenderer), typeof(TMPro.TextMeshProUGUI));
        titleObj.transform.SetParent(box.transform, false);
        RectTransform titleRt = titleObj.GetComponent<RectTransform>();
        titleRt.anchoredPosition = new Vector2(0f, 190f);
        titleRt.sizeDelta = new Vector2(660f, 50f);
        TMPro.TextMeshProUGUI titleTmp = titleObj.GetComponent<TMPro.TextMeshProUGUI>();
        titleTmp.text = "<b><color=#00FFA3>GLOBAL</color> <color=#FFFFFF>LEADERBOARD</color></b>\n<size=50%><color=#8E9BAE>TOP DRIVERS (CLEAN RUNS ONLY)</color></size>";
        titleTmp.fontSize = 24;
        titleTmp.alignment = TMPro.TextAlignmentOptions.Center;

        // Scroll Container (Transparent, bounds the scrollable TMP text area)
        GameObject scrollViewObj = new GameObject("ScrollArea", typeof(RectTransform), typeof(ScrollRect));
        scrollViewObj.transform.SetParent(box.transform, false);
        RectTransform scrollRt = scrollViewObj.GetComponent<RectTransform>();
        scrollRt.anchoredPosition = new Vector2(-8f, 18f);
        scrollRt.sizeDelta = new Vector2(640f, 265f);

        globalLeaderboardScrollRect = scrollViewObj.GetComponent<ScrollRect>();
        globalLeaderboardScrollRect.horizontal = false;
        globalLeaderboardScrollRect.vertical = true;
        globalLeaderboardScrollRect.movementType = ScrollRect.MovementType.Clamped;
        globalLeaderboardScrollRect.scrollSensitivity = 35f;

        // Viewport (Masks the text so it does not bleed beyond the bounds)
        GameObject viewportObj = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
        viewportObj.transform.SetParent(scrollViewObj.transform, false);
        RectTransform viewportRt = viewportObj.GetComponent<RectTransform>();
        viewportRt.anchorMin = Vector2.zero;
        viewportRt.anchorMax = Vector2.one;
        viewportRt.offsetMin = Vector2.zero;
        viewportRt.offsetMax = Vector2.zero;

        globalLeaderboardScrollRect.viewport = viewportRt;

        // Content
        GameObject contentObj = new GameObject("Content", typeof(RectTransform), typeof(CanvasRenderer), typeof(TMPro.TextMeshProUGUI), typeof(ContentSizeFitter));
        contentObj.transform.SetParent(viewportObj.transform, false);
        RectTransform contentRt = contentObj.GetComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0f, 1f);
        contentRt.anchorMax = new Vector2(1f, 1f);
        contentRt.pivot = new Vector2(0.5f, 1f);
        contentRt.anchoredPosition = Vector2.zero;
        contentRt.sizeDelta = new Vector2(0f, 300f);

        ContentSizeFitter fitter = contentObj.GetComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        globalLeaderboardScrollRect.content = contentRt;

        globalLeaderboardText = contentObj.GetComponent<TMPro.TextMeshProUGUI>();
        globalLeaderboardText.fontSize = 17;
        globalLeaderboardText.alignment = TMPro.TextAlignmentOptions.TopLeft;
        globalLeaderboardText.enableWordWrapping = false;

        // Sleek Vertical Scrollbar on Right Side
        GameObject scrollbarObj = new GameObject("Scrollbar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Scrollbar));
        scrollbarObj.transform.SetParent(box.transform, false);
        RectTransform scrollbarRt = scrollbarObj.GetComponent<RectTransform>();
        scrollbarRt.anchoredPosition = new Vector2(325f, 18f);
        scrollbarRt.sizeDelta = new Vector2(8f, 260f);
        Image scrollbarTrack = scrollbarObj.GetComponent<Image>();
        scrollbarTrack.color = new Color(0.14f, 0.17f, 0.22f, 0.8f);

        GameObject slidingArea = new GameObject("Sliding Area", typeof(RectTransform));
        slidingArea.transform.SetParent(scrollbarObj.transform, false);
        RectTransform slidingAreaRt = slidingArea.GetComponent<RectTransform>();
        slidingAreaRt.anchorMin = Vector2.zero;
        slidingAreaRt.anchorMax = Vector2.one;
        slidingAreaRt.offsetMin = Vector2.zero;
        slidingAreaRt.offsetMax = Vector2.zero;

        GameObject handleObj = new GameObject("Handle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        handleObj.transform.SetParent(slidingArea.transform, false);
        RectTransform handleRt = handleObj.GetComponent<RectTransform>();
        handleRt.anchorMin = Vector2.zero;
        handleRt.anchorMax = Vector2.one;
        handleRt.offsetMin = Vector2.zero;
        handleRt.offsetMax = Vector2.zero;
        Image handleImg = handleObj.GetComponent<Image>();
        handleImg.color = new Color(0f, 1f, 0.64f, 0.85f); // #00FFA3 Accent

        globalLeaderboardScrollbar = scrollbarObj.GetComponent<Scrollbar>();
        globalLeaderboardScrollbar.handleRect = handleRt;
        globalLeaderboardScrollbar.targetGraphic = handleImg;
        globalLeaderboardScrollbar.direction = Scrollbar.Direction.BottomToTop;
        globalLeaderboardScrollbar.numberOfSteps = 0;
        globalLeaderboardScrollbar.size = 0.3f;
        globalLeaderboardScrollbar.value = 1f;

        // Connect scrollbar to ScrollRect
        globalLeaderboardScrollRect.verticalScrollbar = globalLeaderboardScrollbar;
        globalLeaderboardScrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;

        // Scroll Hint Note
        GameObject hintObj = new GameObject("ScrollHint", typeof(RectTransform), typeof(CanvasRenderer), typeof(TMPro.TextMeshProUGUI));
        hintObj.transform.SetParent(box.transform, false);
        RectTransform hintRt = hintObj.GetComponent<RectTransform>();
        hintRt.anchoredPosition = new Vector2(0f, -135f);
        hintRt.sizeDelta = new Vector2(600f, 20f);
        TMPro.TextMeshProUGUI hintTmp = hintObj.GetComponent<TMPro.TextMeshProUGUI>();
        hintTmp.text = "<color=#6B7C93><size=75%>Scroll using Arrow Keys [▲/▼], Gamepad D-Pad, or Mouse Wheel</size></color>";
        hintTmp.alignment = TMPro.TextAlignmentOptions.Center;

        // Back Button
        GameObject backBtnObj = new GameObject("BackBtn", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        backBtnObj.transform.SetParent(box.transform, false);
        RectTransform backRt = backBtnObj.GetComponent<RectTransform>();
        backRt.anchoredPosition = new Vector2(0f, -190f);
        backRt.sizeDelta = new Vector2(200f, 42f);
        Image backImg = backBtnObj.GetComponent<Image>();
        backImg.color = new Color(0.18f, 0.22f, 0.28f, 1f);
        globalLeaderboardBackButton = backBtnObj.GetComponent<Button>();
        globalLeaderboardBackButton.onClick.AddListener(CloseGlobalLeaderboard);

        GameObject backTextObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TMPro.TextMeshProUGUI));
        backTextObj.transform.SetParent(backBtnObj.transform, false);
        RectTransform backTRt = backTextObj.GetComponent<RectTransform>();
        backTRt.anchorMin = Vector2.zero;
        backTRt.anchorMax = Vector2.one;
        backTRt.offsetMin = Vector2.zero;
        backTRt.offsetMax = Vector2.zero;
        TMPro.TextMeshProUGUI backTmp = backTextObj.GetComponent<TMPro.TextMeshProUGUI>();
        backTmp.text = "<b>BACK TO MENU</b>";
        backTmp.fontSize = 16;
        backTmp.alignment = TMPro.TextAlignmentOptions.Center;
        backTmp.color = Color.white;

        globalLeaderboardModal = overlay;
    }

    public void Quit()
    {
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.TriggerTransition(() =>
            {
                Application.Quit();
            });
        }
        else
        {
            Application.Quit();
        }
    }

    public void Options()
    {
        if (optionsButton != null) AnimateButtonPress(optionsButton);
        if (isBusy || isOptionMenuOpen || isNameModalOpen || isLeaderboardModalOpen) return;

        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.TriggerTransition(() =>
            {
                OpenOptions();
            });
        }
        else
        {
            OpenOptions();
        }
    }

    public void BackToOptions()
    {
        if (optionBackButton != null) AnimateButtonPress(optionBackButton);
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.TriggerTransition(() =>
            {
                CloseOptions();
            });
        }
        else
        {
            CloseOptions();
        }
    }

    private float nextSliderAdjustTime;

    private void OpenOptions()
    {
        if (isOptionMenuOpen)
        {
            return;
        }

        isOptionMenuOpen = true;
        optionsFocusIndex = 0;

        DisableMainButtons();

        if (OptionMenu != null)
        {
            OptionMenu.SetActive(true);
        }

        SetupVolumeSliders();

        if (CursorManager.Instance != null)
        {
            CursorManager.Instance.SetCursorVisibility(true);
        }
        else
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        if (optionBackButton != null)
        {
            optionBackButton.interactable = true;

            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
                EventSystem.current.SetSelectedGameObject(optionBackButton.gameObject);
            }

            optionBackButton.Select();
        }
    }

    private void CloseOptions()
    {
        isOptionMenuOpen = false;

        if (musicLabelText != null) musicLabelText.transform.localScale = Vector3.one;
        if (sfxLabelText != null) sfxLabelText.transform.localScale = Vector3.one;
        if (optionBackButton != null) optionBackButton.transform.localScale = Vector3.one;

        if (OptionMenu != null)
        {
            OptionMenu.SetActive(false);
        }

        if (CursorManager.Instance != null)
        {
            CursorManager.Instance.SetCursorVisibility(false);
        }
        else
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        EnableMainButtons();

        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }

        SelectButton(selectedIndex);
    }

    public void DisableMainButtons()
    {
        foreach (Button button in buttons)
        {
            if (button != null)
            {
                button.interactable = false;
            }
        }
        if (hostGameButton != null) hostGameButton.interactable = false;
        if (joinGameButton != null) joinGameButton.interactable = false;
        if (optionsButton != null) optionsButton.interactable = false;
        if (leaderboardButton != null) leaderboardButton.interactable = false;
    }

    public void EnableMainButtons()
    {
        foreach (Button button in buttons)
        {
            if (button != null)
            {
                button.interactable = true;
            }
        }
        if (hostGameButton != null) hostGameButton.interactable = true;
        if (joinGameButton != null) joinGameButton.interactable = true;
        if (optionsButton != null) optionsButton.interactable = true;
        if (leaderboardButton != null) leaderboardButton.interactable = true;
    }

    private void ReadOptionsInput()
    {
        if (Keyboard.current != null)
        {
            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                CloseOptions();
                return;
            }
        }

        Gamepad gamepad = GetGamepad();

        if (gamepad != null)
        {
            if (gamepad.buttonEast.wasPressedThisFrame)
            {
                CloseOptions();
                return;
            }
        }

        // Navigate between options items (0: Music, 1: SFX, 2: Back Button)
        if (Time.unscaledTime >= nextOptionsMoveTime)
        {
            int verticalMove = 0;
            if (Keyboard.current != null)
            {
                if (Keyboard.current.upArrowKey.wasPressedThisFrame)
                    verticalMove = -1;
                else if (Keyboard.current.downArrowKey.wasPressedThisFrame)
                    verticalMove = 1;
            }

            if (gamepad != null)
            {
                if (gamepad.dpad.up.wasPressedThisFrame)
                    verticalMove = -1;
                else if (gamepad.dpad.down.wasPressedThisFrame)
                    verticalMove = 1;
            }

            if (verticalMove != 0)
            {
                int maxItems = (optionBackButton != null) ? 3 : (sfxVolumeSlider != null ? 2 : 1);
                optionsFocusIndex = (optionsFocusIndex + verticalMove + maxItems) % maxItems;
                nextOptionsMoveTime = Time.unscaledTime + 0.2f;
            }
        }

        // Handle Gamepad D-Pad / Keyboard Arrow Keys Horizontal Slider Control
        if (Time.unscaledTime >= nextSliderAdjustTime)
        {
            float horizontal = 0f;

            if (Keyboard.current != null)
            {
                if (Keyboard.current.leftArrowKey.isPressed)
                    horizontal = -1f;
                else if (Keyboard.current.rightArrowKey.isPressed)
                    horizontal = 1f;
            }

            if (gamepad != null)
            {
                if (gamepad.dpad.left.isPressed)
                    horizontal = -1f;
                else if (gamepad.dpad.right.isPressed)
                    horizontal = 1f;
            }

            if (Mathf.Abs(horizontal) > 0.1f)
            {
                float step = 0.05f * Mathf.Sign(horizontal);

                // Determine target slider based on focus or availability
                Slider targetSlider = null;
                if (optionsFocusIndex == 1 && sfxVolumeSlider != null)
                {
                    targetSlider = sfxVolumeSlider;
                }
                else if (optionsFocusIndex == 0 && musicVolumeSlider != null)
                {
                    targetSlider = musicVolumeSlider;
                }
                else if (musicVolumeSlider != null)
                {
                    targetSlider = musicVolumeSlider;
                }
                else if (sfxVolumeSlider != null)
                {
                    targetSlider = sfxVolumeSlider;
                }

                if (targetSlider != null)
                {
                    targetSlider.value = Mathf.Clamp01(targetSlider.value + step);
                    nextSliderAdjustTime = Time.unscaledTime + 0.12f;
                }
            }
        }

        if (optionBackButton != null && optionsFocusIndex == 2)
        {
            bool submitPressed = false;

            if (Keyboard.current != null)
            {
                submitPressed =
                    Keyboard.current.enterKey.wasPressedThisFrame ||
                    Keyboard.current.numpadEnterKey.wasPressedThisFrame ||
                    Keyboard.current.spaceKey.wasPressedThisFrame;
            }

            if (gamepad != null)
            {
                if (gamepad.buttonSouth.wasPressedThisFrame)
                {
                    submitPressed = true;
                }
            }

            if (submitPressed &&
                optionBackButton.IsActive() &&
                optionBackButton.IsInteractable())
            {
                optionBackButton.onClick.Invoke();
            }
        }

        AnimateOptionsVisuals();
    }

    private void AnimateOptionsVisuals()
    {
        float targetMusicScale = (optionsFocusIndex == 0) ? 1.2f : 1.0f;
        float targetSfxScale = (optionsFocusIndex == 1) ? 1.2f : 1.0f;
        float targetBackScale = (optionsFocusIndex == 2) ? 1.15f : 1.0f;

        if (musicLabelText != null)
        {
            musicLabelText.transform.localScale = Vector3.Lerp(
                musicLabelText.transform.localScale,
                Vector3.one * targetMusicScale,
                Time.unscaledDeltaTime * 12f
            );
        }

        if (sfxLabelText != null)
        {
            sfxLabelText.transform.localScale = Vector3.Lerp(
                sfxLabelText.transform.localScale,
                Vector3.one * targetSfxScale,
                Time.unscaledDeltaTime * 12f
            );
        }

        if (optionBackButton != null)
        {
            optionBackButton.transform.localScale = Vector3.Lerp(
                optionBackButton.transform.localScale,
                Vector3.one * targetBackScale,
                Time.unscaledDeltaTime * 12f
            );
        }
    }

    private Gamepad GetGamepad()
    {
        if (Gamepad.current != null)
        {
            return Gamepad.current;
        }

        if (Gamepad.all.Count > 0)
        {
            return Gamepad.all[0];
        }

        return null;
    }

    private void ReadSelectionInput()
    {
        if (Time.unscaledTime < nextMoveTime)
        {
            return;
        }

        float vertical = 0f;

        // 1. Keyboard Arrow Keys ONLY (WASD disabled for UI selection)
        if (Keyboard.current != null)
        {
            if (Keyboard.current.upArrowKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
            {
                vertical = 1f;
            }
            else if (Keyboard.current.downArrowKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
            {
                vertical = -1f;
            }
        }

        // 2. Gamepad D-Pad ONLY (Left stick disabled for UI selection)
        Gamepad gamepad = GetGamepad();

        if (gamepad != null)
        {
            float dpadY = gamepad.dpad.ReadValue().y;
            float dpadX = gamepad.dpad.ReadValue().x;

            if (Mathf.Abs(dpadY) >= gamepadDeadzone)
            {
                vertical = dpadY;
            }
            else if (Mathf.Abs(dpadX) >= gamepadDeadzone)
            {
                vertical = -dpadX;
            }
        }

        if (vertical > gamepadDeadzone)
        {
            MoveSelection(-1);
        }
        else if (vertical < -gamepadDeadzone)
        {
            MoveSelection(1);
        }
    }

    private void ReadSubmitInput()
    {
        bool submitPressed = false;

        if (Keyboard.current != null)
        {
            submitPressed =
                Keyboard.current.enterKey.wasPressedThisFrame ||
                Keyboard.current.numpadEnterKey.wasPressedThisFrame ||
                Keyboard.current.spaceKey.wasPressedThisFrame;
        }

        Gamepad gamepad = GetGamepad();

        if (gamepad != null)
        {
            if (gamepad.buttonSouth.wasPressedThisFrame)
            {
                submitPressed = true;
            }
        }

        if (submitPressed)
        {
            SubmitSelectedButton();
        }
    }

    private void MoveSelection(int direction)
    {
        int nextIndex = selectedIndex + direction;

        if (wrapSelection)
        {
            if (nextIndex < 0)
            {
                nextIndex = buttons.Count - 1;
            }
            else if (nextIndex >= buttons.Count)
            {
                nextIndex = 0;
            }
        }
        else
        {
            nextIndex = Mathf.Clamp(
                nextIndex,
                0,
                buttons.Count - 1
            );
        }

        SelectButton(nextIndex);

        nextMoveTime =
            Time.unscaledTime + moveRepeatDelay;
    }

    public void SelectButton(int index)
    {
        if (buttons.Count == 0 ||
            index < 0 ||
            index >= buttons.Count)
        {
            return;
        }

        selectedIndex = index;

        Button selectedButton = buttons[selectedIndex];

        if (EventSystem.current != null &&
            selectedButton != null)
        {
            EventSystem.current.SetSelectedGameObject(
                selectedButton.gameObject
            );

            selectedButton.Select();
        }

        UpdateAnimatorSelection();
    }

    private void UpdateAnimatorSelection()
    {
        for (int i = 0; i < buttons.Count; i++)
        {
            string parameterName = selectedBoolPrefix + (i + 1);

            if (anim != null && HasAnimatorParameter(anim, parameterName, AnimatorControllerParameterType.Bool))
            {
                anim.SetBool(parameterName, i == selectedIndex);
            }

            if (carAnimator != null && HasAnimatorParameter(carAnimator, parameterName, AnimatorControllerParameterType.Bool))
            {
                carAnimator.SetBool(parameterName, i == selectedIndex);
            }

            if (panelAnim != null && HasAnimatorParameter(panelAnim, parameterName, AnimatorControllerParameterType.Bool))
            {
                panelAnim.SetBool(parameterName, i == selectedIndex);
            }
        }
    }

    private void SubmitSelectedButton()
    {
        if (selectedIndex < 0 ||
            selectedIndex >= buttons.Count)
        {
            return;
        }

        Button selectedButton = buttons[selectedIndex];

        if (selectedButton == null ||
            !selectedButton.gameObject.activeInHierarchy ||
            !selectedButton.interactable)
        {
            return;
        }

        AnimateButtonPress(selectedButton);

        if (selectedButton == hostGameButton)
        {
            HostGame();
            return;
        }

        if (selectedButton == joinGameButton)
        {
            JoinGame();
            return;
        }

        if (selectedButton == leaderboardButton)
        {
            OpenGlobalLeaderboard();
            return;
        }

        if (selectedButton == optionsButton)
        {
            Options();
            return;
        }

        selectedButton.onClick.Invoke();
    }

    public void AnimateButtonPress(Button btn)
    {
        if (btn == null) return;
        Transform t = btn.transform;

        if (activePunchCoroutines.ContainsKey(t) && activePunchCoroutines[t] != null)
        {
            StopCoroutine(activePunchCoroutines[t]);
        }

        activePunchCoroutines[t] = StartCoroutine(ButtonPunchRoutine(t));
    }

    private IEnumerator ButtonPunchRoutine(Transform targetTransform)
    {
        if (targetTransform == null) yield break;

        if (!initialButtonScales.ContainsKey(targetTransform))
        {
            initialButtonScales[targetTransform] = targetTransform.localScale;
        }

        Vector3 baseScale = initialButtonScales[targetTransform];
        targetTransform.localScale = baseScale * 0.88f;

        yield return new WaitForSecondsRealtime(0.08f);

        targetTransform.localScale = baseScale;
        activePunchCoroutines.Remove(targetTransform);
    }

    private bool HasAnimatorParameter(
        Animator animatorToCheck,
        string parameterName,
        AnimatorControllerParameterType parameterType)
    {
        if (animatorToCheck == null ||
            string.IsNullOrEmpty(parameterName))
        {
            return false;
        }

        foreach (AnimatorControllerParameter parameter
                 in animatorToCheck.parameters)
        {
            if (parameter.name == parameterName &&
                parameter.type == parameterType)
            {
                return true;
            }
        }

        return false;
    }

    private void DisableUIControllerSubmit()
    {
        if (uiInputModule == null)
        {
            return;
        }

        uiInputModule.submit = null;
        uiInputModule.cancel = null;
    }
}