using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace DiceRoll.Runtime
{
    [Serializable]
    public struct RollConfig
    {
        public float rollForce;
        public float torqueForce;
        public float upwardForce;
        public Vector3 direction;
        public Quaternion rotation;
        public Vector3 torque;

        public static RollConfig Default() => new()
        {
            rollForce = 15f,
            torqueForce = 2f,
            upwardForce = 3f,
            direction = new(0, 0, 1),
            rotation = Quaternion.identity,
            torque = Vector3.zero
        };
    }

    public static class DicePhysics
    {
        public static void ApplyRollForce(
            Rigidbody rb,
            Transform transform,
            in RollConfig config)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            transform.rotation = config.rotation;
            
            var force = config.direction * config.rollForce + Vector3.up * config.upwardForce;
            rb.AddForce(force, ForceMode.Impulse);
            
            var torque = config.torque * config.torqueForce;
            rb.AddTorque(torque, ForceMode.Impulse);
        }

        public static RollConfig RandomizeConfig(RollConfig config)
        {
            config.rotation = Random.rotation;
            config.torque = new Vector3(
                Random.Range(-1f, 1f),
                Random.Range(-1f, 1f),
                Random.Range(-1f, 1f)
            ).normalized;
            return config;
        }
    }

    [RequireComponent(typeof(Rigidbody))]
    public class DiceRoller : MonoBehaviour
    {
        [SerializeField] private bool roll = true;
        [SerializeField] private bool randomize = true;
        [SerializeField] private Vector3 spawnPosition;
        [SerializeField] private RollConfig rollConfig = RollConfig.Default();

        private Rigidbody rb;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            spawnPosition = transform.position;
        }

        private void FixedUpdate()
        {
            ResetPosition();
            if (roll)
            {
                Roll();
                roll = false;
            }
        }

        public void Roll()
        {
            ResetPosition();
            var config = randomize ? DicePhysics.RandomizeConfig(rollConfig) : rollConfig;
            DicePhysics.ApplyRollForce(rb, transform, config);
        }

        public void ResetPosition()
        {
            transform.position = spawnPosition;
        }
    }
}