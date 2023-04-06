using System.Data;
using Microsoft.Data.Sqlite;

namespace ITS291;

using System.Security.Cryptography;

public record Item(string Name, decimal Price);

public class BalanceOverdrawException : Exception {
    public BalanceOverdrawException() : base(
        "Amount must be less than or equal to the account balance when [orangered1]preventOverdraw[/] is set."
    ) {}
    public BalanceOverdrawException(string msg) : base(msg) {}
}

public sealed class User {
    // Primary Constructor
    public User(string name, string pass, decimal bal = 0.00M) {
        UserId = Guid.NewGuid();
        Username = name;
        _salt = Guid.NewGuid().ToByteArray();
        PasswordHash = ComputePasswordHash(_salt, pass);
        _bal = bal;
        _items = new();
    }

    // Secondary Constructor (for reading from database)
    public User(IDataReader reader) {
        UserId = reader.GetGuid(reader.GetOrdinal("userid"));
        Username = reader.GetString(reader.GetOrdinal("username"));
        _salt = (byte[]) reader.GetValue(reader.GetOrdinal("salt"));
        PasswordHash = (byte[]) reader.GetValue(reader.GetOrdinal("pass"));
        _bal = reader.GetDecimal(reader.GetOrdinal("balance"));
        
        _items = new();
        var nameOrd = reader.GetOrdinal("name");
        var priceOrd = reader.GetOrdinal("price");
        while (!reader.IsDBNull(nameOrd)) {
            _items.Add(new(
                reader.GetString(nameOrd),
                reader.GetDecimal(priceOrd)
            ));
            reader.Read();
        }
    }
    
    // Maps the user's data to a command's parameters
    public void MapDataToCommand(SqliteCommand cmd) {
        cmd.Parameters["@userid"].Value = UserId.ToString();
        cmd.Parameters["@username"].Value = Username;
        cmd.Parameters["@salt"].Value = _salt;
        cmd.Parameters["@pass"].Value = PasswordHash;
        cmd.Parameters["@bal"].Value = _bal;
    }

    // Adds an item to the user's list of items
    public void AddItem(string name, decimal price) => _items.Add(new(name, price));

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
    public bool CheckPassword(string pass) {
        return PasswordHash.SequenceEqual(ComputePasswordHash(_salt, pass));
    }
    
    // Determines the color to use for a given balance
    public static string BalanceColor(decimal balance) {
        return balance switch {
            > 0 => "green",
            < 0 => "red",
            _   => "yellow"
        };
    }
    
    // Computes the password hash for a given password
    private static byte[] ComputePasswordHash(byte[] salt, string pass) {
        byte[] GetHash(byte[] b) => SHA256.HashData(b);
        var bytes = Encoding.UTF8.GetBytes(pass);
        return GetHash(salt.ArrConcat(GetHash(bytes.ArrConcat(GetHash(salt)))));
    }

    public Guid                UserId               { get; }
    public string              Username             { get; }
    public byte[]              PasswordHash         { private get; set; }
    // public decimal             AccountBalance       => _bal;
    public Markup              AccountBalanceMarkup => new($"[{BalanceColor(_bal)}]{_bal:C}[/]");
    public IReadOnlyList<Item> Items                => _items;

    private decimal             _bal;
    private readonly byte[]     _salt;
    private readonly List<Item> _items;
}
