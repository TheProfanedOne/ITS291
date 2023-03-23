namespace ITS291; 

public class BalanceOverdrawException : Exception {
    public BalanceOverdrawException()
        : base("Amount must be less than or equal to the account balance when [orangered1]preventOverdraw[/] is set.") {}
    public BalanceOverdrawException(string msg) : base(msg) {}
}
