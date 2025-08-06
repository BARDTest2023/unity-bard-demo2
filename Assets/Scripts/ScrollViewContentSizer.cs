using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Automatically resizes scroll view content based on TextMeshPro text content height.
/// This ensures the scroll view can properly scroll through all the text content.
/// </summary>
public class ScrollViewContentSizer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private TextMeshProUGUI contentText;
    [SerializeField] private RectTransform contentRectTransform;
    
    [Header("Settings")]
    [SerializeField] private float minHeight = 100f;
    [SerializeField] private float padding = 20f;
    [SerializeField] private bool autoScrollToBottom = true;
    [SerializeField] private bool updateOnTextChange = true;
    
    private float lastTextHeight = 0f;
    private bool isInitialized = false;
    
    private void Awake()
    {
        // Auto-assign references if not set
        if (scrollRect == null)
            scrollRect = GetComponent<ScrollRect>();
            
        if (contentText == null && scrollRect != null && scrollRect.content != null)
        {
            contentText = scrollRect.content.GetComponent<TextMeshProUGUI>();
        }
        
        if (contentRectTransform == null && scrollRect != null)
        {
            contentRectTransform = scrollRect.content;
        }
    }
    
    private void Start()
    {
        Initialize();
    }
    
    private void Initialize()
    {
        if (scrollRect == null || contentText == null || contentRectTransform == null)
        {
            Debug.LogError("ScrollViewContentSizer: Missing required references!");
            return;
        }
        
        // Subscribe to text changes if enabled
        if (updateOnTextChange)
        {
            contentText.RegisterDirtyVerticesCallback(OnTextChanged);
        }
        
        isInitialized = true;
        UpdateContentSize();
    }
    
    private void OnTextChanged()
    {
        if (isInitialized)
        {
            UpdateContentSize();
        }
    }
    
    /// <summary>
    /// Updates the content size based on the text height
    /// </summary>
    public void UpdateContentSize()
    {
        if (!isInitialized || contentText == null || contentRectTransform == null)
            return;
            
        // Get the preferred height of the text
        float textHeight = contentText.preferredHeight;
        
        // Only update if the height has actually changed
        if (Mathf.Abs(textHeight - lastTextHeight) > 0.1f)
        {
            // Calculate the new height with padding
            float newHeight = Mathf.Max(textHeight + padding, minHeight);
            
            // Update the content size
            Vector2 sizeDelta = contentRectTransform.sizeDelta;
            sizeDelta.y = newHeight;
            contentRectTransform.sizeDelta = sizeDelta;
            
            lastTextHeight = textHeight;
            
            // Auto-scroll to bottom if enabled
            if (autoScrollToBottom)
            {
                StartCoroutine(ScrollToBottomNextFrame());
            }
        }
    }
    
    /// <summary>
    /// Scrolls to the bottom of the content on the next frame
    /// </summary>
    private System.Collections.IEnumerator ScrollToBottomNextFrame()
    {
        yield return null; // Wait for the next frame
        
        if (scrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 0f;
        }
    }
    
    /// <summary>
    /// Manually scroll to the bottom
    /// </summary>
    public void ScrollToBottom()
    {
        if (scrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 0f;
        }
    }
    
    /// <summary>
    /// Manually scroll to the top
    /// </summary>
    public void ScrollToTop()
    {
        if (scrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 1f;
        }
    }
    
    /// <summary>
    /// Sets the text content and updates the size
    /// </summary>
    /// <param name="text">The text to set</param>
    public void SetText(string text)
    {
        if (contentText != null)
        {
            contentText.text = text;
            UpdateContentSize();
        }
    }
    
    /// <summary>
    /// Appends text to the existing content
    /// </summary>
    /// <param name="text">The text to append</param>
    public void AppendText(string text)
    {
        if (contentText != null)
        {
            contentText.text += text;
            UpdateContentSize();
        }
    }
    
    /// <summary>
    /// Clears the text content
    /// </summary>
    public void ClearText()
    {
        if (contentText != null)
        {
            contentText.text = "";
            UpdateContentSize();
        }
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from text changes
        if (contentText != null)
        {
            contentText.UnregisterDirtyVerticesCallback(OnTextChanged);
        }
    }
    
    private void OnValidate()
    {
        // Update in editor when values change
        if (Application.isPlaying && isInitialized)
        {
            UpdateContentSize();
        }
    }
} 