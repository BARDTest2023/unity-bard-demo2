using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SubmitResultsButtonCreator : MonoBehaviour
{
    [Header("Button Settings")]
    [SerializeField] private string buttonText = "Submit Results";
    [SerializeField] private Vector2 buttonPosition = new Vector2(0, 200);
    [SerializeField] private Vector2 buttonSize = new Vector2(200, 70);
    [SerializeField] private Color buttonColor = new Color(0.2f, 0.6f, 1f, 1f); // Blue color

    [Header("Auto Create")]
    [SerializeField] private bool createButtonOnStart = true;

    private void Start()
    {
        if (createButtonOnStart)
        {
            CreateSubmitResultsButton();
        }
    }

    [ContextMenu("Create Submit Results Button")]
    public void CreateSubmitResultsButton()
    {
        Debug.Log("Creating Submit Results Button...");

        // Find the Canvas
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("No Canvas found in the scene!");
            return;
        }

        // Check if button already exists
        if (IsSubmitResultsButtonExists())
        {
            Debug.LogWarning("Submit Results Button already exists!");
            return;
        }

        // Create the button GameObject
        GameObject buttonGO = new GameObject("Submit Results Button");
        buttonGO.transform.SetParent(canvas.transform, false);

        // Add RectTransform
        RectTransform rectTransform = buttonGO.AddComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 1);
        rectTransform.anchorMax = new Vector2(0.5f, 1);
        rectTransform.anchoredPosition = buttonPosition;
        rectTransform.sizeDelta = buttonSize;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);

        // Add Image component (button background)
        Image image = buttonGO.AddComponent<Image>();
        image.color = buttonColor;
        image.sprite = GetDefaultButtonSprite();

        // Add Button component
        Button button = buttonGO.AddComponent<Button>();
        button.targetGraphic = image;

        // Set button colors
        ColorBlock colors = button.colors;
        colors.normalColor = buttonColor;
        colors.highlightedColor = new Color(buttonColor.r + 0.1f, buttonColor.g + 0.1f, buttonColor.b + 0.1f, 1f);
        colors.pressedColor = new Color(buttonColor.r - 0.1f, buttonColor.g - 0.1f, buttonColor.b - 0.1f, 1f);
        button.colors = colors;

        // Create text GameObject
        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(buttonGO.transform, false);

        // Add RectTransform for text
        RectTransform textRectTransform = textGO.AddComponent<RectTransform>();
        textRectTransform.anchorMin = Vector2.zero;
        textRectTransform.anchorMax = Vector2.one;
        textRectTransform.offsetMin = Vector2.zero;
        textRectTransform.offsetMax = Vector2.zero;
        textRectTransform.pivot = new Vector2(0.5f, 0.5f);

        // Add TextMeshPro component
        TextMeshProUGUI textComponent = textGO.AddComponent<TextMeshProUGUI>();
        textComponent.text = buttonText;
        textComponent.fontSize = 18;
        textComponent.color = Color.white;
        textComponent.alignment = TextAlignmentOptions.Center;
        textComponent.fontStyle = FontStyles.Bold;

        // Connect to UIManager
        ConnectButtonToUIManager(button);

        Debug.Log("✅ Submit Results Button created successfully!");
        Debug.Log("Button is now connected to UIManager and ready to use.");
    }

    private bool IsSubmitResultsButtonExists()
    {
        UIManager uiManager = FindObjectOfType<UIManager>();
        if (uiManager != null)
        {
            // Use reflection to check if the button is already assigned
            var field = typeof(UIManager).GetField("submitResultsButton", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                Button existingButton = field.GetValue(uiManager) as Button;
                return existingButton != null;
            }
        }
        return false;
    }

    private void ConnectButtonToUIManager(Button button)
    {
        UIManager uiManager = FindObjectOfType<UIManager>();
        if (uiManager != null)
        {
            // Use reflection to set the button reference
            var field = typeof(UIManager).GetField("submitResultsButton", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(uiManager, button);
                Debug.Log("✅ Button connected to UIManager");
            }
            else
            {
                Debug.LogError("❌ Could not find submitResultsButton field in UIManager");
            }
        }
        else
        {
            Debug.LogError("❌ UIManager not found in scene!");
        }
    }

    private Sprite GetDefaultButtonSprite()
    {
        // Try to get the default UI sprite
        Sprite defaultSprite = Resources.Load<Sprite>("UI/Skin/UISprite");
        if (defaultSprite == null)
        {
            // Create a simple white sprite if default is not found
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            defaultSprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
        }
        return defaultSprite;
    }

    [ContextMenu("Remove Submit Results Button")]
    public void RemoveSubmitResultsButton()
    {
        GameObject buttonGO = GameObject.Find("Submit Results Button");
        if (buttonGO != null)
        {
            DestroyImmediate(buttonGO);
            Debug.Log("✅ Submit Results Button removed");
        }
        else
        {
            Debug.LogWarning("Submit Results Button not found");
        }
    }

    [ContextMenu("Test Button Connection")]
    public void TestButtonConnection()
    {
        UIManager uiManager = FindObjectOfType<UIManager>();
        if (uiManager != null)
        {
            var field = typeof(UIManager).GetField("submitResultsButton", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                Button button = field.GetValue(uiManager) as Button;
                if (button != null)
                {
                    Debug.Log("✅ Submit Results Button is properly connected to UIManager");
                }
                else
                {
                    Debug.LogWarning("⚠️ Submit Results Button field is null");
                }
            }
        }
    }
} 