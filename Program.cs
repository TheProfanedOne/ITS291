// See https://aka.ms/new-console-template for more information
IAnsiConsole ansi = AnsiConsole.Create(new AnsiConsoleSettings {
    Ansi = AnsiSupport.Yes,
    ColorSystem = ColorSystemSupport.TrueColor,
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

string AccountBalanceColor(int userId) {
    return accountBalances[userId] switch {
        > 0 => "green",
        < 0 => "red",
        _ => "yellow"
    };
}

int Logon() {
    var name = new TextPrompt<string>("[green]login[/]:")
        .AddChoices(logins).HideChoices()
        .InvalidChoiceMessage("[red]unknown login[/]")
        .Show(ansi);

    var userId = Array.IndexOf(logins, name);
    
    var _ = new TextPrompt<string>("Enter [cyan1]password[/]?")
        .Secret().PromptStyle("mediumorchid1_1")
        .Validate(p => p == passwords[userId], "[red]invalid password[/]")
        .Show(ansi);

    return userId;
}

void ListUsers() {
    Table table = new();
    table.AddColumns(
        new TableColumn("id"),
        new TableColumn("name"),
        new TableColumn("password"),
        new TableColumn("balance")
    );
    var zipped = logins.Zip(passwords, accountBalances); int id = 0;
    foreach ((string name, string password, decimal balance) in zipped) {
        table.AddRow($"{id}", name, password, $"[{AccountBalanceColor(id++)}]{balance:C}[/]");
    }
    ansi.Write(table);
}

void Main() {
    Console.OutputEncoding = System.Text.Encoding.UTF8;
    var _ = Logon();
    ListUsers();
}

Main();
