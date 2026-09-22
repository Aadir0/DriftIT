using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Controls the 2-Thumb Mobile Touch HUD for DriftIT.
/// Displays a Virtual Joystick on the bottom-left for Steering/Acceleration & Natural Drifting,
/// a Jump/Start Engine button on the bottom-right, and live stage timer / room code in the top bar.
/// </summary>
public class MobileHUDManager : MonoBehaviour
{
    public static MobileHUDManager Instance { get; private set; }

    [Header("Mobile HUD Canvas Root")]
    [SerializeField] private GameObject mobileHudCanvas;

    [Header("Top Bar UI References")]
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI roomCodeText;

    [Header("Mobile Controls")]
    [SerializeField] private Button actionButton; // Start Engine / Jump
    [SerializeField] private TextMeshProUGUI actionButtonLabel;

    [Header("Input System Action Trigger (Optional)")]
    [SerializeField] private InputActionReference jumpActionReference;

    [Header("HUD Visibility Options")]
    [SerializeField] private bool forceShowInEditor = true;
    [SerializeField] private bool autoDetectTouchDevice = true;

    private LevelTimer cachedLevelTimer;
    private CarControllerSingle cachedSingleCar;
    private NetworkCarController cachedNetworkCar;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        ConfigureHUDVisibility();
        HookActionButton();
        FindLevelReferences();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ConfigureHUDVisibility();
        FindLevelReferences();
    }

    /// <summary>
    /// Evaluates whether the mobile HUD should be displayed based on platform, touch input, and active scene.
    /// </summary>
    public void ConfigureHUDVisibility()
    {
        if (mobileHudCanvas == null) return;

        string sceneName = SceneManager.GetActiveScene().name;
        bool isGameplayScene = sceneName.StartsWith("Level", StringComparison.OrdinalIgnoreCase);

        if (!isGameplayScene)
        {
            mobileHudCanvas.SetActive(false);
            return;
        }

        bool shouldShow = false;

        if (forceShowInEditor && Application.isEditor)
        {
            shouldShow = true;
        }
        else if (autoDetectTouchDevice)
        {
            shouldShow = Application.isMobilePlatform || 
                         Input.touchSupported || 
                         SystemInfo.deviceType == DeviceType.Handheld;
        }

        mobileHudCanvas.SetActive(shouldShow);
    }

    private void HookActionButton()
    {
        if (actionButton != null)
        {
            actionButton.onClick.RemoveListener(OnActionButtonClicked);
            actionButton.onClick.AddListener(OnActionButtonClicked);
        }
    }

    private void FindLevelReferences()
    {
        cachedLevelTimer = UnityEngine.Object.FindFirstObjectByType<LevelTimer>();
        cachedSingleCar = UnityEngine.Object.FindFirstObjectByType<CarControllerSingle>();
        cachedNetworkCar = null;

        var networkCars = UnityEngine.Object.FindObjectsByType<NetworkCarController>(FindObjectsSortMode.None);
        foreach (var car in networkCars)
        {
            if (car.IsOwner)
            {
                cachedNetworkCar = car;
                break;
            }
        }
    }

    private void Update()
    {
        if (mobileHudCanvas == null || !mobileHudCanvas.activeSelf) return;

        UpdateTimerDisplay();
        UpdateRoomCodeDisplay();
    }

    private void UpdateTimerDisplay()
    {
        if (timerText == null) return;

        if (cachedLevelTimer == null)
        {
            cachedLevelTimer = UnityEngine.Object.FindFirstObjectByType<LevelTimer>();
        }

        if (cachedLevelTimer != null)
        {
            float elapsed = cachedLevelTimer.currentTime;
            int minutes = Mathf.FloorToInt(elapsed / 60f);
            int seconds = Mathf.FloorToInt(elapsed % 60f);
            int hundredths = Mathf.FloorToInt((elapsed * 100f) % 100f);
            timerText.text = $"{minutes:00}:{seconds:00}.{hundredths:00}";
        }
    }

    private void UpdateRoomCodeDisplay()
    {
        if (roomCodeText == null) return;

        string joinCode = RelayManager.Instance != null ? RelayManager.Instance.JoinCode : null;
        if (!string.IsNullOrEmpty(joinCode) && !string.Equals(joinCode, "LOCAL", StringComparison.OrdinalIgnoreCase))
        {
            roomCodeText.text = $"ROOM: {joinCode}";
            roomCodeText.gameObject.SetActive(true);
        }
        else
        {
            roomCodeText.text = "";
            roomCodeText.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Called when the Jump/Start Engine button is tapped on mobile.
    /// </summary>
    public void OnActionButtonClicked()
    {
        // 1. If car controller exists in single player, notify action
        if (cachedSingleCar == null)
        {
            cachedSingleCar = UnityEngine.Object.FindFirstObjectByType<CarControllerSingle>();
        }

        if (cachedSingleCar != null)
        {
            // Simulate spacebar / jump action in Unity New Input System
            if (jumpActionReference != null && jumpActionReference.action != null)
            {
                // Action reference is triggered by OnScreenButton component
            }
        }

        // 2. If in network multiplayer, notify local owner car
        if (cachedNetworkCar == null)
        {
            var networkCars = UnityEngine.Object.FindObjectsByType<NetworkCarController>(FindObjectsSortMode.None);
            foreach (var car in networkCars)
            {
                if (car.IsOwner)
                {
                    cachedNetworkCar = car;
                    break;
                }
            }
        }
    }
}
