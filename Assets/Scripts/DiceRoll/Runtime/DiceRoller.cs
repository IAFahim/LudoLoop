using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace DiceRoll.Runtime
{
    [RequireComponent(typeof(Rigidbody))]
    public class DiceRoller : MonoBehaviour
    {
        [Header("Roll Settings")] [SerializeField]
        private bool initRoll;

        [SerializeField] private bool random = true;
        [SerializeField] private RollConfig rollConfig = RollConfig.Default();

        [Header("Physics Settings")] [SerializeField]
        private Rigidbody rb;

        [Header("Debug")] [SerializeField] private bool showDebug = true;
        [SerializeField] private Vector3 restPosition;

        private void OnValidate()
        {
            rb ??= GetComponent<Rigidbody>();
        }

        [ContextMenu("Roll")]
        public void Roll()
        {
            PlaceAgain();
            if (random)
            {
                RandomPhysis.DirectionTorque(out rollConfig.rotation, out rollConfig.torque);
            }

            AddForceInDirection(transform, rb,
                rollConfig.rollForce, rollConfig.upwardForce, rollConfig.torqueForce,
                rollConfig.direction, rollConfig.rotation, rollConfig.torque
            );
        }

        [ContextMenu("PlaceAgain")]
        public void PlaceAgain()
        {
            transform.position = restPosition;
        }

        private void FixedUpdate()
        {
            if (initRoll)
            {
                Roll();
                initRoll = false;
            }
        }

        private void AddForceInDirection(
            Transform transform, Rigidbody rb,
            in float rollForce, in float upwardForce, in float torqueForce,
            in Vector3 rollDirection, in Quaternion rotation, in Vector3 torqueDirection
        )
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            transform.rotation = rotation;
            var force = rollDirection * rollForce + Vector3.up * upwardForce;
            rb.AddForce(force, ForceMode.Impulse); 
            var torque = torqueDirection * torqueForce;
            rb.AddTorque(torque, ForceMode.Impulse);
        }
    }

    [Serializable]
    public struct RollConfig
    {
        public float rollForce;
        public float torqueForce;
        public float upwardForce;

        public Vector3 direction;
        public Quaternion rotation;
        public Vector3 torque;

        public static RollConfig Default()
        {
            return new RollConfig()
            {
                rollForce = 15,
                torqueForce = 2,
                upwardForce = 3,
                direction = new Vector3(0, 0, 1),
                rotation = Quaternion.identity,
                torque = Vector3.zero
            };
        }
    }
    
    public static class RandomPhysis{
        public static void DirectionTorque(out Quaternion rotation, out Vector3 torque)
        {
            rotation = Random.rotation;
            torque = new Vector3(
                Random.Range(-1f, 1f),
                Random.Range(-1f, 1f),
                Random.Range(-1f, 1f)
            ).normalized;
        }
    }
}