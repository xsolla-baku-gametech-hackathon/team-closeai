using UnityEngine;

namespace InertialNetcodeDemo
{
    [DisallowMultipleComponent]
    public sealed class RotateEarth : MonoBehaviour
    {
        private void Update()
        {
            transform.Rotate(Vector3.up, 20f * Time.deltaTime, Space.Self);
        }
    }
}
