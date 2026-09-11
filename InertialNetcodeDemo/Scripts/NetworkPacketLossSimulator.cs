using System;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace InertialNetcodeDemo
{
    /// <summary>
    /// A controlled, shared burst-loss gate, NOT a transport/rollback library.
    /// Publishes one position-only snapshot per rendered simulation frame.
    /// Both clients receive exactly the same accepted snapshots.
    /// No target state is exposed to clients while loss is active.
    /// </summary>
    [DefaultExecutionOrder(-200)]
    [DisallowMultipleComponent]
    public sealed class NetworkPacketLossSimulator : MonoBehaviour
    {
        public readonly struct Snapshot
        {
            public readonly Vector3 Position;
            public readonly double SimulationTime;
            public Snapshot(Vector3 position, double simulationTime)
            {
                Position = position;
                SimulationTime = simulationTime;
            }
        }

        [SerializeField] private TargetMovement target;
        [SerializeField] private bool startWithPacketLoss;
        [SerializeField] private bool enableSpacebar = true;
        [Tooltip("Display only. No latency queue is applied in this loss-only experiment.")]
        [SerializeField, Min(0f)] private float illustrativePingMilliseconds = 150f;

        public bool IsPacketDropped { get; private set; }
        public float IllustrativePingMilliseconds => illustrativePingMilliseconds;
        public event Action<bool> PacketLossChanged;
        public event Action<Snapshot> SnapshotReceived;

        private Snapshot latest;
        private bool hasLatest;

        private void Awake()
        {
            IsPacketDropped = startWithPacketLoss;
            if (target == null)
            {
                Debug.LogError("Assign the authoritative TargetMovement to the loss simulator.", this);
                enabled = false;
            }
        }

        private void Update()
        {
            bool spacePressed = false;
            // Prefer the new backend when Active Input Handling is Both so one
            // physical key press never toggles the state twice.
#if ENABLE_INPUT_SYSTEM
            spacePressed = Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            spacePressed = Input.GetKeyDown(KeyCode.Space);
#endif
            if (enableSpacebar && spacePressed)
                TogglePacketLoss();

            if (!IsPacketDropped && Time.deltaTime > 0f)
                PublishSnapshot();
        }

        /// <summary>Wire a uGUI Button.onClick to this parameterless method.</summary>
        public void TogglePacketLoss() => SetPacketLossActive(!IsPacketDropped);

        public void SetPacketLossActive(bool active)
        {
            if (!isActiveAndEnabled || IsPacketDropped == active)
                return;

            IsPacketDropped = active;
            PacketLossChanged?.Invoke(active);
            // Recovery delivers the CURRENT authority immediately, including
            // when a UI click runs after this component's normal Update.
            if (!active)
                PublishSnapshot();
        }

        /// <summary>
        /// Returns only the last DELIVERED snapshot, never live authority state.
        /// Used when a client is enabled after the stream has already begun.
        /// </summary>
        public bool TryGetLatestSnapshot(out Snapshot snapshot)
        {
            snapshot = latest;
            return hasLatest;
        }

        private void PublishSnapshot()
        {
            if (target == null || !target.isActiveAndEnabled || IsPacketDropped)
                return;

            double now = Time.timeAsDouble;
            // Recovery and Update can run in the same frame. Suppress duplicates
            // so finite differences never divide by a zero sample interval.
            if (hasLatest && now <= latest.SimulationTime)
                return;

            latest = new Snapshot(target.Position, now);
            hasLatest = true;
            SnapshotReceived?.Invoke(latest);
        }
    }
}
