using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class SheepController : MonoBehaviour
{
    public enum SheepState
    {
        Idle,
        Walking,
        Eating,
        Dead
    }

    [Header("State")]
    [SerializeField] private SheepState currentState = SheepState.Idle;

    [Header("Movement & Roam")]
    [SerializeField] private float walkSpeed = 1.2f;
    [SerializeField] private float wanderRadius = 4.0f;
    [SerializeField] private float minIdleDuration = 2.0f;
    [SerializeField] private float maxIdleDuration = 4.0f;
    [SerializeField] private float minWalkDuration = 2.0f;
    [SerializeField] private float maxWalkDuration = 4.5f;
    [SerializeField] private float minEatDuration = 2.5f;
    [SerializeField] private float maxEatDuration = 5.0f;
    [Range(0f, 1f)]
    [SerializeField] private float eatProbability = 0.45f;

    [Header("Ground Layer Restriction")]
    [Tooltip("Layer mask representing valid walkable ground (defaults to Ground layer).")]
    [SerializeField] private LayerMask groundLayerMask = (1 << 7) | (1 << 0);
    [Tooltip("Layer mask representing hazards, holes, and boundaries to avoid.")]
    [SerializeField] private LayerMask obstacleLayerMask = (1 << 3) | (1 << 4);

    [Header("Effects & Blood")]
    [SerializeField] private GameObject bloodPrefab;
    [SerializeField] private GameObject hitEffectPrefab;
    [SerializeField] private AudioClip hitSound;
    [SerializeField] private bool createPixelBloodstain = true;

    [Header("Visuals")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Animator animator;
    [SerializeField] private bool flipXFacingLeft = true;

    private Rigidbody2D rb;
    private Collider2D col;
    private Vector2 originPosition;
    private Vector2 targetPosition;
    private float stateTimer;
    private Tilemap[] groundTilemaps;

    private static readonly int IsWalkingHash = Animator.StringToHash("isWalking");
    private static readonly int IsEatingHash = Animator.StringToHash("isEating");
    private static readonly int IsIdleHash = Animator.StringToHash("isIdle");

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        spriteRenderer ??= GetComponent<SpriteRenderer>() ?? GetComponentInChildren<SpriteRenderer>();
        animator ??= GetComponent<Animator>() ?? GetComponentInChildren<Animator>();

        originPosition = transform.position;

        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
        }

        if (bloodPrefab == null)
        {
            bloodPrefab = Resources.Load<GameObject>("DeathMark");
        }

        FindGroundTilemaps();
    }

    private void Start()
    {
        if (groundTilemaps == null || groundTilemaps.Length == 0)
        {
            FindGroundTilemaps();
        }

        EnterIdleState();
    }

    private void FindGroundTilemaps()
    {
        Tilemap[] allTilemaps = Object.FindObjectsByType<Tilemap>(FindObjectsSortMode.None);
        List<Tilemap> validGround = new List<Tilemap>();
        if (allTilemaps != null)
        {
            foreach (var tm in allTilemaps)
            {
                if (tm == null) continue;
                string tmName = tm.gameObject.name.ToLower();
                int tmLayer = tm.gameObject.layer;
                if (tmLayer == LayerMask.NameToLayer("Ground") || tm.CompareTag("Ground") || tmName.Contains("ground") || tmName.Contains("grass"))
                {
                    validGround.Add(tm);
                }
            }
        }
        groundTilemaps = validGround.ToArray();
    }

    /// <summary>
    /// Checks if a world position is strictly on the Ground layer and not on holes/traps/obstacles.
    /// </summary>
    public bool IsPositionOnGround(Vector2 worldPos)
    {
        // 1. Check if on a valid Ground tilemap if available
        if (groundTilemaps != null && groundTilemaps.Length > 0)
        {
            bool hasGroundTile = false;
            foreach (var tm in groundTilemaps)
            {
                if (tm != null && tm.isActiveAndEnabled)
                {
                    Vector3Int cellPos = tm.WorldToCell(worldPos);
                    if (tm.HasTile(cellPos))
                    {
                        hasGroundTile = true;
                        break;
                    }
                }
            }
            if (!hasGroundTile) return false;
        }

        // 2. Check for hazard/obstacle overlaps (Hole, Water, Trap, Boundary)
        Collider2D hitObstacle = Physics2D.OverlapCircle(worldPos, 0.2f, obstacleLayerMask);
        if (hitObstacle != null) return false;

        // 3. Check for trap/boundary tags on overlapping colliders
        Collider2D[] allHits = Physics2D.OverlapCircleAll(worldPos, 0.2f);
        if (allHits != null)
        {
            foreach (var h in allHits)
            {
                if (h == null || h == col) continue;
                if (h.CompareTag("Trap") || h.CompareTag("Hole") || h.CompareTag("Boundary"))
                {
                    return false;
                }
            }
        }

        return true;
    }

    private void Update()
    {
        if (currentState == SheepState.Dead) return;

        stateTimer -= Time.deltaTime;

        switch (currentState)
        {
            case SheepState.Walking:
                UpdateWalkingState();
                break;
            case SheepState.Idle:
                UpdateIdleState();
                break;
            case SheepState.Eating:
                UpdateEatingState();
                break;
        }
    }

    private void UpdateWalkingState()
    {
        Vector2 currentPos = transform.position;
        Vector2 stepTarget = Vector2.MoveTowards(currentPos, targetPosition, walkSpeed * Time.deltaTime);

        // Ensure next step remains strictly on the Ground layer
        if (!IsPositionOnGround(stepTarget))
        {
            // Reached edge of ground or approaching obstacle -> stop and enter Idle
            EnterIdleState();
            return;
        }

        transform.position = stepTarget;

        Vector2 moveDir = (targetPosition - currentPos);
        if (moveDir.sqrMagnitude > 0.001f && spriteRenderer != null)
        {
            bool movingLeft = moveDir.x < 0f;
            spriteRenderer.flipX = flipXFacingLeft ? movingLeft : !movingLeft;
        }

        if (Vector2.Distance(currentPos, targetPosition) < 0.1f || stateTimer <= 0f)
        {
            if (Random.value < eatProbability)
                EnterEatingState();
            else
                EnterIdleState();
        }
    }

    private void UpdateIdleState()
    {
        if (stateTimer <= 0f)
        {
            EnterWalkingState();
        }
    }

    private void UpdateEatingState()
    {
        if (stateTimer <= 0f)
        {
            EnterWalkingState();
        }
    }

    private void EnterIdleState()
    {
        currentState = SheepState.Idle;
        stateTimer = Random.Range(minIdleDuration, maxIdleDuration);
        SetAnimationState(isIdle: true, isWalking: false, isEating: false);
    }

    private void EnterWalkingState()
    {
        currentState = SheepState.Walking;
        stateTimer = Random.Range(minWalkDuration, maxWalkDuration);

        // Find a valid target point strictly on Ground layer within wanderRadius
        bool foundValidGround = false;
        Vector2 candidate = originPosition;

        for (int attempts = 0; attempts < 10; attempts++)
        {
            Vector2 randomOffset = Random.insideUnitCircle * wanderRadius;
            candidate = originPosition + randomOffset;

            if (IsPositionOnGround(candidate))
            {
                foundValidGround = true;
                break;
            }
        }

        if (foundValidGround)
        {
            targetPosition = candidate;
            SetAnimationState(isIdle: false, isWalking: true, isEating: false);
        }
        else
        {
            // If no valid target found, stay idle
            EnterIdleState();
        }
    }

    private void EnterEatingState()
    {
        currentState = SheepState.Eating;
        stateTimer = Random.Range(minEatDuration, maxEatDuration);
        SetAnimationState(isIdle: false, isWalking: false, isEating: true);
    }

    private void SetAnimationState(bool isIdle, bool isWalking, bool isEating)
    {
        if (animator == null || !animator.enabled) return;

        try
        {
            animator.SetBool(IsIdleHash, isIdle);
            animator.SetBool(IsWalkingHash, isWalking);
            animator.SetBool(IsEatingHash, isEating);
        }
        catch { }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        Vector2 contactPoint = collision.contactCount > 0 ? collision.GetContact(0).point : (Vector2)transform.position;
        Vector2 contactNormal = collision.contactCount > 0 ? collision.GetContact(0).normal : (Vector2)(transform.position - collision.transform.position).normalized;
        
        HandleCarImpact(collision.gameObject, contactPoint, contactNormal);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Vector2 contactPoint = col != null ? col.ClosestPoint(other.transform.position) : (Vector2)transform.position;
        Vector2 contactNormal = ((Vector2)transform.position - contactPoint).normalized;
        if (contactNormal == Vector2.zero) contactNormal = Vector2.up;

        HandleCarImpact(other.gameObject, contactPoint, contactNormal);
    }

    private void HandleCarImpact(GameObject hitObj, Vector2 contactPoint, Vector2 contactNormal)
    {
        if (currentState == SheepState.Dead) return;

        NetworkCarController netCar = hitObj.GetComponentInParent<NetworkCarController>();
        CarControllerSingle singleCar = hitObj.GetComponentInParent<CarControllerSingle>();

        if (netCar == null && singleCar == null)
        {
            CarHealth health = hitObj.GetComponentInParent<CarHealth>();
            if (health != null)
            {
                netCar = health.GetComponent<NetworkCarController>();
                singleCar = health.GetComponent<CarControllerSingle>();
            }
        }

        bool isPlayer = hitObj.CompareTag("Player") || netCar != null || singleCar != null;
        if (!isPlayer) return;

        // 1. Calculate impact velocity
        float impactSpeed = 2.0f;
        Rigidbody2D hitRb = hitObj.GetComponentInParent<Rigidbody2D>();
        if (hitRb != null)
        {
            impactSpeed = hitRb.linearVelocity.magnitude;
        }

        // 2. Slow debuffs
        if (singleCar != null)
        {
            singleCar.ApplySheepSlowDebuff(1.0f, 0.45f);
        }

        if (netCar != null && netCar.IsOwner)
        {
            netCar.ApplySheepSlowDebuff(1.0f, 0.45f);
            netCar.SyncSheepKnockedRpc(gameObject.name, transform.position, hitObj.transform.position, Vector2.zero);
        }

        // 3. Pixel Bloodstain System: Attach stain to car & sheep, spawn chunky pixel splash
        if (createPixelBloodstain)
        {
            ApplyPixelBloodEffects(hitObj, contactPoint, contactNormal, impactSpeed);
        }

        Die();
    }

    private void ApplyPixelBloodEffects(GameObject hitCarObj, Vector2 contactPoint, Vector2 contactNormal, float impactSpeed)
    {
        // 1. Add subtle pixel bloodstain to car
        CarBloodStainReceiver receiver = hitCarObj.GetComponentInParent<CarBloodStainReceiver>();
        if (receiver == null)
        {
            receiver = hitCarObj.GetComponent<CarBloodStainReceiver>() ?? hitCarObj.AddComponent<CarBloodStainReceiver>();
        }

        if (receiver != null)
        {
            receiver.AddBloodStain(contactPoint, contactNormal, impactSpeed);
        }

        // 2. Add matching pixel bloodstain to sheep
        AttachStainToSheep(contactPoint);
    }

    private void AttachStainToSheep(Vector2 worldContactPoint)
    {
        Sprite stainSprite = PixelBloodManager.Instance.GeneratePixelStainSprite(
            null,
            minPixels: 4,
            maxPixels: 7,
            gridSize: 6,
            pixelsPerUnit: 32f // Chunky visible pixel splatter on sheep
        );

        if (stainSprite == null) return;

        GameObject stainObj = new GameObject("SheepBloodStain");
        stainObj.transform.SetParent(transform, false);

        stainObj.transform.localPosition = new Vector3(0f, 0.02f, -0.05f);
        stainObj.transform.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
        stainObj.transform.localScale = Vector3.one * 1.2f;

        SpriteRenderer sr = stainObj.AddComponent<SpriteRenderer>();
        sr.sprite = stainSprite;
        sr.sortingLayerName = "Player";
        sr.sortingOrder = 15;
    }

    public void KnockFromCarHit(Vector3 carPosition, Vector2 carVelocity)
    {
        Die();
    }

    private void Die()
    {
        if (currentState == SheepState.Dead) return;
        currentState = SheepState.Dead;

        if (col != null) col.enabled = false;
        if (animator != null) animator.enabled = false;
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        // 1. Spawn ground blood puddle with spreading animation
        GameObject prefabToSpawn = bloodPrefab ?? hitEffectPrefab;
        if (prefabToSpawn == null)
        {
            prefabToSpawn = Resources.Load<GameObject>("DeathMark");
        }

        if (prefabToSpawn != null)
        {
            GameObject blood = Instantiate(prefabToSpawn, transform.position, Quaternion.identity);
            if (!blood.TryGetComponent<GroundBloodSpreader>(out _))
            {
                blood.AddComponent<GroundBloodSpreader>();
            }
        }

        // 2. Play hit sound
        if (hitSound != null)
        {
            float vol = AudioManager.Instance != null ? AudioManager.Instance.GetSfxVolume() : 1.0f;
            AudioSource.PlayClipAtPoint(hitSound, transform.position, vol);
        }

        // 3. Lie down on side
        float tiltAngle = (spriteRenderer != null && spriteRenderer.flipX) ? -90f : 90f;
        transform.rotation = Quaternion.Euler(0f, 0f, tiltAngle);

        // 4. Lie for 2.5s then fade out and disappear
        StartCoroutine(LieAndDisappearRoutine());
    }

    private IEnumerator LieAndDisappearRoutine()
    {
        // Lie there with blood prefab for 2.5 seconds
        yield return new WaitForSeconds(2.5f);

        // Smooth fade out
        if (spriteRenderer != null)
        {
            Color startColor = spriteRenderer.color;
            float fadeDuration = 0.5f;
            float elapsed = 0f;

            // Also fade child stains on the sheep
            SpriteRenderer[] allRenderers = GetComponentsInChildren<SpriteRenderer>();

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);

                foreach (var r in allRenderers)
                {
                    if (r != null)
                    {
                        Color c = r.color;
                        r.color = new Color(c.r, c.g, c.b, alpha);
                    }
                }

                yield return null;
            }
        }

        Destroy(gameObject);
    }
}