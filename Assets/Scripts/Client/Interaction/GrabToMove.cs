using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Moves the sequence on right hand grab
/// </summary>
public class GrabToMove : MonoBehaviour
{
    /// <summary>
    /// Grab target object
    /// </summary>
    public GameObject target;

    /// <summary>
    /// Hand object
    /// </summary>
    public GameObject rightHand;

    /// <summary>
    /// Right hand grab input property
    /// </summary>
    public InputActionProperty rightSelect;

    /// <summary>
    /// Last postion of the right hand controller
    /// </summary>
    Vector3 lastRightPosition;

    /// <summary>
    /// True if the action was active in the previous frame, false otherwise
    /// </summary>
    bool wasDownR;

    /// <summary>
    /// Translates the target object on drag
    /// </summary>
    void Update()
    {
        if (InflateDeflateUI.IsAnyPickActive)
        {
            wasDownR = false;
            return;
        }

        if (StickyHandMenuToggle.SuppressRightGrabToMove || PinnedHandMenuController.SuppressRightGrabToMove)
        {
            wasDownR = false;
            return;
        }

        // Disabled during editing
        if (Sequence.editing || CenterUI.HasActiveSelection)
        {
            wasDownR = false;
            return;
        }

        bool rightDown = rightSelect.action.IsPressed();
        
        if (rightDown)
        {
            if (wasDownR)
            {
                // Apply translation
                var translation = rightHand.transform.position - lastRightPosition;
                target.transform.position = target.transform.position + translation * 2f;
            }

            lastRightPosition = rightHand.transform.position;
            wasDownR = true;
        }
        else
        {
            wasDownR = false;
        }
    }
}
