using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace DiceRoll.Runtime
{
    [RequireComponent(typeof(Rigidbody))]
    public class DiceRoller : MonoBehaviour
    {
        public int targetSeed;

        [Header("State")] public RollState rollState = RollState.Default();
        public Dice dice = Dice.Default();

        [Header("Configuration")] public RandomRollConfig randomRollConfig = RandomRollConfig.Default();
        [FormerlySerializedAs("settledThresholds")] public TurnSettings turnSettings = TurnSettings.Default();
        public Vector3 handPosition;
        public Quaternion handRotation = Quaternion.identity;

        


        private Rigidbody rb;
        private IRandomGenerator randomGenerator;

        private void OnValidate()
        {
            if (handPosition == Vector3.zero)
            {
                handPosition = transform.position;
            }
        }

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            randomGenerator = new UnityRandomGenerator();
        }

        // Allow injection for testing
        public void SetRandomGenerator(IRandomGenerator generator)
        {
            randomGenerator = generator;
        }

        private void FixedUpdate()
        {
            switch (rollState.state)
            {
                case DiceState.InHand:
                    HandleInHandState();
                    break;

                case DiceState.Throwing:
                    HandleThrowingState();
                    break;

                case DiceState.Rolling:
                    HandleRollingState();
                    break;

                case DiceState.Settled:
                    HandleSettledState();
                    break;

                case DiceState.ReturningToHand:
                    HandleReturningState();
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void HandleInHandState()
        {
            UnityPhysicsAdapter.SetKinematic(rb, transform, handPosition, handRotation);
        }

        private void HandleThrowingState()
        {
            RollForceData forceData = DiceRollLogic.GenerateRollForce(
                randomRollConfig,
                targetSeed,
                randomGenerator
            );

            UnityPhysicsAdapter.ApplyRollForce(rb, transform, forceData);

            rollState.state = DiceState.Rolling;
            rollState.settledTimer = 0f;
        }

        private void HandleRollingState()
        {
            PhysicsData physicsData = UnityPhysicsAdapter.GetPhysicsData(rb, transform);

            if (DiceRollLogic.IsSettled(physicsData, turnSettings))
            {
                rollState.settledTimer += Time.fixedDeltaTime;

                if (rollState.settledTimer >= turnSettings.settledTime)
                {
                    dice.currentSide = DiceRollLogic.DetectTopFace(physicsData.rotation);
                    rollState.state = DiceState.Settled;
                    rollState.returnTimer = 0f;

                    Debug.Log($"Dice settled on: {dice.currentSide} (Value: {(int)dice.currentSide})");
                }
            }
            else
            {
                rollState.settledTimer = 0f;
            }
        }

        private void HandleSettledState()
        {
            rollState.returnTimer += Time.fixedDeltaTime;

            if (rollState.returnTimer >= turnSettings.returnDelay)
            {
                rollState.state = DiceState.ReturningToHand;
                rb.isKinematic = false;
            }
        }

        private void HandleReturningState()
        {
            PhysicsData physicsData = UnityPhysicsAdapter.GetPhysicsData(rb, transform);
            
            float distanceToHand = Vector3.Distance(physicsData.position, handPosition);
            
            if (distanceToHand < 0.1f)
            {
                rollState.state = DiceState.InHand;
                return;
            }

            Vector3 direction = (handPosition - physicsData.position).normalized;
            float step = turnSettings.returnSpeed * Time.fixedDeltaTime;
            Vector3 newPosition = physicsData.position + direction * step;
            
            rb.MovePosition(newPosition);
            
            Quaternion targetRotation = Quaternion.Slerp(physicsData.rotation, handRotation, 5f * Time.fixedDeltaTime);
            rb.MoveRotation(targetRotation);
        }

        public void ThrowDiceWithSeed(int seed)
        {
            if (rollState.state == DiceState.InHand)
            {
                targetSeed = seed;
                rollState.state = DiceState.Throwing;
            }
        }

        public void ForceReturnToHand()
        {
            rollState.state = DiceState.ReturningToHand;
            rb.isKinematic = false;
        }

        public void ThrowDiceLocal()
        {
            ThrowDiceWithSeed(Environment.TickCount);
        }
    }
}