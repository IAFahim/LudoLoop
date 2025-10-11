using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace DiceRoll.Runtime
{
    [RequireComponent(typeof(Rigidbody))]
    public class DiceRoller : MonoBehaviour
    {
        [Header("Roll Settings")] 
        [SerializeField] private float rollForce = 5f;
        [SerializeField] private float torqueForce = 10f;
        [SerializeField] private float upwardForce = 3f;
        [SerializeField] private float rollHeight = 2f;
        [SerializeField] private Vector3 direction;
        [SerializeField] private bool initRoll;
        
        
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
            AddForceInDirection(direction);
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

        private void AddForceInDirection(Vector3 rollDirection)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            transform.rotation = Random.rotation;

            Vector3 force = rollDirection * rollForce + Vector3.up * upwardForce;
            rb.AddForce(force, ForceMode.Impulse);

            Vector3 randomTorque = new Vector3(
                Random.Range(-1f, 1f),
                Random.Range(-1f, 1f),
                Random.Range(-1f, 1f)
            ).normalized * torqueForce;

            rb.AddTorque(randomTorque, ForceMode.Impulse);
        }
    }
}