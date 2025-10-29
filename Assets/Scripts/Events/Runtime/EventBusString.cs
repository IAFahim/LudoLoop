using UnityEngine;

namespace Events
{
    [CreateAssetMenu(fileName = "StringEvent", menuName = "EventBus/String")]
    public class EventBusString : EventBus<string>
    {
    }
}