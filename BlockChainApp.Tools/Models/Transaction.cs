namespace BlockChainApp.Tools.Models;
public class Transaction
{
    public int Amount { get; set; }
    public string Recipient { get; set; } = string.Empty;
    public string Sender { get; set; } = string.Empty;
}
