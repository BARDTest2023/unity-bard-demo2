# ScrollView Content Sizer

This component automatically resizes a scroll view's content based on the TextMeshPro text content height. It ensures that the scroll view can properly scroll through all the text content as it grows.

## Features

- **Automatic Content Resizing**: Automatically adjusts the content height based on text content
- **Auto-scroll to Bottom**: Optionally scrolls to the bottom when new content is added
- **Text Change Detection**: Automatically updates when text content changes
- **Manual Control**: Provides methods to manually control text and scrolling
- **Fallback Support**: Works with existing scroll view implementations

## Setup Instructions

### 1. Add the Component

1. Select your ScrollRect GameObject in the hierarchy
2. Add the `ScrollViewContentSizer` component to the ScrollRect GameObject
3. The component will automatically try to find the required references

### 2. Configure References

The component will auto-assign references, but you can manually configure them:

- **Scroll Rect**: The ScrollRect component (auto-assigned)
- **Content Text**: The TextMeshProUGUI component in the content area
- **Content Rect Transform**: The RectTransform of the content area (auto-assigned)

### 3. Configure Settings

- **Min Height**: Minimum height for the content (default: 100)
- **Padding**: Additional padding added to the text height (default: 20)
- **Auto Scroll to Bottom**: Whether to automatically scroll to bottom when content changes
- **Update on Text Change**: Whether to automatically update when text content changes

## Usage

### Basic Usage

```csharp
// Get the component
ScrollViewContentSizer contentSizer = GetComponent<ScrollViewContentSizer>();

// Set text content
contentSizer.SetText("Your text content here");

// Append text
contentSizer.AppendText("Additional text to append");

// Clear text
contentSizer.ClearText();

// Manual scrolling
contentSizer.ScrollToBottom();
contentSizer.ScrollToTop();
```

### Integration with UIManager

The `UIManager.cs` has been updated to use the ScrollViewContentSizer:

1. Add the `ScrollViewContentSizer` component to your response scroll view
2. Assign the `responseContentSizer` reference in the UIManager inspector
3. The response text will now automatically resize as content is added

### Test Script

Use the `ScrollViewTest.cs` script to test the functionality:

1. Add the `ScrollViewTest` component to a GameObject
2. Assign the `contentSizer` reference
3. Use the test buttons to add text and see the content grow

## How It Works

1. **Text Height Detection**: The component monitors the `preferredHeight` of the TextMeshPro text
2. **Content Resizing**: When the text height changes, it updates the content RectTransform's `sizeDelta.y`
3. **Auto-scrolling**: Optionally scrolls to the bottom using `verticalNormalizedPosition = 0f`
4. **Change Detection**: Uses TextMeshPro's `RegisterDirtyVerticesCallback` to detect text changes

## Troubleshooting

### Common Issues

1. **Content doesn't resize**: Make sure the TextMeshPro component is properly assigned
2. **Scroll view doesn't scroll**: Check that the ScrollRect's content is properly configured
3. **Text not visible**: Ensure the content RectTransform has proper anchors and pivot settings

### Debug Tips

- Enable debug logging in the component to see what's happening
- Check the Inspector to ensure all references are properly assigned
- Verify that the TextMeshPro component has the correct text settings

## Example Hierarchy

```
ScrollRect (GameObject with ScrollRect + ScrollViewContentSizer)
├── Viewport (GameObject with Mask + Image)
│   └── Content (GameObject with RectTransform + TextMeshProUGUI)
└── Scrollbar (GameObject with Scrollbar)
```

## Performance Notes

- The component only updates when the text height actually changes
- Uses efficient TextMeshPro callbacks for change detection
- Minimal performance impact during normal operation 