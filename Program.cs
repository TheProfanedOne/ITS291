namespace ITS291;

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
            
            users.Clear();
            foreach (var user in jsonList ?? throw new JsonException())
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
            AnsiConsole.MarkupLine("[green]Users saved[/]");
        } catch (JsonException) {
            AnsiConsole.MarkupLine("[red]Error saving users[/]");
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
    private static void ListUsers() {
        var table = new Table().AddColumns(
            new TableColumn("[bold yellow]id[/]"),
            new TableColumn("[bold green]name[/]"),
            new TableColumn("[bold mediumorchid1_1]item count[/]"),
            new TableColumn("[bold blue]balance[/]")
        );
        
        foreach (var user in users.Values.ToList()) table.AddRow(
            new Markup($"[yellow]{user.UserId}[/]"),
            new Markup($"[green]{user.Username}[/]"),
            new Markup($"[mediumorchid1_1]{user.Items.Count}[/]"),
            user.AccountBalanceMarkup
        );
        
        AnsiConsole.Write(table);
    }
    
    // Adds a user to the dictionary
    private static void AddUser() {
        var name = new TextPrompt<string>("Enter [green]username[/]:")
            .Validate(s => !users.ContainsKey(s), "[red]username already exists[/]")
            .Show(AnsiConsole.Console);

        var pass = new TextPrompt<string>("Enter [cyan1]password[/]:")
            .Secret().PromptStyle("mediumorchid1_1")
            .Validate(p =>
                string.IsNullOrWhiteSpace(p) ? ValidationResult.Error("[red]Password cannot be empty[/]") :
                p.Length < 8                 ? ValidationResult.Error("[red]Password must be at least 8 characters[/]") :
                p.Any(char.IsWhiteSpace)     ? ValidationResult.Error("[red]Password cannot contain whitespace[/]") :
                !p.Any(char.IsUpper)         ? ValidationResult.Error("[red]Password must contain at least one uppercase letter[/]") :
                !p.Any(char.IsLower)         ? ValidationResult.Error("[red]Password must contain at least one lowercase letter[/]") :
                p.All(char.IsLetter)         ? ValidationResult.Error("[red]Password must contain at least one non-letter character[/]")
                                             : ValidationResult.Success()
            )
            .Show(AnsiConsole.Console);
            
        var bal = new TextPrompt<decimal>("Enter an initial [blue]balance[/] [dim](Must be positive)[/]:")
            .Validate(b => b >= 0, "[red]Balance must be positive[/]")
            .Show(AnsiConsole.Console);
        
        users.Add(name, new(name, pass, bal));
    }
    
    // Removes a user from the dictionary
    private static void RemoveUser() {
        var keys = users.Keys.ToList();
        keys.Insert(0, "<cancel>");
        
        var name = new SelectionPrompt<string>()
            .Title("Enter [green]username[/] to remove:")
            .AddChoices(keys)
            .Show(AnsiConsole.Console);
        
        if (name == "<cancel>") return;
        if (name == "admin") {
            AnsiConsole.MarkupLine("[red]Cannot remove admin user[/]");
            return;
        }
        
        users.Remove(name);
    }
    
    // Creates and displays a table of the logged in user's information
    private static void ShowUserDetails(User user) {
        AnsiConsole.Write(new Table()
            .AddColumns(
                new TableColumn("[bold mediumorchid1_1]Property[/]"),
                new TableColumn("[bold green]Value[/]")
            )
            .AddRow("[mediumorchid1_1]id[/]", $"[green]{user.UserId}[/]")
            .AddRow("[mediumorchid1_1]name[/]", $"[green]{user.Username}[/]")
            .AddRow("[mediumorchid1_1]item count[/]", $"[green]{user.Items.Count}[/]")
            .AddRow(new Markup("[mediumorchid1_1]balance[/]"), user.AccountBalanceMarkup));
    }
    
    // Prompts the user for an amount to add to the logged in user's balance
    private static void IncBalance(User user) {
        var amount = new TextPrompt<decimal>("How much do you want to [green]add[/]?")
            .Validate(a => a >= 0, "[red]Amount must be positive[/]")
            .Show(AnsiConsole.Console);

        AnsiConsole.Console.WriteLine($"Adding [{User.BalanceColor(amount)}]{amount:C}[/] to ", user.AccountBalanceMarkup);
        user.IncrementBalance(amount);
        AnsiConsole.Console.WriteLine($"Account Balance now ", user.AccountBalanceMarkup);
    }
    
    // Prompts the user for an amount to remove from the logged in user's balance
    private static void DecBalance(User user) {
        var amount = new TextPrompt<decimal>("How much do you want to [red]remove[/]?")
            .Validate(a => a >= 0, "[red]Amount must be positive[/]")
            .Show(AnsiConsole.Console);
        
        try {
            var oldMarkup = user.AccountBalanceMarkup;
            user.DecrementBalance(amount);
            AnsiConsole.Console.WriteLine($"Removing [{User.BalanceColor(amount * -1)}]{amount:C}[/] from ", oldMarkup);
        } catch (BalanceOverdrawException e) {
            AnsiConsole.MarkupLine(e.Message);
        } finally {
            AnsiConsole.Console.WriteLine($"Account Balance now ", user.AccountBalanceMarkup);
        }
    }
    
    // Lists all of the user's items
    private static void ListItems(User user) {
        var table = new Table().AddColumns(
            new TableColumn("[bold green]Name[/]"),
            new TableColumn("[bold blue]Price[/]")
        );
        
        foreach (var (name, price) in user.Items) table.AddRow(
            $"[green]{name}[/]", $"[blue]{price:C}[/]"
        );
        
        AnsiConsole.Write(table);
    }
    
    // Adds an item to the user's list of items
    private static void AddItem(User user) {
        var name = new TextPrompt<string>("What is the [green]name[/] of the item you wish to add?")
            .Validate(string.IsNullOrWhiteSpace, "[red]Name cannot be empty[/]")
            .Show(AnsiConsole.Console);
            
        var price = new TextPrompt<decimal>("What is the [blue]price[/] of the item?")
            .Validate(p => p >= 0, "[red]Price must be positive[/]")
            .Show(AnsiConsole.Console);
        
        user.AddItem(name, price);
    }
    
    // Removes an item from the users's list of items
    private static void RemoveItem(User user) {
        var item = new SelectionPrompt<Item>()
            .Title("What is the [green]name[/] of the item you wish to remove?")
            .AddChoices(user.Items).UseConverter(item => item.Name)
            .Show(AnsiConsole.Console);
        
        user.RemoveItem(item);
    }
    
    // Array of tuples containing the menu option text and the function to call when it is selected
    private static readonly (string, Action<User>)[] selections = {
        ("Increment Balance", IncBalance),
        ("Decrement Balance", DecBalance),
        ("List Users", _ => ListUsers()),
        ("Add User", _ => AddUser()),
        ("Remove User", _ => RemoveUser()),
        ("Show User Details", ShowUserDetails),
        ("List Items", ListItems),
        ("Add Item", AddItem),
        ("Remove Item", RemoveItem),
        ("Quit", _ => {})
    };
    
    // Displays a menu of available options and continuously prompts the user for input until they quit
    private static bool DoMenu(User user) {
        var sel = new SelectionPrompt<(string, Action<User>)>()
            .Title("[bold]What do you want to do?[/]")
            .AddChoices(selections).UseConverter(s => s.Item1)
            .Show(AnsiConsole.Console);
        
        var retVal = sel.Item1 switch {
            "Quit" => false,
            // A rather hacky way of having a multi-line case value in a switch expression
            var msg when selections.Any(s => s.Item1 == msg) => true,
            _ => throw new InvalidOperationException("Invalid Selection")
        };
        
        sel.Item2(user);
        return retVal;
    }

    public static void Main(string[] args) {
        if (args.Length != 1) {
            Console.WriteLine("Usage: ITS291 <users.json file>");
            return;
        }
        var path = args[0];
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        LoadUsers(path);
        var user = Logon();
        while (DoMenu(user)) {}
        SaveUsers(path);
    }
}
