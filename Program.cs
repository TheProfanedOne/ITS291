namespace ITS291;

/*
 * File Name: Program.cs
 * Author: Matthew Mousseau
 * Desc: The entrypoint for my ITS291 project
 */
internal static class Program {
    // IAnsiConsole instance to be used by the various functions
    private static readonly IAnsiConsole ansi = AnsiConsole.Create(new() {
        Ansi = AnsiSupport.Detect,
        ColorSystem = ColorSystemSupport.Detect
    });

    // Array of users
    private static readonly IReadOnlyList<User> users = new List<User>(new User[] {
        new() {
            UserId = Guid.NewGuid(),
            Username = "user1",
            Password = "password1",
            AccountBalance = 5.00M
        },
        new() {
            UserId = Guid.NewGuid(),
            Username = "user2",
            Password = "password2",
            AccountBalance = 0.00M
        },
        new() {
            UserId = Guid.NewGuid(),
            Username = "user3",
            Password = "password3",
            AccountBalance = -5.00M
        }
    });

    // Logs the user in and returns their info
    private static User Logon() {
        var user = new TextPrompt<User>("[bold green]login[/] ([dim]username[/]):")
            .AddChoices(users).WithConverter(u => u.Username).HideChoices()
            .InvalidChoiceMessage("[red]unknown login[/]")
            .Show(ansi);
        
        var _ = new TextPrompt<string>("Enter [cyan1]password[/]?")
            .Secret().PromptStyle("mediumorchid1_1")
            .Validate(user.CheckPassword, "[red]invalid password[/]")
            .Show(ansi);
        
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
        
        foreach (var user in users) table.AddRow(
            new Markup($"[yellow]{user.UserId}[/]"),
            new Markup($"[green]{user.Username}[/]"),
            new Markup($"[mediumorchid1_1]{user.Items.Count}[/]"),
            user.AccountBalanceMarkup
        );
        
        ansi.Write(table);
    }
    
    // Creates and displays a table of the logged in user's information
    private static void ShowUserDetails(User user) {
        ansi.Write(new Table()
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
            .Show(ansi);

        ansi.WriteLine($"Adding [{User.BalanceColor(amount)}]{amount:C}[/] to ", user.AccountBalanceMarkup);
        user.IncrementBalance(amount);
        ansi.WriteLine($"Account Balance now ", user.AccountBalanceMarkup);
    }
    
    // Prompts the user for an amount to remove from the logged in user's balance
    private static void DecBalance(User user) {
        var amount = new TextPrompt<decimal>("How much do you want to [red]remove[/]?")
            .Validate(a => a >= 0, "[red]Amount must be positive[/]")
            .Show(ansi);
        
        try {
            var oldMarkup = user.AccountBalanceMarkup;
            user.DecrementBalance(amount);
            ansi.WriteLine($"Removing [{User.BalanceColor(amount * -1)}]{amount:C}[/] from ", oldMarkup);
        } catch (BalanceOverdrawException e) {
            ansi.MarkupLine(e.Message);
        } finally {
            ansi.WriteLine($"Account Balance now ", user.AccountBalanceMarkup);
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
        
        ansi.Write(table);
    }
    
    // Adds an item to the user's list of items
    private static void AddItem(User user) {
        var name = new TextPrompt<string>("What is the [green]name[/] of the item you wish to add?")
            .Validate(string.IsNullOrWhiteSpace, "[red]Name cannot be empty[/]")
            .Show(ansi);
            
        var price = new TextPrompt<decimal>("What is the [blue]price[/] of the item?")
            .Validate(p => p >= 0, "[red]Price must be positive[/]")
            .Show(ansi);
        
        user.AddItem(name, price);
    }
    
    // Removes an item from the users's list of items
    private static void RemoveItem(User user) {
        var item = new SelectionPrompt<Item>()
            .Title("What is the [green]name[/] of the item you wish to remove?")
            .AddChoices(user.Items).UseConverter(item => item.Name)
            .Show(ansi);
        
        user.RemoveItem(item);
    }

    // Array of tuples containing the menu option text and the function to call when it is selected
    private static readonly (string, Action<User>)[] selections = {
        ("Increment Balance", IncBalance),
        ("Decrement Balance", DecBalance),
        ("List Items", ListItems),
        ("Add Item", AddItem),
        ("Remove Item", RemoveItem),
        ("List Users", _ => ListUsers()),
        ("Show User Details", ShowUserDetails),
        ("Quit", _ => {})
    };
    
    // Displays a menu of available options and continuously prompts the user for input until they quit
    private static bool DoMenu(User user) {
        var sel = new SelectionPrompt<(string, Action<User>)>()
            .Title("[bold]What do you want to do?[/]")
            .AddChoices(selections).UseConverter(s => s.Item1)
            .Show(ansi);

        var selIdx = Array.IndexOf(selections, sel);
        var retVal = selIdx switch {
            7 => false,
            // A rather hacky way of having a multi-line case value in a switch expression
            < 7 and >= 0 => true,
            _ => throw new InvalidOperationException("Invalid Selection")
        };
        
        if (retVal) sel.Item2(user);
        return retVal;
    }

    public static void Main(string[] args) {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        var userId = Logon();
        while (DoMenu(userId)) {}
    }
}
