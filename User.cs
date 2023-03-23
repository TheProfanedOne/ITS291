namespace ITS291;

public class User {
    public void AddItem(string name, decimal price) {
        _items.Add(new(name, price));
    }
    
    public void RemoveItem(Item item) {
        _items.Remove(item);
    }
    
    public void IncrementBalance(decimal amount) {
        if (amount < 0) throw new ArgumentException("Amount must be positive");
        _bal += amount;
    }
    
    public void DecrementBalance(decimal amount, bool preventOverdraw = true) {
        if (amount < 0) throw new ArgumentException("Amount must be positive");
        if (preventOverdraw && amount > _bal) throw new BalanceOverdrawException();
        _bal -= amount;
    }
    
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
    public Markup              AccountBalanceMarkup => Markup.FromInterpolated($"[{BalanceColor(AccountBalance)}]{AccountBalance:C}[/]");

    private string     _pass = "";
    private List<Item> _items = new();
    // ReSharper disable once RedundantDefaultMemberInitializer
    private decimal    _bal = 0.00M;
}
