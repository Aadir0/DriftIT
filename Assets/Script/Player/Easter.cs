using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Easter : MonoBehaviour
{
    [Header("UI Text References")]
    [SerializeField] private Text easterText;
    [SerializeField] private TMPro.TextMeshProUGUI easterTmpText;
    [SerializeField] private GameObject easterTextObject;

    [Header("Message Settings")]
    [SerializeField] private string easterMessage = "You found the Easter!";

    [Header("Multi-Easter Tracking")]
    [Tooltip("Unique ID for this Easter trigger. If left empty, uses current scene name.")]
    [SerializeField] private string easterId = "";
    [Tooltip("Total number of unique Easters that must be found to trigger completion.")]
    [SerializeField] private int requiredEasterCount = 2;

    [Header("Easter Reward & Car Customization")]
    [Tooltip("Assign the new sprite here that replaces the player car sprite once both Easters are found.")]
    [SerializeField] private Sprite easterCarSprite;
    [Tooltip("Whether to stop/freeze the player car movement when the final Easter is triggered.")]
    [SerializeField] private bool freezePlayerMovementOnCompletion = true;
    [Tooltip("Whether to display the stats popup showing Sheep Hits and Drifts.")]
    [SerializeField] private bool showStatsPopupOnCompletion = true;
    [Tooltip("Whether dismissing the popup restores player movement.")]
    [SerializeField] private bool unfreezeOnDismissPopup = true;

    // Static set tracking which Easters have been found across scenes during this session/run
    public static readonly HashSet<string> FoundEasters = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    public static bool IsEasterCompleted { get; set; } = false;

    private BoxCollider2D boxCollider;
    private static GameObject activePopupCanvasObject;
    private static bool isPopupOpen = false;
    private static Easter lastTriggeredEasterInstance;

    private void Awake()
    {
        boxCollider = GetComponent<BoxCollider2D>();
        if (boxCollider == null)
        {
            boxCollider = gameObject.AddComponent<BoxCollider2D>();
        }
        boxCollider.isTrigger = true;

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.simulated = true;
        }

        if (string.IsNullOrWhiteSpace(easterId))
        {
            easterId = SceneManager.GetActiveScene().name;
        }

        AutoFindTextComponent();
        HideEasterUI();
    }

    private void Start()
    {
        AutoFindTextComponent();
        HideEasterUI();
    }

    private void Update()
    {
        if (isPopupOpen)
        {
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Return))
            {
                DismissEasterPopup();
            }
        }
    }

    private void AutoFindTextComponent()
    {
        if (easterTextObject == null)
        {
            if (easterText != null) easterTextObject = easterText.gameObject;
            else if (easterTmpText != null) easterTextObject = easterTmpText.gameObject;
        }

        if (easterText == null && easterTmpText == null && easterTextObject == null)
        {
            Canvas canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
            if (canvas != null)
            {
                Transform[] allChildren = canvas.GetComponentsInChildren<Transform>(true);
                foreach (var t in allChildren)
                {
                    if (t.gameObject.name.ToLower().Contains("easter"))
                    {
                        easterTextObject = t.gameObject;
                        easterText = t.GetComponent<Text>();
                        easterTmpText = t.GetComponent<TMPro.TextMeshProUGUI>();
                        break;
                    }
                }
            }
        }
        else if (easterTextObject != null)
        {
            if (easterText == null) easterText = easterTextObject.GetComponent<Text>();
            if (easterTmpText == null) easterTmpText = easterTextObject.GetComponent<TMPro.TextMeshProUGUI>();
        }
    }

    private void ShowEasterUI()
    {
        if (easterTextObject != null)
        {
            easterTextObject.SetActive(true);
        }

        if (easterText != null)
        {
            easterText.gameObject.SetActive(true);
            easterText.enabled = true;
            easterText.text = easterMessage;
        }

        if (easterTmpText != null)
        {
            easterTmpText.gameObject.SetActive(true);
            easterTmpText.enabled = true;
            easterTmpText.text = easterMessage;
        }
    }

    private void HideEasterUI()
    {
        if (easterText != null)
        {
            easterText.text = "";
            easterText.enabled = false;
        }

        if (easterTmpText != null)
        {
            easterTmpText.text = "";
            easterTmpText.enabled = false;
        }

        if (easterTextObject != null)
        {
            easterTextObject.SetActive(false);
        }
    }

    private bool IsPlayerCollider(Collider2D other, out GameObject playerRoot)
    {
        playerRoot = null;
        if (other == null) return false;

        if (other.CompareTag("Player"))
        {
            playerRoot = other.gameObject;
            return true;
        }

        if (other.transform.root != null && other.transform.root.CompareTag("Player"))
        {
            playerRoot = other.transform.root.gameObject;
            return true;
        }

        CarControllerSingle singleCar = other.GetComponentInParent<CarControllerSingle>();
        if (singleCar != null)
        {
            playerRoot = singleCar.gameObject;
            return true;
        }

        NetworkCarController netCar = other.GetComponentInParent<NetworkCarController>();
        if (netCar != null)
        {
            playerRoot = netCar.gameObject;
            return true;
        }

        return false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (IsPlayerCollider(other, out GameObject playerRoot))
        {
            ShowEasterUI();
            HandleEasterTriggered(playerRoot);
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (IsPlayerCollider(other, out _))
        {
            ShowEasterUI();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (IsPlayerCollider(other, out _))
        {
            HideEasterUI();
        }
    }

    private void HandleEasterTriggered(GameObject playerObj)
    {
        string currentId = string.IsNullOrWhiteSpace(easterId) ? SceneManager.GetActiveScene().name : easterId;
        FoundEasters.Add(currentId);
        lastTriggeredEasterInstance = this;

        Debug.Log($"[Easter] Triggered: '{currentId}'. Total found: {FoundEasters.Count}/{requiredEasterCount}");

        if (FoundEasters.Count >= requiredEasterCount && !IsEasterCompleted)
        {
            IsEasterCompleted = true;
            ExecuteEasterCompletion(playerObj);
        }
    }

    private void ExecuteEasterCompletion(GameObject playerObj)
    {
        Debug.Log("[Easter] All Easters found! Executing completion event...");

        // 1. Freeze player movement
        if (freezePlayerMovementOnCompletion)
        {
            FreezePlayer(playerObj, true);
        }

        // 2. Change car sprite
        if (easterCarSprite != null)
        {
            ApplyCarSprite(playerObj, easterCarSprite);
        }
        else
        {
            Debug.Log("[Easter] No easterCarSprite assigned in Inspector. Keeping current sprite.");
        }

        // 3. Show Stats Popup Dialog
        if (showStatsPopupOnCompletion)
        {
            ShowEasterStatsDialog();
        }
    }

    private static void FreezePlayer(GameObject playerObj, bool freeze)
    {
        if (playerObj != null)
        {
            CarControllerSingle singleCar = playerObj.GetComponentInChildren<CarControllerSingle>();
            if (singleCar != null) singleCar.FreezeMovement(freeze);

            NetworkCarController netCar = playerObj.GetComponentInChildren<NetworkCarController>();
            if (netCar != null) netCar.FreezeMovement(freeze);

            Rigidbody2D rb = playerObj.GetComponentInChildren<Rigidbody2D>();
            if (rb != null && freeze)
            {
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }
        }
        else
        {
            CarControllerSingle[] allSingle = UnityEngine.Object.FindObjectsByType<CarControllerSingle>(FindObjectsSortMode.None);
            foreach (var c in allSingle) c.FreezeMovement(freeze);

            NetworkCarController[] allNet = UnityEngine.Object.FindObjectsByType<NetworkCarController>(FindObjectsSortMode.None);
            foreach (var c in allNet) c.FreezeMovement(freeze);
        }
    }

    private static void ApplyCarSprite(GameObject playerObj, Sprite newSprite)
    {
        if (newSprite == null) return;

        if (playerObj != null)
        {
            CarControllerSingle singleCar = playerObj.GetComponentInChildren<CarControllerSingle>();
            if (singleCar != null) singleCar.SetCarSprite(newSprite);

            NetworkCarController netCar = playerObj.GetComponentInChildren<NetworkCarController>();
            if (netCar != null) netCar.SetCarSprite(newSprite);

            SpriteRenderer sr = playerObj.GetComponent<SpriteRenderer>() ?? playerObj.GetComponentInChildren<SpriteRenderer>();
            if (sr != null) sr.sprite = newSprite;
        }
        else
        {
            CarControllerSingle[] allSingle = UnityEngine.Object.FindObjectsByType<CarControllerSingle>(FindObjectsSortMode.None);
            foreach (var c in allSingle) c.SetCarSprite(newSprite);

            NetworkCarController[] allNet = UnityEngine.Object.FindObjectsByType<NetworkCarController>(FindObjectsSortMode.None);
            foreach (var c in allNet) c.SetCarSprite(newSprite);
        }
    }

    public static void ShowEasterStatsDialog()
    {
        if (activePopupCanvasObject != null)
        {
            UnityEngine.Object.Destroy(activePopupCanvasObject);
        }

        isPopupOpen = true;

        int sheepHits = EasterStatsTracker.SheepHitCount;
        int drifts = EasterStatsTracker.DriftCount;

        // 1. Root Canvas
        activePopupCanvasObject = new GameObject("EasterStatsPopupCanvas");
        Canvas canvas = activePopupCanvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999;

        CanvasScaler scaler = activePopupCanvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        activePopupCanvasObject.AddComponent<GraphicRaycaster>();

        // 2. Dim Overlay Background
        GameObject dimObj = new GameObject("DimBackground", typeof(RectTransform), typeof(Image));
        dimObj.transform.SetParent(activePopupCanvasObject.transform, false);
        RectTransform dimRect = dimObj.GetComponent<RectTransform>();
        dimRect.anchorMin = Vector2.zero;
        dimRect.anchorMax = Vector2.one;
        dimRect.sizeDelta = Vector2.zero;
        Image dimImg = dimObj.GetComponent<Image>();
        dimImg.color = new Color(0f, 0f, 0f, 0.75f);

        // 3. Wooden Modal Box
        GameObject modalObj = new GameObject("ModalPanel", typeof(RectTransform), typeof(Image));
        modalObj.transform.SetParent(activePopupCanvasObject.transform, false);
        RectTransform modalRect = modalObj.GetComponent<RectTransform>();
        modalRect.sizeDelta = new Vector2(560, 420);
        modalRect.anchoredPosition = Vector2.zero;
        Image modalImg = modalObj.GetComponent<Image>();
        modalImg.color = new Color(0.42f, 0.22f, 0.18f, 0.98f); // Rich wood brown tone

        // Outline / Border on Modal
        Outline outline = modalObj.AddComponent<Outline>();
        outline.effectColor = new Color(0.22f, 0.09f, 0.08f, 1f);
        outline.effectDistance = new Vector2(5, -5);

        // 4. Modal Header Title
        GameObject titleObj = new GameObject("TitleText", typeof(RectTransform), typeof(TMPro.TextMeshProUGUI));
        titleObj.transform.SetParent(modalObj.transform, false);
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0, -25);
        titleRect.sizeDelta = new Vector2(-40, 50);
        TMPro.TextMeshProUGUI titleTmp = titleObj.GetComponent<TMPro.TextMeshProUGUI>();
        titleTmp.text = "★ ALL EASTERS FOUND! ★";
        titleTmp.fontSize = 38;
        titleTmp.alignment = TMPro.TextAlignmentOptions.Center;
        titleTmp.color = new Color(1f, 0.84f, 0f); // Gold
        titleTmp.enableWordWrapping = false;

        // Try load monogram SDF font if present
        TMPro.TMP_FontAsset fontAsset = Resources.Load<TMPro.TMP_FontAsset>("monogram SDF");
        if (fontAsset != null) titleTmp.font = fontAsset;

        // 5. Divider Line
        GameObject divObj = new GameObject("Divider", typeof(RectTransform), typeof(Image));
        divObj.transform.SetParent(modalObj.transform, false);
        RectTransform divRect = divObj.GetComponent<RectTransform>();
        divRect.anchorMin = new Vector2(0.1f, 1f);
        divRect.anchorMax = new Vector2(0.9f, 1f);
        divRect.pivot = new Vector2(0.5f, 1f);
        divRect.anchoredPosition = new Vector2(0, -80);
        divRect.sizeDelta = new Vector2(0, 3);
        Image divImg = divObj.GetComponent<Image>();
        divImg.color = new Color(0.22f, 0.09f, 0.08f, 1f);

        // 6. Stats Content Box
        GameObject statsObj = new GameObject("StatsContent", typeof(RectTransform), typeof(TMPro.TextMeshProUGUI));
        statsObj.transform.SetParent(modalObj.transform, false);
        RectTransform statsRect = statsObj.GetComponent<RectTransform>();
        statsRect.anchorMin = new Vector2(0f, 0.32f);
        statsRect.anchorMax = new Vector2(1f, 0.82f);
        statsRect.anchoredPosition = Vector2.zero;
        statsRect.sizeDelta = new Vector2(-50, 0);
        TMPro.TextMeshProUGUI statsTmp = statsObj.GetComponent<TMPro.TextMeshProUGUI>();
        statsTmp.fontSize = 32;
        statsTmp.lineSpacing = 20;
        statsTmp.alignment = TMPro.TextAlignmentOptions.Center;
        statsTmp.text = $"SHEEP HIT : <color=#FF6B81><b>{sheepHits}</b></color>\n\nDRIFTS : <color=#00FFA3><b>{drifts}</b></color>";
        if (fontAsset != null) statsTmp.font = fontAsset;

        // 7. Continue Button
        GameObject btnObj = new GameObject("ContinueButton", typeof(RectTransform), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(modalObj.transform, false);
        RectTransform btnRect = btnObj.GetComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0.5f, 0f);
        btnRect.anchorMax = new Vector2(0.5f, 0f);
        btnRect.pivot = new Vector2(0.5f, 0f);
        btnRect.anchoredPosition = new Vector2(0, 24);
        btnRect.sizeDelta = new Vector2(260, 52);

        Image btnImg = btnObj.GetComponent<Image>();
        btnImg.color = new Color(0.35f, 0.16f, 0.13f, 1f);

        Outline btnOutline = btnObj.AddComponent<Outline>();
        btnOutline.effectColor = new Color(0.18f, 0.07f, 0.06f, 1f);
        btnOutline.effectDistance = new Vector2(3, -3);

        Button btn = btnObj.GetComponent<Button>();
        btn.onClick.AddListener(DismissEasterPopup);

        // Button Text
        GameObject btnTextObj = new GameObject("BtnText", typeof(RectTransform), typeof(TMPro.TextMeshProUGUI));
        btnTextObj.transform.SetParent(btnObj.transform, false);
        RectTransform btnTextRect = btnTextObj.GetComponent<RectTransform>();
        btnTextRect.anchorMin = Vector2.zero;
        btnTextRect.anchorMax = Vector2.one;
        btnTextRect.sizeDelta = Vector2.zero;
        TMPro.TextMeshProUGUI btnTmp = btnTextObj.GetComponent<TMPro.TextMeshProUGUI>();
        btnTmp.text = "CONTINUE [SPACE]";
        btnTmp.fontSize = 26;
        btnTmp.alignment = TMPro.TextAlignmentOptions.Center;
        btnTmp.color = Color.white;
        if (fontAsset != null) btnTmp.font = fontAsset;
    }

    public static void DismissEasterPopup()
    {
        isPopupOpen = false;

        if (activePopupCanvasObject != null)
        {
            UnityEngine.Object.Destroy(activePopupCanvasObject);
            activePopupCanvasObject = null;
        }

        if (lastTriggeredEasterInstance != null && lastTriggeredEasterInstance.unfreezeOnDismissPopup)
        {
            FreezePlayer(null, false);
        }
    }

    /// <summary>
    /// Resets Easter progress and stats (call when starting a new game / returning to MainMenu).
    /// </summary>
    public static void ResetEasterState()
    {
        FoundEasters.Clear();
        IsEasterCompleted = false;
        DismissEasterPopup();
        EasterStatsTracker.Reset();
    }
}