using System;
using System.IO;
using UnityEngine;

namespace TvmVr2.Client.DebugTools
{
    [DisallowMultipleComponent]
    public sealed class ScreenshotTaker : MonoBehaviour
    {
        [SerializeField] private KeyCode captureKey = KeyCode.Alpha0;
        [SerializeField] private int superSize = 1;

        private void Update()
        {
            if (Input.GetKeyDown(captureKey))
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

            Debug.Log($"ScreenshotTaker: saved screenshot to {path}");
        }

        private static string GetProjectRootPath()
        {
            return Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;
        }
    }
}
