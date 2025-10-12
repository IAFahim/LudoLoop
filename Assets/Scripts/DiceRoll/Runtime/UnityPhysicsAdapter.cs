using UnityEngine;

namespace DiceRoll.Runtime
{
    public static class UnityPhysicsAdapter
    {
        public static void ApplyRollForce(Rigidbody rb, Transform transform, RollForceData forceData)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = false;
            // Don't instantly set rotation - let it spin naturally from the starting rotation
            // transform.rotation = forceData.rotation;

            rb.AddForce(forceData.force, ForceMode.Impulse);
            rb.AddTorque(forceData.torque, ForceMode.Impulse);
        }

        public static void SetKinematic(Rigidbody rb, Transform transform, Vector3 position, Quaternion rotation)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
            transform.position = position;
            transform.rotation = rotation;
        }

        public static void SetVelocity(Rigidbody rb, Vector3 velocity)
        {
            rb.linearVelocity = velocity;
        }

        public static PhysicsData GetPhysicsData(Rigidbody rb, Transform transform)
        {
            return new PhysicsData
            {
                position = transform.position,
                rotation = transform.rotation,
                velocity = rb.linearVelocity,
                angularVelocity = rb.angularVelocity
            };
        }
    }
}