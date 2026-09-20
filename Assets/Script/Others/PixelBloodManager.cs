using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Modular Pixel-Art Bloodstain & Particle Splash System.
/// Generates stylized low-resolution blood textures with hard pixel edges (Point filtering)
/// and manages pooled chunky pixel particles on sheep-car impacts.
/// </summary>
public class PixelBloodManager : MonoBehaviour
{
    private static PixelBloodManager instance;
    public static PixelBloodManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = Object.FindFirstObjectByType<PixelBloodManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("PixelBloodManager");
                    instance = go.AddComponent<PixelBloodManager>();
                    if (Application.isPlaying)
                    {
                        DontDestroyOnLoad(go);
                    }
                }
            }
            return instance;
        }
    }

    [Header("Default Pixel Blood Palette")]
    public Color[] defaultBloodPalette = new Color[]
    {
        new Color(0.40f, 0.04f, 0.04f, 1f), // Deep Clotted Maroon (#660A0A)
        new Color(0.60f, 0.06f, 0.06f, 1f), // Dark Crimson (#990F0F)
        new Color(0.75f, 0.10f, 0.10f, 1f), // Blood Red (#BF1A1A)
        new Color(0.85f, 0.15f, 0.15f, 1f), // Fresh Red Accent (#D92626)
        new Color(0.22f, 0.02f, 0.02f, 1f)  // Dark Shadow Edge (#380505)
    };

    [Header("Pixel Splash Settings")]
    [SerializeField] private int initialPoolSize = 30;
    [SerializeField] private float defaultSplashSpeed = 3.0f;
    [SerializeField] private float defaultSplashLifetime = 0.35f;

    // Sprite & Texture cache
    private Sprite singlePixelSprite;
    private readonly Queue<PixelParticle> particlePool = new Queue<PixelParticle>();
    private readonly List<PixelParticle> activeParticles = new List<PixelParticle>();

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }

        InitializeSinglePixelSprite();
        InitializePool();
    }

    private void InitializeSinglePixelSprite()
    {
        if (singlePixelSprite != null) return;

        Texture2D tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();

        singlePixelSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
    }

    private void InitializePool()
    {
        for (int i = 0; i < initialPoolSize; i++)
        {
            PixelParticle p = CreateNewParticle();
            p.gameObject.SetActive(false);
            particlePool.Enqueue(p);
        }
    }

    private PixelParticle CreateNewParticle()
    {
        GameObject go = new GameObject("PixelBloodParticle");
        go.transform.SetParent(transform);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = singlePixelSprite;
        sr.sortingLayerName = "Player";
        sr.sortingOrder = 60;

        PixelParticle p = go.AddComponent<PixelParticle>();
        p.Initialize(sr, ReturnParticleToPool);
        return p;
    }

    private void ReturnParticleToPool(PixelParticle particle)
    {
        if (particle == null) return;
        activeParticles.Remove(particle);
        particle.gameObject.SetActive(false);
        particlePool.Enqueue(particle);
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        for (int i = activeParticles.Count - 1; i >= 0; i--)
        {
            if (activeParticles[i] != null)
            {
                activeParticles[i].Tick(dt);
            }
        }
    }

    /// <summary>
    /// Spawns a burst of chunky pixel blood particles at the impact point.
    /// </summary>
    public void SpawnSplash(Vector2 worldPosition, Vector2 impactNormal, int count = 8, float speedMultiplier = 1f)
    {
        InitializeSinglePixelSprite();

        for (int i = 0; i < count; i++)
        {
            PixelParticle p;
            if (particlePool.Count > 0)
            {
                p = particlePool.Dequeue();
            }
            else
            {
                p = CreateNewParticle();
            }

            p.gameObject.SetActive(true);
            activeParticles.Add(p);

            Color color = defaultBloodPalette[Random.Range(0, defaultBloodPalette.Length)];
            
            // Chunky pixel scale
            float pixelSize = Random.Range(0.04f, 0.08f);
            
            // Ejection angle: biased towards impact normal with random spread
            Vector2 randomSpread = Random.insideUnitCircle.normalized;
            Vector2 dir = (impactNormal * 0.7f + randomSpread * 0.5f).normalized;
            float speed = defaultSplashSpeed * Random.Range(0.6f, 1.4f) * speedMultiplier;
            float lifetime = defaultSplashLifetime * Random.Range(0.7f, 1.2f);

            p.Launch(worldPosition, dir * speed, color, pixelSize, lifetime);
        }
    }

    /// <summary>
    /// Procedurally creates an irregular chunky pixel-art bloodstain Sprite.
    /// Uses FilterMode.Point for hard, retro pixel edges and 100 PPU to match car sprite pixel dimensions.
    /// </summary>
    public Sprite GeneratePixelStainSprite(Color[] palette, int minPixels = 2, int maxPixels = 4, int gridSize = 4, float pixelsPerUnit = 100f)
    {
        if (palette == null || palette.Length == 0)
        {
            palette = defaultBloodPalette;
        }

        Texture2D tex = new Texture2D(gridSize, gridSize, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;

        // Clear transparent
        Color clear = new Color(0, 0, 0, 0);
        Color[] clearColors = new Color[gridSize * gridSize];
        for (int i = 0; i < clearColors.Length; i++) clearColors[i] = clear;
        tex.SetPixels(clearColors);

        // Core start position (around center)
        int cx = gridSize / 2;
        int cy = gridSize / 2;

        int targetPixels = Random.Range(minPixels, maxPixels + 1);
        HashSet<Vector2Int> filled = new HashSet<Vector2Int>();
        Queue<Vector2Int> frontier = new Queue<Vector2Int>();

        Vector2Int start = new Vector2Int(cx, cy);
        filled.Add(start);
        frontier.Enqueue(start);

        Vector2Int[] neighbors = new Vector2Int[]
        {
            new Vector2Int(1, 0),
            new Vector2Int(-1, 0),
            new Vector2Int(0, 1),
            new Vector2Int(0, -1)
        };

        while (filled.Count < targetPixels && frontier.Count > 0)
        {
            Vector2Int current = frontier.Dequeue();

            for (int n = 0; n < neighbors.Length; n++)
            {
                int r = Random.Range(n, neighbors.Length);
                var temp = neighbors[n];
                neighbors[n] = neighbors[r];
                neighbors[r] = temp;
            }

            foreach (var offset in neighbors)
            {
                Vector2Int next = current + offset;
                if (next.x >= 0 && next.x < gridSize && next.y >= 0 && next.y < gridSize)
                {
                    if (!filled.Contains(next))
                    {
                        filled.Add(next);
                        frontier.Enqueue(next);
                        if (filled.Count >= targetPixels) break;
                    }
                }
            }
        }

        // Paint pixels with randomized shades from palette
        foreach (var pos in filled)
        {
            Color pixelColor = palette[Random.Range(0, palette.Length)];
            tex.SetPixel(pos.x, pos.y, pixelColor);
        }

        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, gridSize, gridSize), new Vector2(0.5f, 0.5f), pixelsPerUnit);
    }
}

/// <summary>
/// Helper class for updating individual pooled chunky pixel splash particles.
/// </summary>
public class PixelParticle : MonoBehaviour
{
    private SpriteRenderer sr;
    private System.Action<PixelParticle> onComplete;
    private Vector2 velocity;
    private float lifetime;
    private float maxLifetime;
    private Color initialColor;

    public void Initialize(SpriteRenderer renderer, System.Action<PixelParticle> returnCallback)
    {
        sr = renderer;
        onComplete = returnCallback;
    }

    public void Launch(Vector2 startPos, Vector2 initialVelocity, Color color, float size, float duration)
    {
        transform.position = startPos;
        transform.localScale = new Vector3(size, size, 1f);
        velocity = initialVelocity;
        initialColor = color;
        sr.color = color;
        lifetime = 0f;
        maxLifetime = Mathf.Max(0.05f, duration);
    }

    public void Tick(float dt)
    {
        lifetime += dt;
        if (lifetime >= maxLifetime)
        {
            onComplete?.Invoke(this);
            return;
        }

        // Move and apply slight linear drag
        transform.position += (Vector3)(velocity * dt);
        velocity = Vector2.MoveTowards(velocity, Vector2.zero, 6.0f * dt);

        // Alpha fade out in last 40% of lifetime
        float normalized = lifetime / maxLifetime;
        if (normalized > 0.6f)
        {
            float alpha = Mathf.Lerp(initialColor.a, 0f, (normalized - 0.6f) / 0.4f);
            sr.color = new Color(initialColor.r, initialColor.g, initialColor.b, alpha);
        }
    }
}

