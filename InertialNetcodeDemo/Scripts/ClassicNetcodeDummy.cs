using UnityEngine;

namespace InertialNetcodeDemo
{
    /// <summary>
    /// Deliberately naive freeze-and-snap snapshot replication baseline.
    /// This is NOT input-history rollback, resimulation, or lag compensation.
    /// No Update loop: without delivered snapshots its position cannot change.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    public sealed class ClassicNetcodeDummy : MonoBehaviour
    {
        [SerializeField] private NetworkPacketLossSimulator network;
        [Tooltip("Presentation translation only; clients share one logical world.")]
        [SerializeField] private Vector3 presentationOffset = new Vector3(-20f, 0f, 0f);

        private bool isDropped;

        private void OnEnable()
        {
            if (network == null)
            {
                Debug.LogError("Assign the shared loss simulator to the classic client.", this);
                enabled = false;
                return;
            }

            network.PacketLossChanged += OnPacketLossChanged;
            network.SnapshotReceived += OnSnapshot;
            isDropped = network.IsPacketDropped;
            // Bootstrap only from previously delivered, not live, authority.
            if (network.TryGetLatestSnapshot(out var snapshot))
                transform.position = snapshot.Position + presentationOffset;
        }

        private void OnDisable()
        {
            if (network == null) return;
            network.PacketLossChanged -= OnPacketLossChanged;
            network.SnapshotReceived -= OnSnapshot;
        }

        private void OnPacketLossChanged(bool dropped) => isDropped = dropped;

        private void OnSnapshot(NetworkPacketLossSimulator.Snapshot snapshot)
        {
            if (isDropped) return;
            // Intentional direct assignment: recovery produces rubberbanding.
            transform.position = snapshot.Position + presentationOffset;
        }
    }
}
