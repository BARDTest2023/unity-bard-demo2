using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Test script to demonstrate ScrollViewContentSizer functionality
/// This script adds text to a scroll view to show how the content grows dynamically
/// </summary>
public class ScrollViewTest : MonoBehaviour
{
    [Header("Test Settings")]
    [SerializeField] private ScrollViewContentSizer contentSizer;
    [SerializeField] private Button addTextButton;
    [SerializeField] private Button clearTextButton;
    [SerializeField] private TMP_InputField testInputField;
    [SerializeField] private Button addCustomTextButton;
    
    [Header("Auto Test")]
    [SerializeField] private bool enableAutoTest = false;
    [SerializeField] private float autoTestInterval = 2f;
    [SerializeField] private int maxAutoTestLines = 20;
    
    private float autoTestTimer = 0f;
    private int autoTestLineCount = 0;
    
    private void Start()
    {
        // Set up button listeners
        if (addTextButton != null)
        {
            addTextButton.onClick.AddListener(AddTestText);
        }
        
        if (clearTextButton != null)
        {
            clearTextButton.onClick.AddListener(ClearText);
        }
        
        if (addCustomTextButton != null)
        {
            addCustomTextButton.onClick.AddListener(AddCustomText);
        }
        
        // Initialize with some test text
        if (contentSizer != null)
        {
            contentSizer.SetText("ScrollView Content Sizer Test\n" +
                               "This text will automatically resize the scroll view content.\n" +
                               "Add more text to see the content grow dynamically.\n\n");
        }
    }
    
    private void Update()
    {
        // Auto test functionality
        if (enableAutoTest && contentSizer != null)
        {
            autoTestTimer += Time.deltaTime;
            
            if (autoTestTimer >= autoTestInterval)
            {
                autoTestTimer = 0f;
                AddAutoTestText();
            }
        }
    }
    
    /// <summary>
    /// Adds predefined test text to demonstrate content growth
    /// </summary>
    public void AddTestText()
    {
        if (contentSizer != null)
        {
            string testText = $"[{System.DateTime.Now.ToString("HH:mm:ss")}] " +
                            "This is a test message to demonstrate how the scroll view content " +
                            "automatically resizes as more text is added. The content will grow " +
                            "vertically to accommodate all the text, and the scroll view will " +
                            "automatically scroll to the bottom to show the latest content.\n\n";
            
            contentSizer.AppendText(testText);
        }
    }
    
    /// <summary>
    /// Adds custom text from the input field
    /// </summary>
    public void AddCustomText()
    {
        if (contentSizer != null && testInputField != null && !string.IsNullOrEmpty(testInputField.text))
        {
            string customText = $"[{System.DateTime.Now.ToString("HH:mm:ss")}] {testInputField.text}\n";
            contentSizer.AppendText(customText);
            testInputField.text = ""; // Clear the input field
        }
    }
    
    /// <summary>
    /// Clears all text from the scroll view
    /// </summary>
    public void ClearText()
    {
        if (contentSizer != null)
        {
            contentSizer.ClearText();
            autoTestLineCount = 0;
        }
    }
    
    /// <summary>
    /// Adds auto-generated test text
    /// </summary>
    private void AddAutoTestText()
    {
        if (contentSizer != null && autoTestLineCount < maxAutoTestLines)
        {
            string autoText = $"[AUTO {System.DateTime.Now.ToString("HH:mm:ss")}] " +
                            $"Auto-generated line {autoTestLineCount + 1}. " +
                            "This demonstrates automatic content growth over time.\n";
            
            contentSizer.AppendText(autoText);
            autoTestLineCount++;
        }
    }
    
    /// <summary>
    /// Toggles auto test mode
    /// </summary>
    public void ToggleAutoTest()
    {
        enableAutoTest = !enableAutoTest;
        if (enableAutoTest)
        {
            autoTestLineCount = 0;
        }
    }
    
    /// <summary>
    /// Scrolls to the top of the content
    /// </summary>
    public void ScrollToTop()
    {
        if (contentSizer != null)
        {
            contentSizer.ScrollToTop();
        }
    }
    
    /// <summary>
    /// Scrolls to the bottom of the content
    /// </summary>
    public void ScrollToBottom()
    {
        if (contentSizer != null)
        {
            contentSizer.ScrollToBottom();
        }
    }
} 