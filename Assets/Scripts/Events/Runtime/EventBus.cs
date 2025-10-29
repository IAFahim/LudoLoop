using System;
using UnityEngine;

namespace Events
{
    public class EventBus <T>: ScriptableObject
    {
        [SerializeField] public T value;
        public event Action<T> OnSelectionChanged;

        public void Publish(T data)
        {
            value = data;
            OnSelectionChanged?.Invoke(value);
        }
    }
}