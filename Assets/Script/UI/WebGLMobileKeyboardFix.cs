using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Ensures native mobile software keyboards open when tapping TMP_InputField on iOS, Android, and WebGL browsers.
/// </summary>
[RequireComponent(typeof(TMP_InputField))]
public class WebGLMobileKeyboardFix : MonoBehaviour, ISelectHandler, IPointerClickHandler, IDeselectHandler
{
    [SerializeField] private TouchScreenKeyboardType keyboardType = TouchScreenKeyboardType.Default;
    [SerializeField] private bool autocorrection = false;
    [SerializeField] private bool multiline = false;
    [SerializeField] private bool secure = false;
    [SerializeField] private string placeholderText = "Tap to type...";

    private TMP_InputField inputField;
    private TouchScreenKeyboard mobileKeyboard;

    private void Awake()
    {
        inputField = GetComponent<TMP_InputField>();
    }

    public void OnSelect(BaseEventData eventData)
    {
        OpenMobileKeyboard();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        OpenMobileKeyboard();
    }

    public void OnDeselect(BaseEventData eventData)
    {
        CloseMobileKeyboard();
    }

    private void OpenMobileKeyboard()
    {
        if (TouchScreenKeyboard.isSupported)
        {
            if (mobileKeyboard == null || !mobileKeyboard.active)
            {
                mobileKeyboard = TouchScreenKeyboard.Open(
                    inputField.text,
                    keyboardType,
                    autocorrection,
                    multiline,
                    secure,
                    false,
                    placeholderText
                );
            }
        }
    }

    private void CloseMobileKeyboard()
    {
        if (mobileKeyboard != null)
        {
            mobileKeyboard.active = false;
            mobileKeyboard = null;
        }
    }

    private void Update()
    {
        if (mobileKeyboard != null && mobileKeyboard.active)
        {
            if (inputField != null && inputField.text != mobileKeyboard.text)
            {
                inputField.text = mobileKeyboard.text;
            }

            if (mobileKeyboard.status == TouchScreenKeyboard.Status.Done || 
                mobileKeyboard.status == TouchScreenKeyboard.Status.Canceled)
            {
                mobileKeyboard = null;
            }
        }
    }
}
