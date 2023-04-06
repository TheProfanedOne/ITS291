/*
 * File Name: Program.cs
 * Author: Matthew Mousseau
 * Desc: The entrypoint for my ITS291 project
 */

using System.Diagnostics.CodeAnalysis;
using Microsoft.Data.Sqlite;
using SelTuple = System.ValueTuple<string, System.Func<ITS291.User, bool>>;

// Abbreviation (kind of) for AnsiConsole.Console
var ansi = AnsiConsole.Console;

// Dictionary of users
var users = new Dictionary<string, User>();

// Attempts to read users from database
void LoadUsers(string path) {
    var connStrB = new SqliteConnectionStringBuilder {
        DataSource = path,
        Mode = SqliteOpenMode.ReadWrite
    };
    
    try {
        using var conn = new SqliteConnection(connStrB.ConnectionString);
        conn.Open();
        
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
    } catch (SqliteException) {
        connStrB.Mode = SqliteOpenMode.ReadWriteCreate;
        using var conn = new SqliteConnection(connStrB.ConnectionString);
        conn.Open();
        
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
}

// Attempts to write users to database
void SaveUsers(string path) {
    var connStrB = new SqliteConnectionStringBuilder {
        DataSource = path,
        Mode = SqliteOpenMode.ReadWrite
    };
    
    try {
        using var conn = new SqliteConnection(connStrB.ConnectionString);
        conn.Open();
        
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            delete from items;
            delete from users;
        """;
        cmd.ExecuteNonQuery();

        cmd.CommandText = "insert into users values (@userid, @username, @salt, @pass, @bal)";
        cmd.Parameters.Add("@userid", SqliteType.Text);
        cmd.Parameters.Add("@username", SqliteType.Text);
        cmd.Parameters.Add("@salt", SqliteType.Blob);
        cmd.Parameters.Add("@pass", SqliteType.Blob);
        cmd.Parameters.Add("@bal", SqliteType.Real);
        
        foreach (var user in users.Values) {
            user.MapDataToCommand(cmd);
            cmd.ExecuteNonQuery();
        }
        
        cmd.CommandText = "insert into items values (@userid, @name, @price)";
        cmd.Parameters.Add("@name", SqliteType.Text);
        cmd.Parameters.Add("@price", SqliteType.Real);
        
        foreach (var user in users.Values) {
            cmd.Parameters["@userid"].Value = user.UserId.ToString();
            foreach (var item in user.Items) {
                cmd.Parameters["@name"].Value = item.Name;
                cmd.Parameters["@price"].Value = item.Price;
                cmd.ExecuteNonQuery();
            }
        }
        
        conn.Close();
    } catch (SqliteException ex) {
        ansi.MarkupLine($"[red]Error saving users: {ex.Message}[/]");
    }
}

// Logs the user in and returns their info
User Logon() {
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

// Creates and displays a table of all users' information
[SuppressMessage("ReSharper", "UnusedParameter.Local")]
bool ListUsers(User unused) {
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

// Username validation function
ValidationResult NameValidator(string n) => n switch {
    _ when string.IsNullOrWhiteSpace(n) => ValidationResult.Error("[red]Username cannot be empty[/]"),
    _ when users.ContainsKey(n)         => ValidationResult.Error("[red]Username already exists[/]"),
    _                                   => ValidationResult.Success()
};

// Password validation function
ValidationResult PassValidator(string p) => p switch {
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
bool AddUser(User unused) {
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

// Removes a user from the dictionary
[SuppressMessage("ReSharper", "UnusedParameter.Local")]
bool RemoveUser(User unused) {
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

// Creates and displays a table of the logged in user's information
bool ShowUserDetails(User user) {
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

// Prompts the user for an amount to add to the logged in user's balance
bool IncBalance(User user) {
    var amount = new TextPrompt<decimal>("How much do you want to [green]add[/]?")
        .Validate(a => a >= 0, "[red]Amount must be positive[/]")
        .Show(ansi);

    ansi.WriteLine($"Adding [{User.BalanceColor(amount)}]{amount:C}[/] to ", user.AccountBalanceMarkup);
    user.IncrementBalance(amount);
    ansi.WriteLine($"Account Balance now ", user.AccountBalanceMarkup);
    
    return true;
}

// Prompts the user for an amount to remove from the logged in user's balance
bool DecBalance(User user) {
    var amount = new TextPrompt<decimal>("How much do you want to [red]remove[/]?")
        .Validate(a => a >= 0, "[red]Amount must be positive[/]")
        .Show(ansi);
    
    try {
        var oldMarkup = user.AccountBalanceMarkup;
        user.DecrementBalance(amount);
        ansi.WriteLine($"Removing [{User.BalanceColor(amount * -1)}]{amount:C}[/] from ", oldMarkup);
    } catch (BalanceOverdrawException ex) { ansi.MarkupLine(ex.Message); }
    
    ansi.WriteLine($"Account Balance now ", user.AccountBalanceMarkup);
    return true;
}

// Lists all of the user's items
bool ListItems(User user) {
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

// Adds an item to the user's list of items
bool AddItem(User user) {
    var name = new TextPrompt<string>("What is the [green]name[/] of the item you wish to add?")
        .Validate(n => !string.IsNullOrWhiteSpace(n), "[red]Name cannot be empty[/]")
        .Show(ansi);
        
    var price = new TextPrompt<decimal>("What is the [blue]price[/] of the item?")
        .Validate(p => p >= 0, "[red]Price must be positive[/]")
        .Show(ansi);
    
    user.AddItem(name, price);
    return true;
}

// Removes an item from the users's list of items
bool RemoveItem(User user) {
    var item = new SelectionPrompt<Item>()
        .Title("What is the [green]name[/] of the item you wish to remove?")
        .AddChoices(user.Items.Prepend(new("<cancel>", 0))).UseConverter(item => item.Name)
        .Show(ansi);
    
    user.RemoveItem(item);
    return true;
}

// I don't even know how to describe this monstrosity
(SelTuple, IEnumerable<SelTuple>)[] selGroups = {
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
SelTuple SENTINEL = ("Quit", _ => false);

// Displays a menu of available options and continuously prompts the user for input until they quit
void DoMenu(User user) {
    var menu = new SelectionPrompt<SelTuple>()
        .Title("[bold]What do you want to do?[/]")
        .AddGroups(selGroups).AddChoices(SENTINEL)
        .UseConverter(s => s.Item1);
    
    while (menu.Show(ansi).Item2(user)) {}
}

/* Main */ {
    Console.OutputEncoding = Encoding.UTF8;

    if (args.Length != 1) {
        ansi.WriteLine("Usage: `dotnet run -- <users database file>`");
        Environment.Exit(1);
    }
    
    var path = args[0];
    
    Console.CancelKeyPress += delegate {
        ansi.WriteLine();
        if (new ConfirmationPrompt("Do you want to save what you have?").Show(ansi)) {
            SaveUsers(path);
        }
    };
    
    LoadUsers(path);
    DoMenu(Logon());
    SaveUsers(path);
}
