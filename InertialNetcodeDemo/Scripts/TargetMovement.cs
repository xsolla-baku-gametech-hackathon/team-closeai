using System;
using UnityEngine;

namespace InertialNetcodeDemo
{
    /// <summary>
    /// Invisible authority. Analytic derivatives are exposed for inspection only;
    /// the clients receive position/time snapshots, never these derivatives.
    /// Place this on a root GameObject without a Renderer or Rigidbody.
    /// </summary>
    [DefaultExecutionOrder(-300)]
    [DisallowMultipleComponent]
    public sealed class TargetMovement : MonoBehaviour
    {
        [SerializeField] private Vector3 center = new Vector3(0f, 0.5f, 0f);
        [SerializeField, Min(0.01f)] private float radiusX = 6f;
        [SerializeField, Min(0.01f)] private float radiusZ = 6f;
        [SerializeField, Min(0.1f)] private float periodSeconds = 8f;
        [SerializeField] private float initialPhaseDegrees;

        public Vector3 Position { get; private set; }
        public Vector3 Velocity { get; private set; }
        public Vector3 Acceleration { get; private set; }

        private double phase;

        private void Awake()
        {
            phase = initialPhaseDegrees * Math.PI / 180.0;
            Evaluate();
        }

        private void Update()
        {
            // Accumulate scaled simulation time, not a frame counter. Double
            // precision and phase wrapping avoid drift in long-running demos.
            phase = (phase + AngularSpeed * Time.deltaTime) % (2.0 * Math.PI);
            Evaluate();
        }

        private double AngularSpeed => 2.0 * Math.PI / Math.Max(0.1, periodSeconds);

        private void Evaluate()
        {
            float w = (float)AngularSpeed;
            float c = (float)Math.Cos(phase);
            float s = (float)Math.Sin(phase);
            Position = center + new Vector3(radiusX * c, 0f, radiusZ * s);
            Velocity = new Vector3(-radiusX * w * s, 0f, radiusZ * w * c);
            Acceleration = new Vector3(-radiusX * w * w * c, 0f, -radiusZ * w * w * s);
            transform.position = Position;
        }

        private void OnValidate()
        {
            radiusX = Mathf.Max(0.01f, radiusX);
            radiusZ = Mathf.Max(0.01f, radiusZ);
            periodSeconds = Mathf.Max(0.1f, periodSeconds);
        }
    }
}


