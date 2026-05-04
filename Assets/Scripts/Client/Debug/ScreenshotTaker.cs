using System;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TvmVr2.Client.DebugTools
{
    [DisallowMultipleComponent]
    public sealed class ScreenshotTaker : MonoBehaviour
    {
        [SerializeField] private Key captureKey = Key.F9;
        [SerializeField] private int superSize = 1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureInstance()
        {
            if (FindFirstObjectByType<ScreenshotTaker>() != null)
                return;

            var screenshotTaker = new GameObject("Screenshot Taker");
            DontDestroyOnLoad(screenshotTaker);
            screenshotTaker.AddComponent<ScreenshotTaker>();
        }

        private void Start()
        {
            UnityEngine.Debug.Log($"ScreenshotTaker: press {captureKey} to save a screenshot.");
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || captureKey == Key.None)
                return;

            var key = keyboard[captureKey];
            if (key != null && key.wasPressedThisFrame)
                CaptureScreenshot();
        }

        [ContextMenu("Capture Screenshot")]
        public void CaptureScreenshot()
        {
            var directory = Path.Combine(GetProjectRootPath(), "screens");
            Directory.CreateDirectory(directory);

            var fileName = $"screenshot_{DateTime.Now:yyyyMMdd_HHmmss}.png";
            var path = Path.Combine(directory, fileName);
            ScreenCapture.CaptureScreenshot(path, Mathf.Max(1, superSize));

            UnityEngine.Debug.Log($"ScreenshotTaker: saved screenshot to {path}");
        }

        private static string GetProjectRootPath()
        {
            return Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;
        }
    }
}
