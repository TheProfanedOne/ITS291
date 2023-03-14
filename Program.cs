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
    var namePrompt = "[bold green]login[/] ([dim]username[/]):";
    var name = AnsiConsole.Prompt(new TextPrompt<string>(namePrompt)
        .AddChoices(logins).HideChoices()
        .InvalidChoiceMessage("[red]unknown login[/]"));

    var userId = Array.IndexOf(logins, name);
    
    var _ = AnsiConsole.Prompt(new TextPrompt<string>("Enter [cyan1]password[/]?")
        .Secret().PromptStyle("mediumorchid1_1")
        .Validate(p => p == passwords[userId], "[red]invalid password[/]"));

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
    
    AnsiConsole.Write(table);
}

void Main() {
    Console.OutputEncoding = System.Text.Encoding.UTF8;
    var _ = Logon();
    ListUsers();
}

Main();
