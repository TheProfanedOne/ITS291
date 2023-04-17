/*
 * File Name: Program.cs
 * Author: Matthew Mousseau
 * Desc: The entrypoint for my ITS291 project
 */

namespace ITS291;

using Microsoft.Data.Sqlite;
using static User;
using SelTuple = ValueTuple<string, Func<User, bool>>;

public class Program {
    /// <summary><c>IAnsiConsole</c> "instance" to be used by this program</summary>
    private static readonly IAnsiConsole ansi = AnsiConsole.Create(new() {
        Ansi = AnsiSupport.Detect,
        ColorSystem = ColorSystemSupport.Detect,
    });
    
    /// Dictionary of users
    private static readonly Dictionary<string, User> users = new();
    
    /// Attempts to read users from database
    private static void LoadUsers(string path) {
        var connStrB = new SqliteConnectionStringBuilder {
            DataSource = path,
            Mode = SqliteOpenMode.ReadWrite
        };
    
        try {
            using var conn = new SqliteConnection(connStrB.ConnectionString);
            conn.Open();
        
            LoadUsersFromDatabase(conn, users);
        } catch (SqliteException) {
            connStrB.Mode = SqliteOpenMode.ReadWriteCreate;
            using var conn = new SqliteConnection(connStrB.ConnectionString);
            conn.Open();
        
            InitDatabaseAndUsers(conn, users);
        }
    }
    
    /// Attempts to write users to database
    private static void SaveUsers(string path) {
        var connStrB = new SqliteConnectionStringBuilder {
            DataSource = path,
            Mode = SqliteOpenMode.ReadWrite
        };
    
        try {
            using var conn = new SqliteConnection(connStrB.ConnectionString);
            conn.Open();
        
            SaveUsersToDatabase(conn, users);
        } catch (SqliteException ex) {
            ansi.MarkupLine($"[red]Error saving users: {ex.Message}[/]");
        }
    }
    
    /// Logs the user in and returns their info
    private static User Logon() {
        var user = new TextPrompt<User>("[bold green]login[/] ([dim]username[/]):")
            .AddChoices(users.Values.ToList()).WithConverter(u => u.Username).HideChoices()
            .InvalidChoiceMessage("[red]unknown login[/]")
            .Show(ansi);
    
        var _ = new TextPrompt<string>("Enter [cyan1]password[/]?")
            .Secret().PromptStyle("mediumorchid1_1")
            .Validate(user.CheckPassword, "[red]invalid password[/]")
            .Show(ansi);
    
        return user;
    }
    
    /// Creates and displays a table of all users' information
    private static bool ListUsers(User unused) {
        ansi.Write(new Table()
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
    
    private static ValidationResult Err(string s) => ValidationResult.Error(s);
    private static ValidationResult Ok() => ValidationResult.Success();
    
    private static (bool, string) ValidateName(string n) => n switch {
        _ when string.IsNullOrWhiteSpace(n) => (true, "[red]Username cannot be empty[/]"),
        _ when users.ContainsKey(n)         => (true, "[red]Username already exists[/]"),
        _                                   => (false, null)
    };
    
    private static (bool, string) ValidatePass(string p) => p switch {
        _ when string.IsNullOrWhiteSpace(p) => (true, "[red]Password cannot be an empty string[/]"),
        _ when p.Length < 8                 => (true, "[red]Password must be at least 8 characters[/]"),
        _ when p.Any(char.IsWhiteSpace)     => (true, "[red]Password cannot contain whitespace[/]"),
        _ when !p.Any(char.IsUpper)         => (true, "[red]Password must contain at least one uppercase letter[/]"),
        _ when !p.Any(char.IsLower)         => (true, "[red]Password must contain at least one lowercase letter[/]"),
        _ when p.All(char.IsLetter)         => (true, "[red]Password must contain at least one non-letter character[/]"),
        _                                   => (false, null)
    };
    
    /// Username validation function
    private static ValidationResult NameValidator(string n) => ValidateName(n) switch {
        (true, var msg) => Err(msg),
        _               => Ok()
    };
    
    /// Password validation function
    private static ValidationResult PassValidator(string p) => ValidatePass(p) switch {
        (true, var msg) => Err(msg),
        _               => Ok()
    };
    
    /// Adds a user to the dictionary
    private static bool AddUser(User unused) {
        var name = new TextPrompt<string>("Enter [green]username[/]:")
            .Validate(NameValidator)
            .Show(ansi);

        var pass = new TextPrompt<string>("Enter [cyan1]password[/]:")
            .Secret().PromptStyle("mediumorchid1_1")
            .Validate(PassValidator)
            .Show(ansi);
        
        var bal = new TextPrompt<decimal>("Enter an initial [blue]balance[/] [dim](Must be positive)[/]:")
            .Validate(b => b >= 0, "[red]Balance must be positive[/]")
            .Show(ansi);
    
        users.Add(name, new(name, pass, bal));
        return true;
    }
    
    /// Removes a user from the dictionary
    private static bool RemoveUser(User unused) {
        var name = new SelectionPrompt<string>()
            .Title("Select [green]user[/] to remove:")
            .AddChoices(users.Keys.Prepend("<cancel>"))
            .Show(ansi);
    
        if (name == "admin") {
            ansi.MarkupLine("[red]Cannot remove admin user[/]");
            return true;
        }
    
        users.Remove(name);
        return true;
    }
    
    /// Creates and displays a table of the logged in user's information
    private static bool ShowUserDetails(User user) {
        ansi.Write(new Table()
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
    
    /// Prompts the user for an amount to add to the logged in user's balance
    private static bool IncBalance(User user) {
        var amount = new TextPrompt<decimal>("How much do you want to [green]add[/]?")
            .Validate(a => a >= 0, "[red]Amount must be positive[/]")
            .Show(ansi);

        ansi.WriteLine($"Adding [{BalanceColor(amount)}]{amount:C}[/] to ", user.AccountBalanceMarkup);
        user.IncrementBalance(amount);
        ansi.WriteLine($"Account Balance now ", user.AccountBalanceMarkup);
    
        return true;
    }
    
    /// Prompts the user for an amount to remove from the logged in user's balance
    private static bool DecBalance(User user) {
        var amount = new TextPrompt<decimal>("How much do you want to [red]remove[/]?")
            .Validate(a => a >= 0, "[red]Amount must be positive[/]")
            .Show(ansi);
    
        try {
            var oldMarkup = user.AccountBalanceMarkup;
            user.DecrementBalance(amount);
            ansi.WriteLine($"Removing [{BalanceColor(amount * -1)}]{amount:C}[/] from ", oldMarkup);
        } catch (BalanceOverdrawException ex) {
            ansi.MarkupLine(ex.Message);
        }
    
        ansi.WriteLine($"Account Balance now ", user.AccountBalanceMarkup);
        return true;
    }
    
    /// Lists all of the user's items
    private static bool ListItems(User user) {
        ansi.Write(new Table()
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
    
    /// Adds an item to the user's list of items
    private static bool AddItem(User user) {
        var name = new TextPrompt<string>("What is the [green]name[/] of the item you wish to add?")
            .Validate(n => !string.IsNullOrWhiteSpace(n), "[red]Name cannot be empty[/]")
            .Show(ansi);
        
        var price = new TextPrompt<decimal>("What is the [blue]price[/] of the item?")
            .Validate(p => p >= 0, "[red]Price must be positive[/]")
            .Show(ansi);
    
        user.AddItem(name, price);
        return true;
    }
    
    /// Removes an item from the users's list of items
    private static bool RemoveItem(User user) {
        var item = new SelectionPrompt<Item>()
            .Title("What is the [green]name[/] of the item you wish to remove?")
            .AddChoices(user.Items.Prepend(new("<cancel>", 0))).UseConverter(item => item.Name)
            .Show(ansi);
    
        user.RemoveItem(item);
        return true;
    }
    
    /// I don't even know how to describe this monstrosity
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
    /// The "Quit" option
    private static readonly SelTuple SENTINEL = ("Quit", _ => false);
    
    /// Displays a menu of available options and continuously prompts the user for input until they quit
    private static void DoMenu(User user) {
        var menu = new SelectionPrompt<SelTuple>()
            .Title("[bold]What do you want to do?[/]")
            .AddGroups(selGroups).AddChoices(SENTINEL)
            .UseConverter(s => s.Item1);
    
        while (menu.Show(ansi).Item2(user)) {}
    }

    private static WebApplication StartWebApi(string[] args) {
        var builder = WebApplication.CreateBuilder(args);
        
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        
        var app = builder.Build();
        
        app.UseSwagger();
        app.UseSwaggerUI();
        
        app.UseHttpsRedirection();
        
        var serializerOptions = JsonSerializerOptions.Default;
        const string contentType = "application/json";
        app.MapGet("/users/list", () => Results.Json(
            from u in users.Values select new { user_id = u.UserId, username = u.Username },
            serializerOptions, contentType, StatusCodes.Status200OK
        ));
        app.MapPost("/users", (UserPost body) => {
            switch (ValidateName(body.username)) { case (true, var msg): return Results.BadRequest(msg); }
            switch (ValidatePass(body.password)) { case (true, var msg): return Results.BadRequest(msg); }
            if (body.account_balance < 0) return Results.BadRequest("Account balance cannot be negative");
            users.Add(body.username, new(body));
            return Results.NoContent();
        });
        app.MapDelete("/{username}", (string username) => users.Remove(username)
            ? Results.NoContent()
            : Results.NotFound($"Unknown user: `{username}`")
        );
        app.MapGet("/{username}", (string username) => users.TryGetValue(username, out var user)
            ? Results.Json(new {
                user_id = user.UserId,
                username = user.Username,
                account_balance = user.AccountBalance,
                item_count = user.Items.Count
            }, serializerOptions, contentType, StatusCodes.Status200OK)
            : Results.NotFound($"Unknown user: `{username}`")
        );
        IResult IncResult(User user, decimal amount) {
            user.IncrementBalance(amount);
            return Results.NoContent();
        }
        IResult DecResult(User user, decimal amount) {
            try {
                user.DecrementBalance(amount);
                return Results.NoContent();
            } catch (BalanceOverdrawException ex) {
                return Results.BadRequest(ex.Message);
            }
        }
        app.MapPut("/{username}/accountBalance", (string username, string op, decimal amount) => {
            if (!users.TryGetValue(username, out var user)) return Results.NotFound($"Unknown user `{username}`");
            if (amount < 0) return Results.BadRequest("Amount cannot be negative");
            return op switch {
                "inc" => IncResult(user, amount),
                "dec" => DecResult(user, amount),
                _ => Results.BadRequest($"Invalid operation: `{op}`")
            };
        });
        app.MapGet("/{username}/items", (string username) => users.TryGetValue(username, out var user)
            ? Results.Json(
                from i in user.Items select new { name = i.Name, price = i.Price },
                serializerOptions, contentType, StatusCodes.Status200OK
            )
            : Results.NotFound($"Unknown user: `{username}`")
        );
        app.MapPost("/{username}/items", (string username, ItemPost body) => {
            if (!users.TryGetValue(username, out var user)) return Results.NotFound($"Unknown user: `{username}`");
            if (string.IsNullOrWhiteSpace(body.name)) return Results.BadRequest("Name cannot be empty");
            if (body.price < 0) return Results.BadRequest("Price cannot be negative");
            user.AddItem(body.name, body.price);
            return Results.NoContent();
        });
        app.MapDelete("/{username}/item/{name}", (string username, string name) => {
            if (!users.TryGetValue(username, out var user)) return Results.NotFound($"Unknown user: `{username}`");
            try {
                user.RemoveItem(user.Items.First(i => i.Name == name));
                return Results.NoContent();
            } catch (InvalidOperationException) {
                return Results.NotFound($"Unknown item: `{name}`");
            }
        });
        
        app.RunAsync("https://localhost:5000");
        return app;
    }
    
    public static void Main(string[] args) {
        Console.OutputEncoding = Encoding.UTF8;

        if (args.Length != 1) {
            ansi.WriteLine("Usage: `dotnet run -- <database file>`");
            Environment.Exit(1);
        }

        var path = args[0];

        LoadUsers(path);
        var app = StartWebApi(args);

        Console.CancelKeyPress += delegate {
            AnsiConsole.WriteLine();
            if (new ConfirmationPrompt("Do you want to save what you have?").Show(ansi)) {
                SaveUsers(path);
            }
            app.StopAsync().Wait();
        };

        DoMenu(Logon());
        SaveUsers(path);
        
        app.StopAsync().Wait();
    }
}
