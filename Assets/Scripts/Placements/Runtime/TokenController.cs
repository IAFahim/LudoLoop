// TokenController.cs
using System;
using UnityEngine;

public class TokenController : MonoBehaviour
{
    public int TokenIndex { get; set; }
    public Action<int> OnTokenClicked;

    // This method is called by Unity when the GameObject's collider is clicked
    private void OnMouseDown()
    {
        OnTokenClicked?.Invoke(TokenIndex);
    }

    // You can add visual feedback for highlighting here
    public void SetHighlight(bool isHighlighted)
    {
        // Example: Change scale to show it's selectable
        transform.localScale = isHighlighted ? new Vector3(1.2f, 1.2f, 1.2f) : Vector3.one;
    }
}