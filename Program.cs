using Spectre.Console.Rendering;

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
    
    private static void AnsiWriteLine(IRenderable thing) {
        ansi.Write(thing);
        ansi.WriteLine();
    }

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
            .Validate(p => user.CheckPassword(p), "[red]invalid password[/]")
            .Show(ansi);
        
        return user;
    }
    
    // Creates and displays a table of all users' information
    private static void ListUsers() {
        Table table = new();
        table.AddColumns(
            new TableColumn("[bold yellow]id[/]"),
            new TableColumn("[bold green]name[/]"),
            new TableColumn("[bold mediumorchid1_1]item count[/]"),
            new TableColumn("[bold blue]balance[/]")
        );
        
        foreach (var user in users) {
            table.AddRow(
                new Markup($"[yellow]{user.UserId}[/]"),
                new Markup($"[green]{user.Username}[/]"),
                new Markup($"[mediumorchid1_1]{user.Items.Count}[/]"),
                user.AccountBalanceMarkup
            );
        }
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

        ansi.Markup($"Adding [{User.BalanceColor(amount)}]{amount:C}[/] to ");
        AnsiWriteLine(user.AccountBalanceMarkup);
        
        user.IncrementBalance(amount);
        
        ansi.Markup("Account Balance now ");
        AnsiWriteLine(user.AccountBalanceMarkup);
    }
    
    // Prompts the user for an amount to remove from the logged in user's balance
    private static void DecBalance(User user) {
        var amount = new TextPrompt<decimal>("How much do you want to [red]remove[/]?")
            .Validate(a => a >= 0, "[red]Amount must be positive[/]")
            .Show(ansi);
        
        try {
            var oldBalanceMarkup = user.AccountBalanceMarkup;
            user.DecrementBalance(amount);
            
            ansi.Markup($"Removing [{User.BalanceColor(amount * -1)}]{amount:C}[/] from ");
            AnsiWriteLine(oldBalanceMarkup);
        } catch (BalanceOverdrawException e) {
            ansi.MarkupLine(e.Message);
        } finally {
            ansi.Markup($"Account Balance now ");
            AnsiWriteLine(user.AccountBalanceMarkup);
        }
    }
    
    // Parallel array of menu options
    private static readonly string[] selValues = {
        "Increment Balance",
        "Decrement Balance",
        "List Users",
        "Show User Details",
        "Quit"
    };
    
    // Parallel array of functions to call when the corresponding selection is made
    private static readonly Action<User>[] selActions = {
        IncBalance,
        DecBalance,
        _ => ListUsers(),
        ShowUserDetails
    };
    
    // Displays a menu of available options and continuously prompts the user for input until they quit
    private static bool DoMenu(User user) {
        var sel = Array.IndexOf(selValues, new SelectionPrompt<string>()
            .Title("[bold]What do you want to do?[/]")
            .AddChoices(selValues)
            .Show(ansi));

        return sel switch {
            4 => false,
            // A rather hacky way of having a multi-line case value in a switch expression
            < 4 and >= 0 => ((Func<bool>) delegate {
                selActions[sel](user);
                return true;
            })(),
            _ => throw new InvalidOperationException("Invalid Selection")
        };
    }

    public static void Main(string[] args) {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        var userId = Logon();
        while (DoMenu(userId)) {}
    }
 }
