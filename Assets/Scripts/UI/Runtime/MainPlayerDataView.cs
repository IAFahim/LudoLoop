using System;
using TMPro;
using UnityEngine;

namespace UI.Runtime
{
    public class MainPlayerDataView : MonoBehaviour
    {
        [SerializeField] private TMP_Text userName;
        [SerializeField] private TMP_Text coin;

        private void OnEnable()
        {
            var currentUserName = DataManager.Instance.CurrentUser.name;
            var instanceCoins = DataManager.Instance.CurrentUser.coins;
            SetUI(currentUserName, instanceCoins.ToString());
        }

        public void SetUI(string user, string coinCount)
        {
            userName.text = user;
            coin.text = coinCount;
        }
    }
}