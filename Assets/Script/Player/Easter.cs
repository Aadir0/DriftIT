using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Easter : MonoBehaviour
{
    [Header("Easter Message UI (Optional)")]
    [SerializeField] private Text easterText;
    [SerializeField] private TMPro.TextMeshProUGUI easterTmpText;
    [SerializeField] private GameObject easterTextObject;
    [SerializeField] private string easterMessage = "You found the Easter!";

    [Header("Easter Stats Text UI (Sheep Hits & Drifts)")]
    [Tooltip("Standard UI Text component to display sheep hits and drifts.")]
    [SerializeField] private Text statsText;
    [Tooltip("TextMeshProUGUI component to display sheep hits and drifts.")]
    [SerializeField] private TMPro.TextMeshProUGUI statsTmpText;
    [Tooltip("Optional parent GameObject of stats text to activate upon triggering.")]
    [SerializeField] private GameObject statsTextObject;
    [Tooltip("Format for stats. Placeholders: {sheep} = sheep hits, {drifts} = drifts count.")]
    [TextArea(2, 4)]
    [SerializeField] private string statsFormat = "SHEEP HITS: {sheep}\nDRIFTS: {drifts}";

    [Header("Multi-Easter Tracking")]
    [Tooltip("Unique ID for this Easter trigger. If left empty, uses current scene name.")]
    [SerializeField] private string easterId = "";
    [Tooltip("Total number of unique Easters required across the run.")]
    [SerializeField] private int requiredEasterCount = 2;
    [Tooltip("Disable collider after being triggered once so it cannot be re-triggered.")]
    [SerializeField] private bool disableTriggerAfterFirstUse = true;

    [Header("Easter Rewards & Customization")]
    [Tooltip("Assign the new sprite here that replaces the player car sprite once both Easters are found.")]
    [SerializeField] private Sprite easterCarSprite;
    [Tooltip("Whether to stop/freeze the player car movement when all required Easters are found.")]
    [SerializeField] private bool freezePlayerOnCompletion = true;

    [Header("Auto-Hide Settings")]
    [Tooltip("Duration in seconds before Easter and Stats UI automatically hide.")]
    [SerializeField] private float autoHideDuration = 5f;
    [Tooltip("Whether to automatically hide stats/easter UI after the duration.")]
    [SerializeField] private bool autoHideAfterDuration = true;
    [Tooltip("Whether to unfreeze player movement when the UI auto-hides.")]
    [SerializeField] private bool unfreezeAfterAutoHide = true;

    // Static set tracking which Easters have been found across scenes during this session/run
    public static readonly HashSet<string> FoundEasters = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    public static bool IsEasterCompleted { get; set; } = false;

    private BoxCollider2D boxCollider;
    private bool hasTriggeredThisInstance = false;
    private Coroutine autoHideCoroutine;
    private GameObject lastTriggeredPlayer;

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

        AutoFindTextComponents();
        HideEasterUI();
        HideStatsUI();
    }

    private void Start()
    {
        AutoFindTextComponents();
        HideEasterUI();
        HideStatsUI();
    }

    private void AutoFindTextComponents()
    {
        // 1. Easter message text auto-find
        if (easterTextObject == null)
        {
            if (easterText != null) easterTextObject = easterText.gameObject;
            else if (easterTmpText != null) easterTextObject = easterTmpText.gameObject;
        }

        // 2. Stats text auto-find if not explicitly assigned
        if (statsTextObject == null)
        {
            if (statsText != null) statsTextObject = statsText.gameObject;
            else if (statsTmpText != null) statsTextObject = statsTmpText.gameObject;
        }

        if (easterText == null && easterTmpText == null && easterTextObject == null)
        {
            Canvas canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
            if (canvas != null)
            {
                Transform[] allChildren = canvas.GetComponentsInChildren<Transform>(true);
                foreach (var t in allChildren)
                {
                    string tName = t.gameObject.name.ToLower();
                    if (tName.Contains("easter") && !tName.Contains("stat"))
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

    private void ShowStatsUI()
    {
        string formattedStats = statsFormat
            .Replace("{sheep}", EasterStatsTracker.SheepHitCount.ToString())
            .Replace("{drifts}", EasterStatsTracker.DriftCount.ToString())
            .Replace("{hits}", EasterStatsTracker.SheepHitCount.ToString());

        if (statsTextObject != null)
        {
            statsTextObject.SetActive(true);
        }

        if (statsText != null)
        {
            statsText.gameObject.SetActive(true);
            statsText.enabled = true;
            statsText.text = formattedStats;
        }

        if (statsTmpText != null)
        {
            statsTmpText.gameObject.SetActive(true);
            statsTmpText.enabled = true;
            statsTmpText.text = formattedStats;
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

    private void HideStatsUI()
    {
        if (statsText != null)
        {
            statsText.text = "";
            statsText.enabled = false;
        }

        if (statsTmpText != null)
        {
            statsTmpText.text = "";
            statsTmpText.enabled = false;
        }

        if (statsTextObject != null)
        {
            statsTextObject.SetActive(false);
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

    private void OnDisable()
    {
        if (autoHideCoroutine != null)
        {
            StopCoroutine(autoHideCoroutine);
            autoHideCoroutine = null;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasTriggeredThisInstance) return;

        if (IsPlayerCollider(other, out GameObject playerRoot))
        {
            hasTriggeredThisInstance = true;
            lastTriggeredPlayer = playerRoot;

            ShowEasterUI();
            ShowStatsUI();
            HandleEasterTriggered(playerRoot);
            StartAutoHideTimer(playerRoot);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (IsPlayerCollider(other, out _))
        {
            HideEasterUI();

            if (!IsEasterCompleted)
            {
                HideStatsUI();
            }

            if (disableTriggerAfterFirstUse)
            {
                if (boxCollider != null)
                {
                    boxCollider.enabled = false;
                }
            }
            else
            {
                hasTriggeredThisInstance = false;
            }
        }
    }

    private void StartAutoHideTimer(GameObject playerObj = null)
    {
        if (!autoHideAfterDuration) return;

        if (autoHideCoroutine != null)
        {
            StopCoroutine(autoHideCoroutine);
        }
        autoHideCoroutine = StartCoroutine(AutoHideRoutine(playerObj ?? lastTriggeredPlayer));
    }

    private System.Collections.IEnumerator AutoHideRoutine(GameObject playerObj)
    {
        yield return new WaitForSeconds(autoHideDuration);

        HideEasterUI();
        HideStatsUI();

        if (unfreezeAfterAutoHide)
        {
            FreezePlayer(playerObj ?? lastTriggeredPlayer, false);
        }

        autoHideCoroutine = null;
    }

    private void HandleEasterTriggered(GameObject playerObj)
    {
        string currentId = string.IsNullOrWhiteSpace(easterId) ? SceneManager.GetActiveScene().name : easterId;
        FoundEasters.Add(currentId);

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
        lastTriggeredPlayer = playerObj;

        // 1. Stop / Freeze player movement
        if (freezePlayerOnCompletion)
        {
            FreezePlayer(playerObj, true);
        }

        // 2. Change car sprite
        if (easterCarSprite != null)
        {
            ApplyCarSprite(playerObj, easterCarSprite);
        }

        // 3. Update stats UI display
        ShowStatsUI();

        // 4. Start 5-second auto hide timer
        StartAutoHideTimer(playerObj);
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

    /// <summary>
    /// Resets Easter progress and stats (call when starting a new game / returning to MainMenu).
    /// </summary>
    public static void ResetEasterState()
    {
        FoundEasters.Clear();
        IsEasterCompleted = false;
        EasterStatsTracker.Reset();
    }
}