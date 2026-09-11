using System;
using UnityEngine;
using Stopwatch = System.Diagnostics.Stopwatch;

namespace InertialNetcodeDemo
{
    /// <summary>
    /// Position-history dead reckoning with damped kinematics and a decaying
    /// presentation error on recovery. No access to the authoritative target.
    /// Owns this Transform: do not also attach physics/network motion drivers.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    public sealed class InertialNetcodeDummy : MonoBehaviour
    {
        [SerializeField] private NetworkPacketLossSimulator network;
        [SerializeField] private Vector3 presentationOffset = new Vector3(20f, 0f, 0f);
        [Tooltip("Exponential damping per second; zero is pure constant acceleration.")]
        [SerializeField, Min(0f)] private float dampingPerSecond = 0.45f;
        [Tooltip("Bound speculative movement during a sustained outage.")]
        [SerializeField, Min(0.01f)] private float maximumPredictionSeconds = 1.5f;
        [SerializeField, Min(0.01f)] private float maximumSpeed = 15f;
        [SerializeField, Min(0.01f)] private float maximumAcceleration = 30f;
        [Tooltip("0.05 seconds = three frame intervals at 60 FPS, not all frame rates.")]
        [SerializeField, Min(0.001f)] private float reconciliationSeconds = 0.05f;
        [Tooltip("Reset derivatives after a large sample gap instead of averaging the outage.")]
        [SerializeField, Min(0.001f)] private float maximumHistoryGapSeconds = 0.25f;

        public Vector3 EstimatedVelocity => estimatedVelocity;
        public Vector3 EstimatedAcceleration => estimatedAcceleration;
        public bool IsReconciling => reconciling;
        public bool PredictionLimitReached => dropped && predictionAge >= maximumPredictionSeconds;
        // Main-thread elapsed time for THIS component's sample handler + Update.
        // Excludes the simulator, UI, rendering, and all other clients.
        public double LastComputationMilliseconds { get; private set; }

        private bool hasPosition, dropped, recovering, reconciling;
        private Vector3 authoritativePosition, predictedPosition;
        private Vector3 predictedVelocity, predictedAcceleration;
        private Vector3 estimatedVelocity, estimatedAcceleration;
        private Vector3 initialCorrection;
        private float predictionAge, reconciliationAge;

        // Three positions are represented by the latest position plus the prior
        // interval velocity/duration. This avoids allocations and a history list.
        private int historyCount;
        private Vector3 previousPosition, previousIntervalVelocity;
        private double previousTime, previousIntervalSeconds;
        private double sampleCostMilliseconds;
        private static readonly double TickToMilliseconds = 1000.0 / Stopwatch.Frequency;

        private void OnEnable()
        {
            if (network == null)
            {
                Debug.LogError("Assign the shared loss simulator to the inertial client.", this);
                enabled = false;
                return;
            }
            hasPosition = dropped = recovering = reconciling = false;
            historyCount = 0;
            predictionAge = reconciliationAge = 0f;
            estimatedVelocity = estimatedAcceleration = Vector3.zero;
            sampleCostMilliseconds = LastComputationMilliseconds = 0.0;
            network.PacketLossChanged += OnPacketLossChanged;
            network.SnapshotReceived += OnSnapshot;
            if (network.TryGetLatestSnapshot(out var snapshot)) OnSnapshot(snapshot);
            OnPacketLossChanged(network.IsPacketDropped);
        }

        private void OnDisable()
        {
            if (network == null) return;
            network.PacketLossChanged -= OnPacketLossChanged;
            network.SnapshotReceived -= OnSnapshot;
        }

        private void OnPacketLossChanged(bool active)
        {
            if (dropped == active) return;
            dropped = active;
            if (active)
            {
                // Seed from the DISPLAYED position, including any unfinished
                // recovery. A second outage therefore cannot teleport the object.
                predictedPosition = transform.position - presentationOffset;
                predictedVelocity = estimatedVelocity;
                predictedAcceleration = estimatedAcceleration;
                predictionAge = 0f;
                reconciling = recovering = false;
            }
            else
            {
                // The next accepted snapshot establishes a recovery error once.
                // Do not restart the reconciliation timer on every normal packet.
                recovering = hasPosition;
            }
            // Never differentiate positions across an outage boundary.
            historyCount = 0;
        }

        private void OnSnapshot(NetworkPacketLossSimulator.Snapshot snapshot)
        {
            long started = Stopwatch.GetTimestamp();
            if (!dropped)
            {
                authoritativePosition = snapshot.Position;
                ObservePosition(snapshot);
                if (!hasPosition)
                {
                    // Initial spawn is not a recovery; there is no prior display
                    // trajectory to preserve.
                    transform.position = authoritativePosition + presentationOffset;
                    hasPosition = true;
                }
                else if (recovering)
                {
                    initialCorrection = transform.position - presentationOffset - authoritativePosition;
                    reconciliationAge = 0f;
                    reconciling = true;
                    recovering = false;
                }
            }
            sampleCostMilliseconds += (Stopwatch.GetTimestamp() - started) * TickToMilliseconds;
        }

        private void ObservePosition(NetworkPacketLossSimulator.Snapshot snapshot)
        {
            if (historyCount > 0)
            {
                double dt = snapshot.SimulationTime - previousTime;
                if (dt <= 0.000001) return; // Duplicate/out-of-order sample guard.
                if (dt <= maximumHistoryGapSeconds)
                {
                    Vector3 intervalVelocity = (snapshot.Position - previousPosition) / (float)dt;
                    Vector3 acceleration = Vector3.zero;
                    if (historyCount >= 2)
                    {
                        // Adjacent interval velocities live at their midpoints.
                        // Their time separation is (previousDt + currentDt)/2.
                        acceleration = 2f * (intervalVelocity - previousIntervalVelocity)
                            / (float)(previousIntervalSeconds + dt);
                    }
                    estimatedAcceleration = Vector3.ClampMagnitude(acceleration, maximumAcceleration);
                    // Move the interval-average velocity to the newest endpoint.
                    estimatedVelocity = Vector3.ClampMagnitude(
                        intervalVelocity + estimatedAcceleration * (0.5f * (float)dt), maximumSpeed);
                    previousIntervalVelocity = intervalVelocity;
                    previousIntervalSeconds = dt;
                    historyCount = 2;
                    previousPosition = snapshot.Position;
                    previousTime = snapshot.SimulationTime;
                    return;
                }
            }

            // First sample, recovery, or stalled stream: wait for fresh history.
            // Three fresh snapshots supply a complete acceleration estimate.
            historyCount = 1;
            previousPosition = snapshot.Position;
            previousTime = snapshot.SimulationTime;
            estimatedVelocity = estimatedAcceleration = Vector3.zero;
        }

        private void Update()
        {
            long started = Stopwatch.GetTimestamp();
            if (hasPosition && network != null && network.isActiveAndEnabled)
            {
                float dt = Time.deltaTime;
                if (dropped)
                {
                    // Predict only a bounded amount of unseen simulation time.
                    // No live authority reads occur here.
                    float step = Mathf.Min(dt, Mathf.Max(0f, maximumPredictionSeconds - predictionAge));
                    IntegrateDampedKinematics(step);
                    predictionAge += step;
                    if (predictionAge >= maximumPredictionSeconds)
                        predictedVelocity = predictedAcceleration = Vector3.zero;
                    transform.position = predictedPosition + presentationOffset;
                }
                else if (!recovering)
                {
                    Vector3 correction = Vector3.zero;
                    if (reconciling)
                    {
                        float t = Mathf.Clamp01(reconciliationAge / Mathf.Max(0.001f, reconciliationSeconds));
                        float eased = t * t * (3f - 2f * t);
                        correction = Vector3.Lerp(initialCorrection, Vector3.zero, eased);
                        if (t >= 1f) reconciling = false;
                        // Start at t=0 on the recovery frame: preserve the exact
                        // last displayed position instead of snapping partway.
                        reconciliationAge += dt;
                    }
                    // Follow the MOVING authority while its presentation error
                    // fades; never interpolate toward an obsolete static target.
                    transform.position = authoritativePosition + correction + presentationOffset;
                }
            }
            LastComputationMilliseconds = sampleCostMilliseconds
                + (Stopwatch.GetTimestamp() - started) * TickToMilliseconds;
            sampleCostMilliseconds = 0.0;
        }

        private void IntegrateDampedKinematics(float dt)
        {
            if (dt <= 0f) return;
            double lambda = Math.Max(0.0, dampingPerSecond);
            double x = lambda * dt;
            double decay = Math.Exp(-x);
            double positionVelocityFactor, positionAccelerationFactor;
            if (x < 0.001)
            {
                // Stable Taylor forms avoid catastrophic cancellation near zero.
                // lambda=0 gives exactly p += v*dt + 0.5*a*dt^2.
                positionVelocityFactor = dt * (1.0 - x / 2.0 + x * x / 6.0 - x * x * x / 24.0);
                positionAccelerationFactor = dt * dt * (0.5 - x / 3.0 + x * x / 8.0 - x * x * x / 30.0);
            }
            else
            {
                positionVelocityFactor = (1.0 - decay) / lambda;
                positionAccelerationFactor = (1.0 - (1.0 + x) * decay) / (lambda * lambda);
            }
            // Closed-form integration of v' = a - lambda*v and a' = -lambda*a.
            // Here a is the extrapolated driving acceleration. It decays rather
            // than assuming an old turn continues forever. No Euler substeps,
            // frame-count damping, or per-frame allocations are required.
            predictedPosition += predictedVelocity * (float)positionVelocityFactor
                + predictedAcceleration * (float)positionAccelerationFactor;
            predictedVelocity = (predictedVelocity + predictedAcceleration * dt) * (float)decay;
            predictedAcceleration *= (float)decay;
        }

        private void OnValidate()
        {
            dampingPerSecond = Mathf.Max(0f, dampingPerSecond);
            maximumPredictionSeconds = Mathf.Max(0.01f, maximumPredictionSeconds);
            maximumSpeed = Mathf.Max(0.01f, maximumSpeed);
            maximumAcceleration = Mathf.Max(0.01f, maximumAcceleration);
            reconciliationSeconds = Mathf.Max(0.001f, reconciliationSeconds);
            maximumHistoryGapSeconds = Mathf.Max(0.001f, maximumHistoryGapSeconds);
        }
    }
}
