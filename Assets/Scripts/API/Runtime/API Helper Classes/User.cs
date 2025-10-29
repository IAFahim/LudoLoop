using System;

[Serializable]
public class User
{
    public int id;              // Unique identifier (integer)
    public string name;         // User's name
    public string email;        // User's email
    public string user_id;      // Additional user ID (string, possibly from API)
    public string photo;        // URL or path to user's photo
    public int coins;           // User's coin balance

    // Default constructor (required for JSON deserialization)
    public User() { }

    // Parameterized constructor for basic initialization
    public User(int id, string name, string email, string user_id)
    {
        this.id = id;
        this.name = name;
        this.email = email;
        this.user_id = user_id;
        this.photo = string.Empty; // Default to empty if not provided
        this.coins = 0;           // Default to 0 if not provided
    }

    // Copy constructor to create a new instance from existing data
    public User(User data)
    {
        id = data.id;
        name = data.name;
        email = data.email;
        user_id = data.user_id;
        photo = data.photo;
        coins = data.coins;
    }

    // Optional: Method to check if user data is valid
    public bool IsValid()
    {
        return !string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(user_id) && id > 0;
    }

    // Optional: Override ToString for debugging
    public override string ToString()
    {
        return $"User[id={id}, name={name}, user_id={user_id}, email={email}, coins={coins}, photo={photo}]";
    }
}