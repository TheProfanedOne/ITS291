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
    
    AnsiConsole.Prompt(new TextPrompt<string>("Enter [cyan1]password[/]?")
        .Secret().PromptStyle("mediumorchid1_1")
        .Validate(p => p == passwords[userId], "[red]invalid password[/]"));

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
    var zipped = logins.Zip(passwords, accountBalances); int id = 0;
    foreach ((string name, string password, decimal balance) in zipped) {
        table.AddRow(
            $"[yellow]{id}[/]",
            $"[green]{name}[/]",
            $"[mediumorchid1_1]{password}[/]",
            $"[{AccountBalanceColor(id++)}]{balance:C}[/]"
        );
    }
    AnsiConsole.Write(table);
}

void Main() {
    Console.OutputEncoding = System.Text.Encoding.UTF8;
    Logon();
    ListUsers();
    Console.ReadLine();
}

Main();
