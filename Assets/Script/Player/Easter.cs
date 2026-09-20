using UnityEngine;
using UnityEngine.UI;

public class Easter : MonoBehaviour
{
    [Header("UI Text References")]
    [SerializeField] private Text easterText;
    [SerializeField] private TMPro.TextMeshProUGUI easterTmpText;
    [SerializeField] private GameObject easterTextObject;

    [Header("Message Settings")]
    [SerializeField] private string easterMessage = "You found the Easter!";

    private BoxCollider2D boxCollider;

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

        AutoFindTextComponent();
        HideEasterUI();
    }

    private void Start()
    {
        AutoFindTextComponent();
        HideEasterUI();
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
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
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

    private bool IsPlayerCollider(Collider2D other)
    {
        if (other == null) return false;

        if (other.CompareTag("Player")) return true;
        if (other.transform.root != null && other.transform.root.CompareTag("Player")) return true;
        if (other.GetComponentInParent<CarControllerSingle>() != null) return true;
        if (other.GetComponentInParent<NetworkCarController>() != null) return true;

        return false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (IsPlayerCollider(other))
        {
            ShowEasterUI();
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (IsPlayerCollider(other))
        {
            ShowEasterUI();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (IsPlayerCollider(other))
        {
            HideEasterUI();
        }
    }
}