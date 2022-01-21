namespace BlockChainApp.Tools.Models
{
    public class MineResponse
    {
        public string? Message { get; set; }
        public int Index { get; set; }
        public List<Transaction>? Transactions { get; set; }
        public int Proof { get; set; }
        public string? PreviousHash { get; set; }
    }
}
