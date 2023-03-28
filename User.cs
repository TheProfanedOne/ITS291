namespace ITS291;

using System.Text.Json.Serialization;
using System.Security.Cryptography;

public sealed class User {
    public User(string name, string pass, decimal bal = 0.00M) {
        UserId = Guid.NewGuid();
        Username = name;
        _salt = Guid.NewGuid().ToString();
        PasswordHash = ComputePasswordHash(_salt, pass);
        _bal = bal;
        _items = new();
    }
    
    public User(ref Utf8JsonReader reader) {
        if (reader.TokenType != JsonTokenType.StartObject) throw new JsonException("Expected StartObject");
        
        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject) {
            if (reader.TokenType == JsonTokenType.PropertyName) {
                var pName = reader.GetString();
                reader.Read();
                switch (pName) {
                    case "userid":
                        UserId = reader.GetGuid();
                        break;
                    case "username":
                        Username = reader.GetString() ?? throw new JsonException("Username is null");
                        break;
                    case "password_hash":
                        PasswordHash = reader.GetString() ?? throw new JsonException("PasswordHash is null");
                        break;
                    case "account_balance":
                        _bal = reader.GetDecimal();
                        break;
                    case "salt":
                        _salt = reader.GetString() ?? throw new JsonException("salt is null");
                        break;
                    case "items":
                        _items = JsonSerializer.Deserialize<List<Item>>(ref reader) ?? new();
                        break;
                    default:
                        throw new JsonException($"Unexpected property name: {pName}");
                }
            }
        }
    }

    // Adds an item to the user's list of items
    public void AddItem(string name, decimal price) {
        _items.Add(new(name, price));
    }
    
    // Removes an item from the user's list of items
    public void RemoveItem(Item item) {
        _items.Remove(item);
    }
    
    // Increments the user's account balance
    public void IncrementBalance(decimal amount) {
        if (amount < 0) throw new ArgumentException("Amount must be positive");
        _bal += amount;
    }
    
    // Decrements the user's account balance
    public void DecrementBalance(decimal amount, bool preventOverdraw = true) {
        if (amount < 0) throw new ArgumentException("Amount must be positive");
        if (preventOverdraw && amount > _bal) throw new BalanceOverdrawException();
        _bal -= amount;
    }
    
    // Checks if the given password matches the user's password
    public bool CheckPassword(string password) {
        return ComputePasswordHash(_salt, password) == PasswordHash;
    }
    
    // Determines the color to use for a given balance
    public static string BalanceColor(decimal balance) {
        return balance switch {
            > 0 => "green",
            < 0 => "red",
            _   => "yellow"
        };
    }
    
    private static string ComputePasswordHash(string salt, string pass) {
        string SHA256OfString(string s) {
            var bytes = System.Text.Encoding.UTF8.GetBytes(s);
            var hash = SHA256.HashData(bytes);
            return Convert.ToBase64String(hash);
        }
        
        return SHA256OfString(salt + SHA256OfString(pass + SHA256OfString(salt)));
    }

    public Guid                UserId               { get; }
    public string              Username             { get; }
    public string              PasswordHash         { private get; set; }
    // public decimal             AccountBalance       => _bal;
    public Markup              AccountBalanceMarkup => Markup.FromInterpolated($"[{BalanceColor(_bal)}]{_bal:C}[/]");
    public IReadOnlyList<Item> Items                => _items;

    private decimal             _bal;
    private readonly string     _salt;
    private readonly List<Item> _items;
    
    public class UserJsonConverter : JsonConverter<User> {
#nullable enable
        public override User? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            return new User(ref reader);
        }
#nullable disable
        
        public override void Write(Utf8JsonWriter writer, User value, JsonSerializerOptions options) {
            writer.WriteStartObject();
            writer.WriteString("userid", value.UserId);
            writer.WriteString("salt", value._salt);
            writer.WriteString("username", value.Username);
            writer.WriteString("password_hash", value.PasswordHash);
            writer.WriteNumber("account_balance", value._bal);
            writer.WritePropertyName("items");
            JsonSerializer.Serialize(writer, value._items);
            writer.WriteEndObject();
        }
    }
}
