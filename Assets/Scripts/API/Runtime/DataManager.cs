using System;
using UnityEngine;
using UnityEngine.Events;

public class DataManager : MonoBehaviour
{
    private static DataManager instance;
    public static DataManager Instance => instance;

    public BoardGraphics[] boardGraphics;

    [field: SerializeField] public RoomType CurrentRoomType { get; private set; }
    [field: SerializeField] public RoomMode CurrentRoomMode { get; private set; }
    [field: SerializeField] public string Token { get; private set; }

    [field: SerializeField] public readonly string GameId = "1";

    [field: SerializeField] public string SessionId { get; private set; }
    [field: SerializeField] public User CurrentUser { get; private set; }

    [field: SerializeField] public UserType CurrentUserType { get; private set; }
    [field: SerializeField] public DiceColor OwnDiceColor { get; private set; }
    [field: SerializeField] public TeamColor OpponentTeamColor { get; private set; }
    [field: SerializeField] public byte MaxPlayerNumberForCurrentBoard { get; private set; }

    [field: SerializeField] public int Coins { get; private set; }

    [field: SerializeField] public int CurrentEntryFee { get; private set; }

    [field: SerializeField] public GameType GameType { get; private set; }

    [field: SerializeField] public bool IsLoggedIn { get; private set; }

    public int defaultCoin = 15000;
    [field: SerializeField] public int FeePercentage { get; private set; }

    [field: SerializeField] private Sprite AvatarSprite { get; set; }
    [field: SerializeField] public GameState CurrentGameState { get; private set; }

    [field: SerializeField] public DiceColor ActiveDiceColor { get; private set; }
    [field: SerializeField]public bool IsMyTurn => ActiveDiceColor == OwnDiceColor;
    [SerializeField] private int maxTurnTime = 30;
    [SerializeField] private int maxTurnCanIgnore = 3;
    [SerializeField] private int maxNumberOfFixTurns = 10;
    public int MaxTurnTime => maxTurnTime;
    public int MaxTurnCanIgnore => maxTurnCanIgnore;
    public int MaxNumberOfFixTurns => maxNumberOfFixTurns;

    public UnityEvent onLoggedIn;
    public UnityEvent onSuccessfulnessesResponse;
    
    public LocalRotation LocalRotation { get; private set; }
    
    // Key for storing data in PlayerPrefs
    private const string GameDataKey = "GameData";

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            var tryLoadAtStartup = TryLoadAtStartup();
            if (tryLoadAtStartup)
            {
                onLoggedIn.Invoke();
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    /// <summary>
    /// Loads the game data from PlayerPrefs.
    /// </summary>
    public bool TryLoadAtStartup()
    {
        if (PlayerPrefs.HasKey(GameDataKey))
        {
            string json = PlayerPrefs.GetString(GameDataKey);
            GameData data = JsonUtility.FromJson<GameData>(json);

            // Restore the data
            Token = data.Token;
            CurrentUser = data.CurrentUser;
            Coins = data.Coins;
            IsLoggedIn = data.IsLoggedIn;
            return true;
            Debug.Log("Game data loaded successfully.");
        }
        
        SetCoins(defaultCoin);
        Debug.Log("No saved data found. Initializing with default values.");

        return false;
    }

    /// <summary>
    /// Saves the current game data to PlayerPrefs.
    /// </summary>
    public void Save()
    {
        GameData data = new GameData
        {
            Token = this.Token,
            CurrentUser = this.CurrentUser,
            Coins = this.Coins,
            IsLoggedIn = this.IsLoggedIn
        };

        string json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString(GameDataKey, json);
        PlayerPrefs.Save(); // Ensure data is written to disk
        
        Debug.Log("Game data saved successfully.");
    }

    /// <summary>
    /// Deletes all saved game data and resets the DataManager.
    /// </summary>
    public void ForgetAll()
    {
        PlayerPrefs.DeleteKey(GameDataKey);
        PlayerPrefs.Save(); // Ensure the deletion is written to disk

        // Reset runtime data to initial state
        Token = null;
        CurrentUser = null;
        IsLoggedIn = false;
        SetCoins(defaultCoin);
        
        Debug.Log("All game data has been forgotten.");
    }
    
    // This is called when the application is about to quit.
    private void OnApplicationQuit()
    {
        Save();
    }

    public void ReInitialize()
    {
        instance = null;
        Awake();
    }
    
    public LocalRotation SetLocalRotation(LocalRotation localRotation) => LocalRotation = localRotation;

    public void SetCurrentUser(ApiResponse response)
    {
        CurrentUser = new User(response.user);
        Token = response.token;
        IsLoggedIn = true;
        SetOwnDiceColor(DiceColor.Unknown);
        onSuccessfulnessesResponse.Invoke();
        Save(); // Save after setting the current user
    }

    public void SetPlayerAvatar(Sprite sprite)
    {
        AvatarSprite = sprite;
    }

    public Sprite GetDefaultAvatarSprite()
    {
        return AvatarSprite;
    }

    public void SetCoins(int coin)
    {
        Coins = coin;
        if (CurrentUser != null)
        {
            CurrentUser.coins = Coins;
        }
    }

    public void SetFeePercentage(int value)
    {
        FeePercentage = value;
    }

    public int GetPercentage(int boardFees)
    {
        if (FeePercentage <= 0)
            return 0;

        return (FeePercentage * boardFees) / 100;
    }

    public void SetOwnDiceColor(DiceColor diceColor)
    {
        OwnDiceColor = diceColor;
        //OpponentTeamColor = (diceColor == TeamColor.Blue) ? TeamColor.Red : TeamColor.Blue;
    }

    public void SetActiveDiceColor(DiceColor diceColor) => ActiveDiceColor = diceColor;

    public void SetMaxPlayerNumberForCurrentBoard(byte maxPlayerNumber) => MaxPlayerNumberForCurrentBoard = maxPlayerNumber;

    public void SetCurrentEntryFees(int entryFees) => CurrentEntryFee = entryFees;

    public void SetGameType(GameType gameType) => GameType = gameType;

    public void SetCurrentGameState(GameState gameState) => CurrentGameState = gameState;

    public void SetSessionId(string session) => SessionId = session;

    public void SetAccessToken(string token) => Token = token;

    public void SetCurrentRoomType(RoomType type) => CurrentRoomType = type;

    public void SetCurrentUserType(UserType type) => CurrentUserType = type;

    public void SetCurrentRoomMode(RoomMode mode) => CurrentRoomMode = mode;
    
    public void ReduceNormalUserCoins(int coin, bool saveCoin = true)
    {
        UpdateNormalUserCoins(coin * -1, saveCoin);
    }

    public void UpdateNormalUserCoins(int coins, bool saveCoin = true)
    {
        int prevCoins = Coins;

        Coins = Mathf.Max(0, Coins + coins);
        CurrentUser.coins = Coins;

        string key = CurrentUser.email + "_coins";

        PlayerPrefs.SetInt(key, Coins);
        Debug.Log($"Param: {coins}, TotalCon: {Coins}, PrevCoins: {prevCoins}, UserCoin: {CurrentUser.coins}, Key: {key}");

        if (saveCoin)
        {
            Save(); // Use the new centralized save method
        }
    }
    
    public void UpdateAppUserCoin(int coins)
    {
        int prevCoin = Coins;
        Coins = Mathf.Max(0, Coins + coins);
        CurrentUser.coins = Coins;
        Debug.Log($"Param: {coins}, TotalCon: {Coins}, PrevCoins: {prevCoin}, UserCoin: {CurrentUser.coins}, AppUserCoin");
        
        Save(); // Save changes
    }

    public void SetCurrentEntryFeeFromFormattedValue(string value)
    {
        CurrentEntryFee = GetIntValueFromFormattedPrizeValue(value);
    }
    public int GetIntValueFromFormattedPrizeValue(string value)
    {
        return value switch
        {
            "5K" => 5000,
            "7K" => 7000,
            "10K" => 10000,
            "20K" => 20000,
            "50K" => 50000,
            "100K" => 100000,
            "200K" => 200000,
            "5M" => 5000000,
            "10M" => 10000000,
            "20M" => 20000000,
            "50M" => 50000000,
            _ => 5000
        };
    }

    public void ResetCurrentMatchData()
    {
        CurrentRoomType = RoomType.Null;
        CurrentRoomMode = RoomMode.Null;
        CurrentEntryFee = MaxPlayerNumberForCurrentBoard = 0;
        GameType = GameType.Null;
        CurrentGameState = GameState.Init;
        ActiveDiceColor = DiceColor.Unknown;
        LocalRotation = LocalRotation.Null;
        SessionId = string.Empty;
    }
}

[Serializable]
public struct BoardGraphics
{
    public Sprite boardSprite;
    public Sprite redPieceSprite;
    public Sprite bluePieceSprite;
    public Sprite yellowPieceSprite;
    public Sprite greenPieceSprite;
    public Sprite redBlinkSprite;
    public Sprite blueBlinkSprite;
    public Sprite yellowBlinkSprite;
    public Sprite greenBlinkSprite;
}

[Serializable]
public class GameData
{
    public string Token;
    public User CurrentUser;
    public int Coins;
    public bool IsLoggedIn;
    
    // Add any other fields from DataManager you want to persist here
}