using System;
using Events;
using TMPro;
using UnityEngine;

namespace UI.Runtime
{
    public class MainPlayerDataView : MonoBehaviour
    {
        [SerializeField] private TMP_Text userName;
        [SerializeField] private TMP_Text coin;
        [SerializeField] private EventBusString eventBusString;

        private void OnEnable()
        {
            eventBusString.OnSelectionChanged += EventBusStringOnOnSelectionChanged;
            SetUI();
        }

        private void OnDisable()
        {
            eventBusString.OnSelectionChanged -= EventBusStringOnOnSelectionChanged;
        }

        private void EventBusStringOnOnSelectionChanged(string _)
        {
            gameObject.SetActive(false);
        }

        private void SetUI()
        {
            var currentUserName = DataManager.Instance.CurrentUser.name;
            var instanceCoins = DataManager.Instance.Coins;
            SetUI(currentUserName, instanceCoins.ToString());
        }

        public void SetUI(string user, string coinCount)
        {
            userName.text = user;
            coin.text = coinCount;
        }
    }
}