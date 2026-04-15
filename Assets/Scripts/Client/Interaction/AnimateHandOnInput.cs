using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Implemented following tutorials by Valem Tutorials
/// Passes input action values to an animator that animates the hands
/// </summary>
public class AnimateHandOnInput : MonoBehaviour
{
    /// <summary>
    /// Pinch input action
    /// </summary>
    public InputActionProperty pinchAnimationAction;
    
    /// <summary>
    /// Grip input action
    /// </summary>
    public InputActionProperty gripAnimationAction;

    /// <summary>
    /// Animator attached to a hand model
    /// </summary>
    public Animator handAnimator;

    /// <summary>
    /// Updates animatior state
    /// </summary>
    void Update()
    {
        float triggerValue = pinchAnimationAction.action.ReadValue<float>();
        float gripValue = gripAnimationAction.action.ReadValue<float>();
        handAnimator.SetFloat("Trigger", triggerValue);
        handAnimator.SetFloat("Grip", gripValue);
    }
}
