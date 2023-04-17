namespace ITS291;

using System.Data;
using System.Security.Cryptography;
using Microsoft.Data.Sqlite;

/// A class that describes a user
public sealed class User {
    /// Primary Constructor
    public User(string name, string pass, decimal bal = 0.00M) {
        UserId = Guid.NewGuid();
        Username = name;
        Salt = Guid.NewGuid().ToByteArray();
        PasswordHash = ComputePasswordHash(Salt, pass);
        _bal = bal;
        _items = new();
    }
    
    /// Secondary Constructor (from POST request)
    public User(UserPost post) : this(post.username, post.password, post.account_balance) {}

    /// Ternary Constructor (for reading from database)
    private User(IDataReader reader) {
        UserId = reader.GetGuid(reader.GetOrdinal("userid"));
        Username = reader.GetString(reader.GetOrdinal("username"));
        Salt = (byte[]) reader.GetValue(reader.GetOrdinal("salt"));
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
    
    /// A record that describes an item that a user may have
    public record Item(string Name, decimal Price);
    public record ItemPost(string name, decimal price);

    /// An exception class for when a user tries to withdraw more money than they have
    public class BalanceOverdrawException : Exception {
        public BalanceOverdrawException() : base("Insufficient funds.") {}
        public BalanceOverdrawException(string msg) : base(msg) {}
    }
    
    public record UserPost(string username, string password, decimal account_balance);
    
    /// Populates the "users" dictionary with the users in the database (if the database file already exists)
    public static void LoadUsersFromDatabase(SqliteConnection conn, Dictionary<string, User> users) {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = /* language=SQLite */ """
            select u.*, i.name, i.price
            from users u left join (
                select * from items union
                select userid, null, null from users
            ) i on u.userid = i.userid
            order by u.userid, i.name desc
        """;
        
        using var reader = cmd.ExecuteReader();
        users.Clear();
        while (reader.Read()) {
            User user = new(reader);
            users.Add(user.Username, user);
        }
    }
    
    /// Initializes the database tables and populates the "users" dictionary upon database file creation
    public static void InitDatabaseAndUsers(SqliteConnection conn, Dictionary<string, User> users) {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = /* language=SQLite */ """
            create table users (
                userid primary key not null,
                username unique not null,
                salt not null,
                pass not null,
                balance not null
            )
        """;
        cmd.ExecuteNonQuery();
        
        cmd.CommandText = /* language=SQLite */ """
            create table items (
                userid not null,
                name not null,
                price not null,
                foreign key (userid) references users (userid)
            )
        """;
        cmd.ExecuteNonQuery();
    
        users.Clear();
        users.Add("admin", new("admin", "admin"));
    }
    
    /// Saves users to the database
    public static void SaveUsersToDatabase(SqliteConnection conn, Dictionary<string, User> users) {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = /* language=SQLite */ """
            delete from items;
            delete from users;
        """;
        cmd.ExecuteNonQuery();

        cmd.CommandText = /* language=SQLite */ """
            insert into users values (@userid, @username, @salt, @pass, @bal)
        """;
        cmd.Parameters.Add("@userid", SqliteType.Blob);
        cmd.Parameters.Add("@username", SqliteType.Text);
        cmd.Parameters.Add("@salt", SqliteType.Blob);
        cmd.Parameters.Add("@pass", SqliteType.Blob);
        cmd.Parameters.Add("@bal", SqliteType.Real);
        
        foreach (var user in users.Values) {
            cmd.Parameters["@userid"].Value = user.UserId;
            cmd.Parameters["@username"].Value = user.Username;
            cmd.Parameters["@salt"].Value = user.Salt;
            cmd.Parameters["@pass"].Value = user.PasswordHash;
            cmd.Parameters["@bal"].Value = user._bal;
            cmd.ExecuteNonQuery();
        }
        
        cmd.CommandText = /* language=SQLite */ """
            insert into items values (@userid, @name, @price)
        """;
        cmd.Parameters.Add("@name", SqliteType.Text);
        cmd.Parameters.Add("@price", SqliteType.Real);
        
        foreach (var user in users.Values) {
            cmd.Parameters["@userid"].Value = user.UserId;
            foreach (var item in user._items) {
                cmd.Parameters["@name"].Value = item.Name;
                cmd.Parameters["@price"].Value = item.Price;
                cmd.ExecuteNonQuery();
            }
        }
    }
    
    /// Adds an item to the user's list of items
    public void AddItem(string name, decimal price) => _items.Add(new(name, price));
    /// Removes an item from the user's list of items
    public void RemoveItem(Item item) => _items.Remove(item);

    /// Increments the user's account balance
    public void IncrementBalance(decimal amount) {
        if (amount < 0m) throw new ArgumentException("Amount must be positive", nameof(amount));
        _bal += amount;
    }
    
    /// Decrements the user's account balance
    public void DecrementBalance(decimal amount, bool preventOverdraw = true) {
        if (amount < 0m) throw new ArgumentException("Amount must be positive", nameof(amount));
        if (preventOverdraw && amount > _bal) throw new BalanceOverdrawException();
        _bal -= amount;
    }
    
    /// Checks if the given password matches the user's password
    public bool CheckPassword(string pass) => PasswordHash.SequenceEqual(ComputePasswordHash(Salt, pass));
    
    /// Determines the color to use for a given balance
    public static string BalanceColor(decimal balance) => balance switch {
        > 0m => "green",
        < 0m => "red",
        _    => "yellow"
    };

    /// Computes the password hash for a given password
    private static byte[] ComputePasswordHash(byte[] salt, string pass) {
        byte[] GetHash(byte[] b) => SHA256.HashData(b);
        var bytes = Encoding.UTF8.GetBytes(pass);
        return GetHash(salt.ArrConcat(GetHash(bytes.ArrConcat(GetHash(salt)))));
    }

    public Guid                UserId               { get; }
    public string              Username             { get; }
    private byte[]             Salt                 { get; }
    public byte[]              PasswordHash         { private get; set; }
    public decimal             AccountBalance       => _bal;
    public Markup              AccountBalanceMarkup => new($"[{BalanceColor(_bal)}]{_bal:C}[/]");
    public IReadOnlyList<Item> Items                => _items;

    private decimal             _bal;
    private readonly List<Item> _items;
}
