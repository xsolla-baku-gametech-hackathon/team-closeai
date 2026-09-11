using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace InertialNetcodeDemo
{
    /// <summary>
    /// Controls two side-by-side cameras and an existing Screen Space Overlay
    /// uGUI canvas. Uses legacy uGUI Text (not TextMeshPro) for minimal setup.
    /// Telemetry is refreshed at 4 Hz; object labels follow every LateUpdate.
    /// </summary>
    [DefaultExecutionOrder(200)]
    [DisallowMultipleComponent]
    public sealed class DemoUIController : MonoBehaviour
    {
        [Header("Simulation")]
        [SerializeField] private NetworkPacketLossSimulator network;
        [SerializeField] private ClassicNetcodeDummy classicClient;
        [SerializeField] private InertialNetcodeDummy inertialClient;

        [Header("Two orthographic, non-stacked cameras")]
        [SerializeField] private Camera classicCamera;
        [SerializeField] private Camera inertialCamera;
        [SerializeField, Min(1f)] private float minimumVerticalHalfExtent = 7f;
        [SerializeField, Min(1f)] private float minimumHorizontalHalfExtent = 8f;

        [Header("Existing Screen Space Overlay canvas")]
        [SerializeField] private Canvas overlayCanvas;
        [SerializeField] private Text packetLossText;
        [SerializeField] private Text telemetryText;
        [SerializeField] private Text classicLabel;
        [SerializeField] private Text inertialLabel;
        [SerializeField] private Button toggleLossButton;
        [SerializeField] private Text toggleButtonText;
        [SerializeField] private Vector2 labelOffsetPixels = new Vector2(0f, 38f);

        private Rect oldClassicRect, oldInertialRect;
        private float oldClassicSize, oldInertialSize;
        private bool camerasConfigured;
        private int screenWidth, screenHeight, sampleCount;
        private double elapsed, costSum, costMaximum;
        private float displayedFps;
        private double displayedAverage, displayedMaximum;

        private void OnEnable()
        {
            if (network == null || classicClient == null || inertialClient == null
                || classicCamera == null || inertialCamera == null
                || classicCamera == inertialCamera || overlayCanvas == null
                || overlayCanvas.renderMode != RenderMode.ScreenSpaceOverlay
                || !classicCamera.orthographic || !inertialCamera.orthographic)
            {
                Debug.LogError("Assign both clients, two distinct orthographic cameras, the loss simulator, "
                    + "and a Screen Space Overlay canvas to DemoUIController.", this);
                enabled = false;
                return;
            }

            oldClassicRect = classicCamera.rect;
            oldInertialRect = inertialCamera.rect;
            oldClassicSize = classicCamera.orthographicSize;
            oldInertialSize = inertialCamera.orthographicSize;
            camerasConfigured = true;
            ConfigureCameras();
            elapsed = costSum = costMaximum = 0.0;
            sampleCount = 0;

            if (classicLabel != null) classicLabel.text = "Classic Netcode (Standard)";
            if (inertialLabel != null) inertialLabel.text = "Inertial Kinematic Netcode (Our Solution)";
            network.PacketLossChanged += OnLossChanged;
            // Wire through code OR the Inspector, not both. This script owns its
            // runtime listener and removes only that listener when disabled.
            if (toggleLossButton != null) toggleLossButton.onClick.AddListener(OnToggleClicked);
            RefreshStatus();
            RefreshTelemetry();
        }

        private void OnDisable()
        {
            if (network != null) network.PacketLossChanged -= OnLossChanged;
            if (toggleLossButton != null) toggleLossButton.onClick.RemoveListener(OnToggleClicked);
            if (!camerasConfigured) return;
            if (classicCamera != null)
            {
                classicCamera.rect = oldClassicRect;
                classicCamera.orthographicSize = oldClassicSize;
            }
            if (inertialCamera != null)
            {
                inertialCamera.rect = oldInertialRect;
                inertialCamera.orthographicSize = oldInertialSize;
            }
            camerasConfigured = false;
        }

        private void OnToggleClicked()
        {
            network.TogglePacketLoss();
            // Avoid retaining button focus while Space is used as a demo hotkey.
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        }

        private void OnLossChanged(bool active) => RefreshStatus();

        private void LateUpdate()
        {
            if (screenWidth != Screen.width || screenHeight != Screen.height)
                ConfigureCameras();

            PositionLabel(classicLabel, classicCamera, classicClient.transform.position);
            PositionLabel(inertialLabel, inertialCamera, inertialClient.transform.position);

            // Wall-clock FPS remains meaningful when simulation time is paused.
            elapsed += Time.unscaledDeltaTime;
            sampleCount++;
            double cost = inertialClient.LastComputationMilliseconds;
            costSum += cost;
            costMaximum = System.Math.Max(costMaximum, cost);
            if (elapsed >= 0.25)
            {
                displayedFps = (float)(sampleCount / elapsed);
                displayedAverage = costSum / sampleCount;
                displayedMaximum = costMaximum;
                RefreshTelemetry();
                elapsed = costSum = costMaximum = 0.0;
                sampleCount = 0;
            }
        }

        private void RefreshStatus()
        {
            bool loss = network.IsPacketDropped;
            if (packetLossText != null)
            {
                packetLossText.text = loss ? "PACKET LOSS: ACTIVE" : "PACKET LOSS: NORMAL";
                packetLossText.color = loss ? new Color(1f, 0.25f, 0.25f) : new Color(0.25f, 1f, 0.45f);
            }
            if (toggleButtonText != null)
                toggleButtonText.text = loss ? "Restore packets [Space]" : "Drop packets [Space]";
        }

        private void RefreshTelemetry()
        {
            if (telemetryText == null) return;
            string prediction = inertialClient.PredictionLimitReached ? "PREDICTION LIMIT: HOLDING"
                : inertialClient.IsReconciling ? "RECONCILING" : "";
            telemetryText.text = $"FPS: {displayedFps:F0}   |   PING: {network.IllustrativePingMilliseconds:F0} ms "
                + "(illustrative; no latency applied)\n"
                + $"INERTIAL CPU: {displayedAverage:F4} ms avg / {displayedMaximum:F4} ms max "
                + "(component only; target < 0.05 ms)\n"
                + "Shared 100% burst-loss gate | Classic baseline is freeze-and-snap, NOT rollback\n"
                + prediction;
        }

        private void ConfigureCameras()
        {
            screenWidth = Screen.width;
            screenHeight = Screen.height;
            classicCamera.rect = new Rect(0f, 0f, 0.5f, 1f);
            inertialCamera.rect = new Rect(0.5f, 0f, 0.5f, 1f);
            // Preserve horizontal track visibility when resizing the Game view.
            float panelAspect = Mathf.Max(0.01f, Screen.width * 0.5f / Mathf.Max(1, Screen.height));
            float size = Mathf.Max(minimumVerticalHalfExtent, minimumHorizontalHalfExtent / panelAspect);
            classicCamera.orthographicSize = size;
            inertialCamera.orthographicSize = size;
        }

        private void PositionLabel(Text label, Camera cameraForPanel, Vector3 worldPosition)
        {
            if (label == null) return;
            Vector3 screen = cameraForPanel.WorldToScreenPoint(worldPosition);
            label.enabled = screen.z > 0f;
            if (!label.enabled) return;
            screen.x += labelOffsetPixels.x;
            screen.y += labelOffsetPixels.y;
            Rect viewport = cameraForPanel.pixelRect;
            float halfWidth = label.rectTransform.rect.width * overlayCanvas.scaleFactor * 0.5f;
            float margin = Mathf.Min(halfWidth + 8f, viewport.width * 0.5f);
            screen.x = Mathf.Clamp(screen.x, viewport.xMin + margin, viewport.xMax - margin);
            screen.y = Mathf.Clamp(screen.y, viewport.yMin + 30f, viewport.yMax - 30f);

            // With an overlay canvas, screenPointCamera MUST be null. Converting
            // through the parent rect also supports CanvasScaler resolution changes.
            var parentRect = label.rectTransform.parent as RectTransform;
            if (parentRect != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRect, new Vector2(screen.x, screen.y), null, out Vector2 local))
            {
                label.rectTransform.localPosition = new Vector3(local.x, local.y, 0f);
            }
        }
    }
}
