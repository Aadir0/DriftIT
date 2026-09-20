using System.Collections;
using UnityEngine;

/// <summary>
/// Component attached to the ground blood puddle to smoothly expand/spread
/// in a circular pixel-art shape outward upon impact, and fade out when expired.
/// </summary>
public class GroundBloodSpreader : MonoBehaviour
{
    [Header("Spread Animation")]
    [SerializeField] private float targetScale = 0.85f;
    [SerializeField] private float spreadDuration = 0.7f;
    [SerializeField] private float lifetime = 5.0f;
    [SerializeField] private float fadeDuration = 0.8f;

    private SpriteRenderer sr;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>() ?? GetComponentInChildren<SpriteRenderer>();
        transform.localScale = Vector3.zero;
    }

    private void Start()
    {
        StartCoroutine(SpreadAndFadeRoutine());
    }

    private IEnumerator SpreadAndFadeRoutine()
    {
        // 1. Circular Ease-Out Expansion
        Vector3 initialScale = Vector3.zero;
        Vector3 finalScale = Vector3.one * targetScale;
        float elapsed = 0f;

        while (elapsed < spreadDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / spreadDuration);
            // Ease-out cubic curve for natural liquid pooling
            float ease = 1f - Mathf.Pow(1f - t, 3f);
            transform.localScale = Vector3.Lerp(initialScale, finalScale, ease);
            yield return null;
        }

        transform.localScale = finalScale;

        // 2. Stay on ground
        float waitTime = Mathf.Max(0.5f, lifetime - spreadDuration - fadeDuration);
        yield return new WaitForSeconds(waitTime);

        // 3. Smooth Fade Out
        if (sr != null)
        {
            Color startColor = sr.color;
            elapsed = 0f;

            while (elapsed < fadeDuration && sr != null)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(startColor.a, 0f, elapsed / fadeDuration);
                sr.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
                yield return null;
            }
        }

        Destroy(gameObject);
    }
}

