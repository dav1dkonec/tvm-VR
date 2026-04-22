using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls sequence playback
/// </summary>
public class PlaybackUI : MonoBehaviour
{
    /// <summary>
    /// Sequence object
    /// </summary>
    public Sequence sequence;
    /// <summary>
    /// Play icon
    /// </summary>
    public Sprite playIcon;
    /// <summary>
    /// Pause icon
    /// </summary>
    public Sprite pauseIcon;

    /// <summary>
    /// Progress slider control
    /// </summary>
    public Slider progressSlider;
    /// <summary>
    /// FPS slider control
    /// </summary>
    public Slider fpsSlider;

    /// <summary>
    /// Image component of the play button
    /// </summary>
    public Image playImage;
    
    /// <summary>
    /// Playback UI play button
    /// </summary>
    public Button play;
    /// <summary>
    /// Playback UI previous button
    /// </summary>
    public Button previous;
    /// <summary>
    /// Playback UI next button
    /// </summary>
    public Button next;
    /// <summary>
    /// Playback UI first button
    /// </summary>
    public Button first;
    /// <summary>
    /// Playback UI last button
    /// </summary>
    public Button last;

    /// <summary>
    /// Deform button
    /// </summary>
    public Button deform;

    /// <summary>
    /// FPS lower limit text
    /// </summary>
    public TMP_Text fpsMin;
    /// <summary>
    /// FPS upper limit text
    /// </summary>
    public TMP_Text fpsMax;

    /// <summary>
    /// Plays or pauses the sequence (toggle)
    /// </summary>
    public void Play()
    {
        if (InflateDeflateUI.BlockIfPickActive())
            return;

        if (Sequence.playing)
        {
            sequence.Pause();
            playImage.sprite = playIcon;
        }
        else
        {
            sequence.Play();
            playImage.sprite = pauseIcon;
        }

        previous.interactable = !previous.interactable;
        next.interactable = !next.interactable;
        first.interactable = !first.interactable;
        last.interactable = !last.interactable;
    }


    /// <summary>
    /// Moves the sequence to the previous frame
    /// </summary>
    public void Previous()
    {
        if (InflateDeflateUI.BlockIfPickActive())
            return;

        sequence.Previous();
    }


    /// <summary>
    /// Moves the sequence to the next frame
    /// </summary>
    public void Next()
    {
        if (InflateDeflateUI.BlockIfPickActive())
            return;

        sequence.Next();

    }


    /// <summary>
    /// Moves the sequence to the first frame
    /// </summary>
    public void First()
    {
        if (InflateDeflateUI.BlockIfPickActive())
            return;

        sequence.First();

    }

    /// <summary>
    /// Moves the sequence to the last frame
    /// </summary>
    public void Last()
    {
        if (InflateDeflateUI.BlockIfPickActive())
            return;

        sequence.Last();
    }

    /// <summary>
    /// Enables the playback controls
    /// </summary>
    public void Enable()
    {
        previous.interactable = true;
        next.interactable = true;
        first.interactable = true;
        last.interactable = true;
        play.interactable = true;
        deform.interactable = true;
    }

    /// <summary>
    /// Sets the frames per second
    /// </summary>
    /// <param name="fps">Frames per second</param>
    public void SetFPS(int fps)
    {
        fpsSlider.maxValue = fps * 2;
        fpsSlider.value = fps;
        fpsMin.text = "" + fpsSlider.minValue;
        fpsMax.text = "" + fpsSlider.maxValue;
    }

    /// <summary>
    /// Returns the seconds per frame
    /// </summary>
    /// <returns>Seconds per frame</returns>
    public float GetSPF()
    {
        return 1f / fpsSlider.value;
    }
}
