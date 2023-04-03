using System.Diagnostics.CodeAnalysis;

namespace ITS291;

using SelTuple = ValueTuple<string, Func<User, bool>>;

/*
 * File Name: Program.cs
 * Author: Matthew Mousseau
 * Desc: The entrypoint for my ITS291 project
 */
internal static class Program {
    // Dictionary of users
    private static Dictionary<string, User> users = new();
    
    // Attempts to read users from file
    private static void LoadUsers(string path) {
        using var fStream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Read);
        try {
#nullable enable
            var jsonList = JsonSerializer.Deserialize<List<User>>(fStream, new JsonSerializerOptions {
                Converters = { new User.UserJsonConverter() }
            });
            
            if (jsonList is null) throw new JsonException();
            
            users.Clear();
            foreach (var user in jsonList)
                users.Add(user.Username, user);
#nullable disable
        } catch (JsonException) {
            users.Clear();
            users.Add("admin", new("admin", "admin"));
        }
    }
    
    // Attempts to write users to file
    private static void SaveUsers(string path) {
        using var fStream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write);
        try {
            JsonSerializer.Serialize(fStream, users.Values.ToList(), new JsonSerializerOptions {
                WriteIndented = true,
                Converters = { new User.UserJsonConverter() }
            });
        } catch (JsonException ex) {
            AnsiConsole.MarkupLine($"[red]Error saving users ({ex.LineNumber}|{ex.BytePositionInLine}): {ex.Message}[/]");
            Environment.Exit(1);
        }
    }

    // Logs the user in and returns their info
    private static User Logon() {
        var user = new TextPrompt<User>("[bold green]login[/] ([dim]username[/]):")
            .AddChoices(users.Values.ToList()).WithConverter(u => u.Username).HideChoices()
            .InvalidChoiceMessage("[red]unknown login[/]")
            .Show(AnsiConsole.Console);
        
        var _ = new TextPrompt<string>("Enter [cyan1]password[/]?")
            .Secret().PromptStyle("mediumorchid1_1")
            .Validate(user.CheckPassword, "[red]invalid password[/]")
            .Show(AnsiConsole.Console);
        
        return user;
    }
    
    // Creates and displays a table of all users' information
    [SuppressMessage("ReSharper", "UnusedParameter.Local")]
    private static bool ListUsers(User unused) {
        AnsiConsole.Write(new Table()
            .AddColumns(
                new TableColumn("[bold yellow]id[/]"),
                new TableColumn("[bold green]name[/]"),
                new TableColumn("[bold mediumorchid1_1]item count[/]"),
                new TableColumn("[bold blue]balance[/]")
            )
            .AddRows(users.Values, user => new[] {
                new Markup($"[yellow]{user.UserId}[/]"),
                new Markup($"[green]{user.Username}[/]"),
                new Markup($"[mediumorchid1_1]{user.Items.Count}[/]"),
                user.AccountBalanceMarkup
            }));
        
        return true;
    }
    
    // Password validation function
    private static readonly Func<string, ValidationResult> passValidator = p => p switch {
        _ when string.IsNullOrWhiteSpace(p) => ValidationResult.Error("[red]Password cannot be an empty string[/]"),
        _ when p.Length < 8                 => ValidationResult.Error("[red]Password must be at least 8 characters[/]"),
        _ when p.Any(char.IsWhiteSpace)     => ValidationResult.Error("[red]Password cannot contain whitespace[/]"),
        _ when !p.Any(char.IsUpper)         => ValidationResult.Error("[red]Password must contain at least one uppercase letter[/]"),
        _ when !p.Any(char.IsLower)         => ValidationResult.Error("[red]Password must contain at least one lowercase letter[/]"),
        _ when p.All(char.IsLetter)         => ValidationResult.Error("[red]Password must contain at least one non-letter character[/]"),
        _                                   => ValidationResult.Success()
    };
    
    // Adds a user to the dictionary
    [SuppressMessage("ReSharper", "UnusedParameter.Local")]
    private static bool AddUser(User unused) {
        var name = new TextPrompt<string>("Enter [green]username[/]:")
            .Validate(s => !users.ContainsKey(s), "[red]username already exists[/]")
            .Show(AnsiConsole.Console);

        var pass = new TextPrompt<string>("Enter [cyan1]password[/]:")
            .Secret().PromptStyle("mediumorchid1_1")
            .Validate(passValidator)
            .Show(AnsiConsole.Console);
            
        var bal = new TextPrompt<decimal>("Enter an initial [blue]balance[/] [dim](Must be positive)[/]:")
            .Validate(b => b >= 0, "[red]Balance must be positive[/]")
            .Show(AnsiConsole.Console);
        
        users.Add(name, new(name, pass, bal));
        return true;
    }
    
    // Removes a user from the dictionary
    [SuppressMessage("ReSharper", "UnusedParameter.Local")]
    private static bool RemoveUser(User unused) {
        var keys = new LinkedList<string>(users.Keys);
        keys.AddFirst("<cancel>");
        
        var name = new SelectionPrompt<string>()
            .Title("Select [green]user[/] to remove:")
            .AddChoices(keys)
            .Show(AnsiConsole.Console);
        
        if (name == "<cancel>") return true;
        if (name == "admin") {
            AnsiConsole.MarkupLine("[red]Cannot remove admin user[/]");
            return true;
        }
        
        users.Remove(name);
        return true;
    }
    
    // Creates and displays a table of the logged in user's information
    private static bool ShowUserDetails(User user) {
        AnsiConsole.Write(new Table()
            .AddColumns(
                new TableColumn("[bold mediumorchid1_1]Property[/]"),
                new TableColumn("[bold green]Value[/]")
            )
            .AddRow("[mediumorchid1_1]id[/]", $"[green]{user.UserId}[/]")
            .AddRow("[mediumorchid1_1]name[/]", $"[green]{user.Username}[/]")
            .AddRow("[mediumorchid1_1]item count[/]", $"[green]{user.Items.Count}[/]")
            .AddRow(new Markup("[mediumorchid1_1]balance[/]"), user.AccountBalanceMarkup));
        
        return true;
    }
    
    // Prompts the user for an amount to add to the logged in user's balance
    private static bool IncBalance(User user) {
        var amount = new TextPrompt<decimal>("How much do you want to [green]add[/]?")
            .Validate(a => a >= 0, "[red]Amount must be positive[/]")
            .Show(AnsiConsole.Console);

        AnsiConsole.Console.WriteLine($"Adding [{User.BalanceColor(amount)}]{amount:C}[/] to ", user.AccountBalanceMarkup);
        user.IncrementBalance(amount);
        AnsiConsole.Console.WriteLine($"Account Balance now ", user.AccountBalanceMarkup);
        
        return true;
    }
    
    // Prompts the user for an amount to remove from the logged in user's balance
    private static bool DecBalance(User user) {
        var amount = new TextPrompt<decimal>("How much do you want to [red]remove[/]?")
            .Validate(a => a >= 0, "[red]Amount must be positive[/]")
            .Show(AnsiConsole.Console);
        
        try {
            var oldMarkup = user.AccountBalanceMarkup;
            user.DecrementBalance(amount);
            AnsiConsole.Console.WriteLine($"Removing [{User.BalanceColor(amount * -1)}]{amount:C}[/] from ", oldMarkup);
        } catch (BalanceOverdrawException ex) { AnsiConsole.MarkupLine(ex.Message); }
        
        AnsiConsole.Console.WriteLine($"Account Balance now ", user.AccountBalanceMarkup);
        return true;
    }
    
    // Lists all of the user's items
    private static bool ListItems(User user) {
        AnsiConsole.Write(new Table()
            .AddColumns(
                new TableColumn("[bold green]Name[/]"),
                new TableColumn("[bold blue]Price[/]")
            )
            .AddRows(user.Items, item => new[] {
                new Markup($"[green]{item.Name}[/]"),
                new Markup($"[blue]{item.Price:C}[/]")
            }));

        return true;
    }
    
    // Adds an item to the user's list of items
    private static bool AddItem(User user) {
        var name = new TextPrompt<string>("What is the [green]name[/] of the item you wish to add?")
            .Validate(string.IsNullOrWhiteSpace, "[red]Name cannot be empty[/]")
            .Show(AnsiConsole.Console);
            
        var price = new TextPrompt<decimal>("What is the [blue]price[/] of the item?")
            .Validate(p => p >= 0, "[red]Price must be positive[/]")
            .Show(AnsiConsole.Console);
        
        user.AddItem(name, price);
        return true;
    }
    
    // Removes an item from the users's list of items
    private static bool RemoveItem(User user) {
        var item = new SelectionPrompt<Item>()
            .Title("What is the [green]name[/] of the item you wish to remove?")
            .AddChoices(user.Items).UseConverter(item => item.Name)
            .Show(AnsiConsole.Console);
        
        user.RemoveItem(item);
        return true;
    }
    
    // I don't even know how to describe this monstrosity
    private static readonly (SelTuple, IEnumerable<SelTuple>)[] selGroups = {
        (("Account", _ => true), new SelTuple[] {
            ("Increment Balance", IncBalance),
            ("Decrement Balance", DecBalance)
        }),
        (("Users", _ => true), new SelTuple[] {
            ("List Users", ListUsers),
            ("Add User", AddUser),
            ("Remove User", RemoveUser),
            ("Show User Details", ShowUserDetails)
        }),
        (("Items", _ => true), new SelTuple[] {
            ("List Items", ListItems),
            ("Add Item", AddItem),
            ("Remove Item", RemoveItem)
        })
    };
    private static readonly SelTuple SENTINEL = ("Quit", _ => false);

    // Displays a menu of available options and continuously prompts the user for input until they quit
    private static void DoMenu(User user) {
        var menu = new SelectionPrompt<SelTuple>()
            .Title("[bold]What do you want to do?[/]")
            .AddGroups(selGroups).AddChoices(SENTINEL)
            .UseConverter(s => s.Item1);
        
        while (menu.Show(AnsiConsole.Console).Item2(user)) {}
    }

    public static void Main(string[] args) {
        if (args.Length != 1) {
            Console.WriteLine("Usage: dotnet run -- <users.json file>");
            Environment.Exit(1);
        }
        var path = args[0];
        Console.OutputEncoding = Encoding.UTF8;
        LoadUsers(path);
        DoMenu(Logon());
        SaveUsers(path);
    }
}
