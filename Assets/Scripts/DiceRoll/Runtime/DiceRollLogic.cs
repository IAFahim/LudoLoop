using UnityEngine;

namespace DiceRoll.Runtime
{
    public static class DiceRollLogic
    {
        public static RollForceData GenerateRollForce(
            RandomRollConfig config,
            int seed,
            IRandomGenerator random)
        {
            random.SetSeed(seed);

            float rollForce = random.Range(config.rollForceLimit.x, config.rollForceLimit.y);
            float torqueForce = random.Range(config.torqueForceLimit.x, config.torqueForceLimit.y);
            float upwardForce = random.Range(config.upwardForceLimit.x, config.upwardForceLimit.y);

            Vector3 randomTorque = random.InsideUnitSphere();
            Quaternion randomRotation = random.Rotation();

            Vector3 force = config.throwDirection.normalized * rollForce + Vector3.up * upwardForce;
            Vector3 torque = randomTorque * torqueForce;

            return new RollForceData
            {
                force = force,
                torque = torque,
                rotation = randomRotation
            };
        }

        public static bool IsSettled(PhysicsData physicsData, TurnSettings thresholds)
        {
            return physicsData.velocity.magnitude < thresholds.velocityThreshold &&
                   physicsData.angularVelocity.magnitude < thresholds.angularVelocityThreshold;
        }

        public static DiceSides DetectTopFace(Quaternion rotation)
        {
            // Calculate which local axis points upward most
            Vector3 up = rotation * Vector3.up;
            Vector3 down = rotation * Vector3.down;
            Vector3 forward = rotation * Vector3.forward;
            Vector3 back = rotation * Vector3.back;
            Vector3 right = rotation * Vector3.right;
            Vector3 left = rotation * Vector3.left;

            float maxDot = float.MinValue;
            DiceSides topSide = DiceSides.Top;

            if (Vector3.Dot(up, Vector3.up) > maxDot)
            {
                maxDot = Vector3.Dot(up, Vector3.up);
                topSide = DiceSides.Top;
            }

            if (Vector3.Dot(down, Vector3.up) > maxDot)
            {
                maxDot = Vector3.Dot(down, Vector3.up);
                topSide = DiceSides.Bottom;
            }

            if (Vector3.Dot(forward, Vector3.up) > maxDot)
            {
                maxDot = Vector3.Dot(forward, Vector3.up);
                topSide = DiceSides.Front;
            }

            if (Vector3.Dot(back, Vector3.up) > maxDot)
            {
                maxDot = Vector3.Dot(back, Vector3.up);
                topSide = DiceSides.Back;
            }

            if (Vector3.Dot(right, Vector3.up) > maxDot)
            {
                maxDot = Vector3.Dot(right, Vector3.up);
                topSide = DiceSides.Right;
            }

            if (Vector3.Dot(left, Vector3.up) > maxDot)
            {
                topSide = DiceSides.Left;
            }

            return topSide;
        }

        public static Vector3 CalculateReturnVelocity(
            Vector3 currentPosition,
            Vector3 targetPosition,
            float speed,
            float arrivalThreshold = 0.1f)
        {
            Vector3 direction = (targetPosition - currentPosition);
            float distance = direction.magnitude;

            if (distance > arrivalThreshold)
            {
                return direction.normalized * speed;
            }

            return Vector3.zero;
        }
    }
}