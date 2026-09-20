using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Component attached to the Car to receive, attach, and manage persistent pixel-art bloodstains.
/// The bloodstains are child overlays that translate, rotate, and drift accurately with the car.
/// </summary>
public class CarBloodStainReceiver : MonoBehaviour
{
    [Header("Blood Palette Configuration")]
    [Tooltip("Custom blood palette for pixel stains. Leave empty to use PixelBloodManager default.")]
    [SerializeField] private Color[] bloodPalette;

    [Header("Stain Size & Density")]
    [SerializeField] private int minPixels = 2;
    [SerializeField] private int maxPixels = 4;
    [SerializeField] private float stainScale = 1.0f;

    [Header("Lifetime & Limits")]
    [Tooltip("Stain lifetime in seconds. Set to 0 for persistent stains throughout the level.")]
    [SerializeField] private float stainLifetime = 0f;
    [Tooltip("Maximum simultaneous stains allowed on this car to prevent unbounded memory growth.")]
    [SerializeField] private int maxStainsPerCar = 8;
    [SerializeField] private float minImpactSpeed = 0.2f;

    // Active stain tracking (FIFO)
    private readonly Queue<GameObject> activeStains = new Queue<GameObject>();
    private SpriteRenderer carSpriteRenderer;

    private void Awake()
    {
        carSpriteRenderer = GetComponent<SpriteRenderer>() ?? GetComponentInChildren<SpriteRenderer>();
    }

    private void OnEnable()
    {
        ClearAllStains();
    }

    /// <summary>
    /// Attaches a new procedural pixel-art bloodstain onto the car at the contact location.
    /// </summary>
    public void AddBloodStain(Vector2 worldContactPoint, Vector2 contactNormal, float impactSpeed = 1f)
    {
        if (impactSpeed < minImpactSpeed && impactSpeed > 0f) return;

        if (carSpriteRenderer == null)
        {
            carSpriteRenderer = GetComponent<SpriteRenderer>() ?? GetComponentInChildren<SpriteRenderer>();
        }

        // 1. Maintain max stains buffer (FIFO)
        while (activeStains.Count >= maxStainsPerCar)
        {
            GameObject oldest = activeStains.Dequeue();
            if (oldest != null)
            {
                Destroy(oldest);
            }
        }

        // 2. Generate procedural pixel stain sprite
        Sprite stainSprite = PixelBloodManager.Instance.GeneratePixelStainSprite(
            bloodPalette,
            minPixels,
            maxPixels,
            gridSize: 4
        );

        if (stainSprite == null) return;

        // 3. Create child GameObject attached to this car
        GameObject stainObj = new GameObject($"BloodStain_{activeStains.Count + 1}");
        stainObj.transform.SetParent(transform, false);

        // Convert world contact point to car's local coordinates
        Vector3 localPos = transform.InverseTransformPoint(worldContactPoint);
        localPos.z = -0.01f; // Slightly forward in local z to avoid Z-fighting
        stainObj.transform.localPosition = localPos;

        // Random rotation with slight bias towards impact normal
        float randomAngle = Random.Range(0f, 360f);
        stainObj.transform.localRotation = Quaternion.Euler(0f, 0f, randomAngle);
        stainObj.transform.localScale = Vector3.one * stainScale;

        // 4. Setup SpriteRenderer
        SpriteRenderer sr = stainObj.AddComponent<SpriteRenderer>();
        sr.sprite = stainSprite;
        
        if (carSpriteRenderer != null)
        {
            sr.sortingLayerID = carSpriteRenderer.sortingLayerID;
            sr.sortingOrder = carSpriteRenderer.sortingOrder + 1;
        }
        else
        {
            sr.sortingLayerName = "Player";
            sr.sortingOrder = 2;
        }

        activeStains.Enqueue(stainObj);

        // 5. Optional lifetime fade
        if (stainLifetime > 0f)
        {
            StartCoroutine(FadeAndDestroyStain(stainObj, sr, stainLifetime));
        }
    }

    private IEnumerator FadeAndDestroyStain(GameObject stainObj, SpriteRenderer sr, float duration)
    {
        yield return new WaitForSeconds(duration);

        if (stainObj == null || sr == null) yield break;

        float fadeDuration = 1.0f;
        float elapsed = 0f;
        Color startColor = sr.color;

        while (elapsed < fadeDuration && sr != null)
        {
            elapsed += Time.deltaTime;
            float a = Mathf.Lerp(startColor.a, 0f, elapsed / fadeDuration);
            sr.color = new Color(startColor.r, startColor.g, startColor.b, a);
            yield return null;
        }

        if (stainObj != null)
        {
            Destroy(stainObj);
        }
    }

    public void ClearAllStains()
    {
        while (activeStains.Count > 0)
        {
            GameObject obj = activeStains.Dequeue();
            if (obj != null) Destroy(obj);
        }
    }

    private void OnDestroy()
    {
        ClearAllStains();
    }
}

