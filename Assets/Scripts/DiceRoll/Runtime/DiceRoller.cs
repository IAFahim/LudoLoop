using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace DiceRoll.Runtime
{
    [RequireComponent(typeof(Rigidbody))]
    public class DiceRoller : MonoBehaviour
    {
        public int targetSeed;

        [Header("State")] public RollState rollState = RollState.Default();
        public Dice dice = Dice.Default();

        [Header("Configuration")] public RandomRollConfig randomRollConfig = RandomRollConfig.Default();
        public TurnSettings turnSettings = TurnSettings.Default();
        public HandConfig handConfig = HandConfig.Default();

        public UnityEvent<byte> OnDiceSettled;

        public Transform dropDirectionTarget;


        private Rigidbody rb;
        private IRandomGenerator randomGenerator;

        private void OnValidate()
        {
            if (!Application.isPlaying) handConfig.handPosition = transform.position;
            if (dropDirectionTarget != null)
            {
                randomRollConfig.throwDirection = (dropDirectionTarget.position - transform.position).normalized;
            }
        }

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            randomGenerator = new UnityRandomGenerator();
        }

        public void SetRandomGenerator(IRandomGenerator generator)
        {
            randomGenerator = generator;
        }

        private void Update()
        {
            if (Keyboard.current.anyKey.wasPressedThisFrame)
            {
                switch (rollState.state)
                {
                    case DiceState.Settled:
                        rollState.state = DiceState.ReturningToHand;
                        break;
                    default:
                        rollState.state = DiceState.Throwing;
                        break;
                }
            }
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
            UnityPhysicsAdapter.SetKinematic(rb, transform, handConfig.handPosition, handConfig.handRotation);
        }

        private void HandleThrowingState()
        {
            RollForceData forceData = DiceRollLogic.GenerateRollForce(
                randomRollConfig,
                targetSeed,
                randomGenerator
            );

            UnityPhysicsAdapter.ApplyRollForce(rb, forceData);

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

                    var diceValue = (byte)dice.currentSide;
                    OnDiceSettled.Invoke(diceValue);
                    Debug.Log($"Dice settled on: {dice.currentSide} (Value: {diceValue})");
                }
            }
            else
            {
                rollState.settledTimer = 0f;
            }
        }

        private void HandleSettledState()
        {
            rb.isKinematic = false;
        }

        private void HandleReturningState()
        {
            PhysicsData physicsData = UnityPhysicsAdapter.GetPhysicsData(rb, transform);

            float distanceToHand = Vector3.Distance(physicsData.position, handConfig.handPosition);

            if (distanceToHand < 0.1f)
            {
                rollState.state = DiceState.InHand;
                return;
            }

            Vector3 direction = (handConfig.handPosition - physicsData.position).normalized;
            float step = turnSettings.returnSpeed * Time.fixedDeltaTime;
            Vector3 newPosition = physicsData.position + direction * step;

            rb.MovePosition(newPosition);

            Quaternion targetRotation =
                Quaternion.Slerp(physicsData.rotation, handConfig.handRotation, 5f * Time.fixedDeltaTime);
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
        
        #if UNITY_EDITOR

    private void OnDrawGizmosSelected()
    {
        // --- 1. Visualize the Hand Configuration ---
        // We use a cyan color for the "ghost" or configured hand position.
        Gizmos.color = Color.cyan;
        
        // Save the current Gizmo matrix to not affect other Gizmos in the scene.
        Matrix4x4 originalMatrix = Gizmos.matrix;

        // Set the Gizmo's transform to match the hand position and rotation.
        // This makes drawing the wire cube at the correct orientation trivial.
        Gizmos.matrix = Matrix4x4.TRS(handConfig.handPosition, handConfig.handRotation, transform.lossyScale);
        
        // Draw a wire cube representing the dice in its "hand" state.
        Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
        
        // IMPORTANT: Restore the original matrix.
        Gizmos.matrix = originalMatrix;


        // --- 2. Visualize the Throw Trajectory and Force ---
        Gizmos.color = Color.yellow;
        float forceVizScale = 0.05f; // A small scaler to keep the gizmo from being huge.

        // Draw a ray representing the maximum throw force.
        Vector3 throwVectorMax = randomRollConfig.throwDirection.normalized * randomRollConfig.torqueForceLimit.y * forceVizScale;
        Gizmos.DrawRay(handConfig.handPosition, throwVectorMax);
        
        // Draw a smaller, thicker line for the minimum throw force to show the range.
        Gizmos.color = new Color(1f, 0.92f, 0.016f, 0.5f); // Semi-transparent yellow
        Vector3 throwVectorMin = randomRollConfig.throwDirection.normalized * randomRollConfig.torqueForceLimit.x * forceVizScale;
        Gizmos.DrawLine(handConfig.handPosition, handConfig.handPosition + throwVectorMin);


        // --- 3. Visualize the Link to the Drop Target ---
        if (dropDirectionTarget != null)
        {
            // Draw a dotted line to the target to show how the throw direction is being calculated.
            UnityEditor.Handles.color = Color.gray;
            UnityEditor.Handles.DrawDottedLine(transform.position, dropDirectionTarget.position, 5.0f);
        }

        // Only show state-based gizmos when the application is playing.
        if (!Application.isPlaying) return;


        // --- 4. Visualize the Current State with Colors ---
        Color stateColor;
        switch (rollState.state)
        {
            case DiceState.InHand:
                stateColor = new Color(0, 1, 1, 0.25f); // Transparent Cyan
                break;
            case DiceState.Rolling:
                stateColor = new Color(1, 0.92f, 0.016f, 0.25f); // Transparent Yellow
                break;
            case DiceState.Settled:
                stateColor = new Color(0, 1, 0, 0.25f); // Transparent Green
                break;
            case DiceState.ReturningToHand:
                stateColor = new Color(1, 0.5f, 0, 0.25f); // Transparent Orange
                break;
            default:
                stateColor = new Color(1, 1, 1, 0.1f); // Transparent White
                break;
        }

        // Draw a sphere around the actual dice with the color representing its current state.
        Gizmos.color = stateColor;
        Gizmos.DrawSphere(transform.position, 1f);


        // --- 5. Visualize the Settled Result ---
        if (rollState.state == DiceState.Settled)
        {
            // Prepare a style for the text label to make it visible.
            GUIStyle style = new GUIStyle();
            style.normal.textColor = Color.white;
            style.fontSize = 20;
            style.fontStyle = FontStyle.Bold;
            style.alignment = TextAnchor.MiddleCenter;
            
            // Use Handles to draw a text label in the scene view showing the dice result.
            string labelText = $"Result: {(int)dice.currentSide}";
            UnityEditor.Handles.Label(transform.position + Vector3.up * 1.2f, labelText, style);
        }
    }
#endif
    }
}