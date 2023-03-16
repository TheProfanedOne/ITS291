IAnsiConsole ansi = AnsiConsole.Create(new AnsiConsoleSettings {
    Ansi = AnsiSupport.Detect,
    ColorSystem = ColorSystemSupport.Detect,
});

string[] logins = {
    "user1",
    "user2",
    "user3"
};

string[] passwords = {
    "password1",
    "password2",
    "password3"
};

decimal[] accountBalances = {
    5.00M,
    0.00M,
    -5.00M
};

string BalanceColor(decimal balance) {
    return balance switch {
        > 0 => "green",
        < 0 => "red",
        _   => "yellow"
    };
}

string AccountBalanceColor(int userId) {
    return BalanceColor(accountBalances[userId]);
}

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

bool DoMenu(int userId) {
    string[] selValues = {
        "Increment Balance",
        "Decrement Balance",
        "List Users",
        "Show User Details",
        "Quit"
    };

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
