/*
 * File Name: Program.cs
 * Author: Matthew Mousseau
 * Desc: The entrypoint for my ITS291 project
 */


// IAnsiConsole instance to be used by the various functions
IAnsiConsole ansi = AnsiConsole.Create(new AnsiConsoleSettings {
    Ansi = AnsiSupport.Detect,
    ColorSystem = ColorSystemSupport.Detect
});

// Parallel array of usernames
string[] logins = {
    "user1",
    "user2",
    "user3"
};

// Parallel array of passwords
string[] passwords = {
    "password1",
    "password2",
    "password3"
};

// Parallel array of account balances
decimal[] accountBalances = {
    5.00M,
    0.00M,
    -5.00M
};

// Determines the color to use for a given balance
string BalanceColor(decimal balance) {
    return balance switch {
        > 0 => "green",
        < 0 => "red",
        _   => "yellow"
    };
}

// Determines the color to use for a given user's balance
string AccountBalanceColor(int userId) {
    return BalanceColor(accountBalances[userId]);
}

// Logs the user in and returns their user id
int Logon() {
    const string namePrompt = "[bold green]login[/] ([dim]username[/]):";
    var userId = Array.IndexOf(logins, new TextPrompt<string>(namePrompt)
        .AddChoices(logins).HideChoices()
        .InvalidChoiceMessage("[red]unknown login[/]")
        .Show(ansi));

    var _ = new TextPrompt<string>("Enter [cyan1]password[/]?")
        .Secret().PromptStyle("mediumorchid1_1")
        .Validate(p => p == passwords[userId], "[red]invalid password[/]")
        .Show(ansi);

    return userId;
}

// Creates and displays a table of all users' information
void ListUsers() {
    Table table = new();
    table.AddColumns(
        new TableColumn("[bold yellow]id[/]"),
        new TableColumn("[bold green]name[/]"),
        new TableColumn("[bold mediumorchid1_1]password[/]"),
        new TableColumn("[bold blue]balance[/]")
    );
    var userData = logins.Zip(passwords, accountBalances); int id = 0;
    foreach ((string name, string password, decimal balance) in userData) {
        table.AddRow(
            $"[yellow]{id}[/]",
            $"[green]{name}[/]",
            $"[mediumorchid1_1]{password}[/]",
            $"[{AccountBalanceColor(id++)}]{balance:C}[/]"
        );
    }
    ansi.Write(table);
}

// Creates and displays a table of the logged in user's information
void ShowUserDetails(int userId) {
    ansi.Write(new Table()
        .AddColumns(
            new TableColumn("[bold mediumorchid1_1]Property[/]"),
            new TableColumn("[bold green]Value[/]")
        )
        .AddRow("[mediumorchid1_1]id[/]", $"[green]{userId}[/]")
        .AddRow("[mediumorchid1_1]name[/]", $"[green]{logins[userId]}[/]")
        .AddRow("[mediumorchid1_1]password[/]", $"[green]{passwords[userId]}[/]")
        .AddRow(
            "[mediumorchid1_1]balance[/]",
            $"[{AccountBalanceColor(userId)}]{accountBalances[userId]:C}[/]"
        ));
}

// Prompts the user for an amount to add to the logged in user's balance
void IncBalance(int userId) {
    var amount = new TextPrompt<decimal>("How much do you want to [green]add[/]?")
        .Validate(a => a >= 0, "[red]Amount must be positive[/]")
        .Show(ansi);

    var balColor = BalanceColor(amount);
    var accBalColor = AccountBalanceColor(userId);
    ansi.MarkupLine($"Adding [{balColor}]{amount:C}[/] to [{accBalColor}]{accountBalances[userId]:C}[/]");
    accountBalances[userId] += amount;
    ansi.MarkupLine($"Account Balance now [{AccountBalanceColor(userId)}]{accountBalances[userId]:C}[/]");
}

// Prompts the user for an amount to remove from the logged in user's balance
void DecBalance(int userId) {
    var amount = new TextPrompt<decimal>("How much do you want to [red]remove[/]?")
        .Validate(a => a >= 0, "[red]Amount must be positive[/]")
        .Show(ansi);

    var balColor = BalanceColor(amount * -1);
    var accBalColor = AccountBalanceColor(userId);
    ansi.MarkupLine($"Removing [{balColor}]{amount:C}[/] from [{accBalColor}]{accountBalances[userId]:C}[/]");
    accountBalances[userId] -= amount;
    ansi.MarkupLine($"Account Balance now [{AccountBalanceColor(userId)}]{accountBalances[userId]:C}[/]");
}

// Displays a menu of available options and continuously prompts the user for input until they quit
bool DoMenu(int userId) {
    // Parallel array of menu options
    string[] selValues = {
        "Increment Balance",
        "Decrement Balance",
        "List Users",
        "Show User Details",
        "Quit"
    };

    // Parallel array of function references to call when the corresponding selection is made
    Action[] selActions = {
        () => IncBalance(userId),
        () => DecBalance(userId),
        ListUsers,
        () => ShowUserDetails(userId)
    };
    
    var selection = new SelectionPrompt<string>()
        .Title("[bold]What do you want to do?[/]")
        .AddChoices(selValues)
        .Show(ansi);

    return selection switch {
        "Quit" => false,
        // A rather hacky way of having a multi-line case value in a switch expression
        var sel when selValues.Contains(sel) => ((Func<bool>)(() => {
            selActions[Array.IndexOf(selValues, sel)]();
            return true;
        }))(),
        _ => throw new InvalidOperationException("Invalid Selection")
    };
}

void Main() {
    Console.OutputEncoding = System.Text.Encoding.UTF8;
    var userId = Logon();
    while (DoMenu(userId)) {}
}

Main();
