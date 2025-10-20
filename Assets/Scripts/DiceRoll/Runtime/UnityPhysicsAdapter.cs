using UnityEngine;

namespace DiceRoll.Runtime
{
    public static class UnityPhysicsAdapter
    {
        public static void ApplyRollForce(Rigidbody rb, RollForceData forceData)
        {
            rb.isKinematic = false;

            rb.AddForce(forceData.force, ForceMode.Impulse);
            rb.AddTorque(forceData.torque, ForceMode.Impulse);
        }

        public static void SetKinematic(Rigidbody rb, Transform transform, in Vector3 position, in Quaternion rotation)
        {
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