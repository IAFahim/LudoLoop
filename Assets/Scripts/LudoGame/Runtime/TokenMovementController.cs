using UnityEngine;

public class TokenMovementController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float maxSpeed = 5f;
    [SerializeField] private float arrivalThreshold = 0.1f;
    
    [Header("PID Controller")]
    [SerializeField] private float proportionalGain = 2f;
    [SerializeField] private float integralGain = 0.5f;
    [SerializeField] private float derivativeGain = 1f;
    
    [Header("Height Control")]
    [SerializeField] private float hoverHeight = 0.5f;
    [SerializeField] private bool maintainHeight = true;
    
    private Rigidbody rb;
    private Vector3 targetPosition;
    private Vector3 errorIntegral;
    private Vector3 lastError;
    private bool hasTarget;
    private bool isMoving;
    
    public bool IsMoving => isMoving;
    public bool HasReachedTarget => !isMoving && hasTarget;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError($"TokenMovementController requires a Rigidbody component on {gameObject.name}");
        }
        
        // Configure rigidbody for smooth movement
        rb.useGravity = !maintainHeight;
        rb.linearDamping = 1f;
        rb.angularDamping = 0.5f;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
    }

    private void FixedUpdate()
    {
        if (!hasTarget || rb == null) return;

        Vector3 currentPos = transform.position;
        Vector3 error = targetPosition - currentPos;
        
        // Check if we've reached the target
        if (error.magnitude < arrivalThreshold)
        {
            isMoving = false;
            rb.linearVelocity = Vector3.zero;
            errorIntegral = Vector3.zero;
            lastError = Vector3.zero;
            return;
        }

        isMoving = true;

        // PID calculations
        errorIntegral += error * Time.fixedDeltaTime;
        Vector3 errorDerivative = (error - lastError) / Time.fixedDeltaTime;
        lastError = error;

        // PID output
        Vector3 pidOutput = 
            proportionalGain * error + 
            integralGain * errorIntegral + 
            derivativeGain * errorDerivative;

        // Clamp to max speed
        Vector3 desiredVelocity = Vector3.ClampMagnitude(pidOutput, maxSpeed);
        
        // Apply force to reach desired velocity
        Vector3 velocityChange = desiredVelocity - rb.linearVelocity;
        rb.AddForce(velocityChange, ForceMode.VelocityChange);

        // Maintain height if enabled
        if (maintainHeight)
        {
            float heightError = hoverHeight - currentPos.y;
            float heightForce = heightError * proportionalGain * 2f;
            rb.AddForce(Vector3.up * heightForce, ForceMode.Acceleration);
        }
    }

    /// <summary>
    /// Sets a new target position for the token to move towards
    /// </summary>
    public void SetTargetPosition(Vector3 target)
    {
        targetPosition = target;
        hasTarget = true;
        isMoving = true;
        
        // Reset PID controller
        errorIntegral = Vector3.zero;
        lastError = targetPosition - transform.position;
    }

    /// <summary>
    /// Sets multiple waypoints for the token to follow in sequence
    /// </summary>
    public void SetPath(Vector3[] waypoints)
    {
        if (waypoints == null || waypoints.Length == 0) return;
        
        // For now, just set the first waypoint
        // You can extend this to handle multiple waypoints with coroutines
        SetTargetPosition(waypoints[0]);
    }

    /// <summary>
    /// Stops movement and clears the target
    /// </summary>
    public void Stop()
    {
        hasTarget = false;
        isMoving = false;
        rb.linearVelocity = Vector3.zero;
        errorIntegral = Vector3.zero;
        lastError = Vector3.zero;
    }

    /// <summary>
    /// Instantly teleports to target (for initialization)
    /// </summary>
    public void TeleportToPosition(Vector3 position)
    {
        Stop();
        transform.position = position;
        targetPosition = position;
    }

    private void OnDrawGizmos()
    {
        if (hasTarget)
        {
            Gizmos.color = isMoving ? Color.yellow : Color.green;
            Gizmos.DrawLine(transform.position, targetPosition);
            Gizmos.DrawWireSphere(targetPosition, 0.2f);
        }
    }
}