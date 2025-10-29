using System;
using System.Threading.Tasks;
using Events;
using SecureDataSaver;
using TMPro;
using UnityEngine;

namespace UI.Runtime
{
    public class UIManage : MonoBehaviour
    {
        [Header("Common Section"), Space(20)]
        [SerializeField] private UserType userType;
        
        private const string nameParam = "name";
        private const string emailParam = "email";
        private const string passwordParam = "password";
        private const string passwordConfirmParam = "password_confirmation";
        private const string userTypeParam = "user_type";
        private const string gameNameParam = "game_name";
        private const string userIdParam = "user_id";
        private const string referralIdParam = "referral_id";
        private const string gameIdParam = "game_id";


        [SerializeField] private EventBusString eventBusString;
        [SerializeField] private TMP_InputField logInEmailField;
        [SerializeField] private TMP_InputField logInPasswordField;

        public void Login()
        {
            switch (userType)
            {
                case UserType.APP:
                    LogInForAppUser();
                    break;
                case UserType.NORMAL:
                    LogInForGeneralMember();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void LogInForAppUser()
        {
            APIHandler.Instance.AppLogInPostRequest(CommonLogInTasks(), OnLogInSuccess, OnLogInFailed);
        }

        private void LogInForGeneralMember()
        {
            APIHandler.Instance.GameLogInPostRequest(CommonLogInTasks(), OnLogInSuccess, OnLogInFailed);
        }

        private WWWForm CommonLogInTasks()
        {
            WWWForm wwwForm = new WWWForm();
            wwwForm.AddField(emailParam, logInEmailField.text);
            wwwForm.AddField(passwordParam, logInPasswordField.text);
            wwwForm.AddField(userTypeParam, Enum.GetName(typeof(UserType), userType));
            wwwForm.AddField(gameIdParam, DataManager.Instance.GameId);

            return wwwForm;
        }

        private void OnLogInSuccess(string jsonData)
        {
            ApiResponse response = JsonUtility.FromJson<ApiResponse>(jsonData);

            if (userType == UserType.NORMAL)
            {
                response.user = new User
                {
                    email = logInEmailField.text,
                    name = logInEmailField.text.Split('@')[0]
                };

                Debug.Log($"Email: {response.user.email}, ID: {logInEmailField.text}");
            }

            DataManager.Instance.SetCurrentUser(response);

            if (DataManager.Instance.CurrentUserType == UserType.APP)
            {
                APIHandler.Instance.GetUserTotalCoin(DataManager.Instance.Token, res =>
                {
                    CoinResponse coinResponse = JsonUtility.FromJson<CoinResponse>(res);

                    if (coinResponse != null)
                    {
                        DataManager.Instance.SetCoins(Convert.ToInt32(coinResponse.coin));
                        response.user.coins = DataManager.Instance.Coins;
                        Debug.Log($"Coin Res: {res}");
                        return;
                    }

                    Debug.LogError($"Response: {res}");
                    eventBusString.Publish("Something went wrong, please try again");
                });

                APIHandler.Instance.DownloadImageAsync(response.user.photo, texture2D =>
                {
                    if (texture2D == null) return;
                    
                    // Ensure texture is valid before encoding
                    byte[] imageData = texture2D.EncodeToJPG();
                    if (imageData == null || imageData.Length == 0)
                    {
                        Debug.LogError("Encoded image data is empty, skipping save.");
                        return;
                    }

                    // Sanitize file name
                    string safeFileName = response.user.email.Replace("@", "_").Replace(".", "_") + "_photo";

                    Debug.Log($"Safe FileName: {safeFileName}");

                    // Delay before writing to avoid race conditions
                    Task.Run(async () =>
                    {
                        await Task.Delay(100);
                        DataSaver.WriteAllBytes(imageData, safeFileName);
                    });

                });

                APIHandler.Instance.GetConfig(DataManager.Instance.Token, configResponse =>
                {
                    MessageResponse config = JsonUtility.FromJson<MessageResponse>(configResponse);

                    if (configResponse == null || !config.status)
                        return;

                    DataManager.Instance.SetFeePercentage(Convert.ToInt32(config.message));
                    PlayerPrefs.SetInt(string.Concat(response.user.email, "_config"), Convert.ToInt32(config.message));
                });
            }

            Debug.Log($"Res: {JsonUtility.ToJson(response)}");
            PlayerPrefs.SetString("userData", response.user.email);
            DataSaver.WriteData(response, response.user.email);
        }
        
        private void OnLogInFailed(string message)
        {
            try
            {
                MessageResponse response = JsonUtility.FromJson<MessageResponse>(message);
            
                CloseLoadingPanel();

                eventBusString.Publish(response != null ? response.message : message);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                CloseLoadingPanel();
                eventBusString.Publish(message);
            }
        }

        private void CloseLoadingPanel()
        {
            
        }
    }
}