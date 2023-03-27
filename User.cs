namespace ITS291;

public sealed class User {
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
        return password == _pass;
    }
    
    // Determines the color to use for a given balance
    public static string BalanceColor(decimal balance) {
        return balance switch {
            > 0 => "green",
            < 0 => "red",
            _   => "yellow"
        };
    }

    public Guid                UserId               { get; init; }
    public required string     Username             { get; init; }
    public required string     Password             { set => _pass = value; }
    public IReadOnlyList<Item> Items                => _items;
    public decimal             AccountBalance       { get => _bal; init => _bal = value; }
    public Markup              AccountBalanceMarkup => Markup.FromInterpolated($"[{BalanceColor(_bal)}]{_bal:C}[/]");

    private string     _pass = "";
    private List<Item> _items = new();
    // ReSharper disable once RedundantDefaultMemberInitializer
    private decimal    _bal = 0.00M;
}
