using BlockChainApp.Tools.Models;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace BlockChainApp.Tools.Services
{
    /// <summary>
    /// The blockchain
    /// </summary>
    public class BlockChain
    {
        private readonly List<Transaction> _currentTransactions = new();

        private List<Block> _chain = new();

        private readonly List<Node> _nodes = new();

        private static readonly HttpClient client = new();

        private Block LastBlock => _chain.Last();

        public string NodeId { get; private set; }

        public BlockChain()
        {
            NodeId = Guid.NewGuid().ToString().Replace("-", "");
            CreateNewBlock(proof: 100, previousHash: "1");
        }

        /// <summary>
        /// The mining, please buy videocards while it's not too late
        /// </summary>
        /// <returns>Result response</returns>
        public MineResponse Mine()
        {
            int proof = CreateProofOfWork(LastBlock.Proof, LastBlock.PreviousHash ?? string.Empty);

            CreateTransaction(sender: "0", recipient: NodeId, amount: 1);
            Block block = CreateNewBlock(proof /*, _lastBlock.PreviousHash*/);

            return new MineResponse
            {
                Message = "New Block Forged",
                Index = block.Index,
                Transactions = block.Transactions,
                Proof = block.Proof,
                PreviousHash = block.PreviousHash
            };
        }

        /// <summary>
        /// The current full chain
        /// </summary>
        /// <returns></returns>
        public FullChain GetFullChain()
        {
            return new FullChain
            {
                Chain = _chain,
                Length = _chain.Count
            };
        }

        /// <summary>
        /// Register multiple nodes at a time
        /// </summary>
        /// <param name="nodes">Nodes to register</param>
        /// <returns>Registered nodes</returns>
        public string RegisterNodes(List<string> nodes)
        {
            var builder = new StringBuilder();
            foreach (string node in nodes)
            {
                string url = $"http://{node}";
                RegisterNode(url);
                builder.Append($"{url}, ");
            }

            builder.Insert(0, $"{nodes.Count} new nodes have been added: ");
            string result = builder.ToString();
            return result[0..^2];
        }

        /// <summary>
        /// The consensus
        /// </summary>
        /// <returns>Consensus</returns>
        public async Task<ConsensusModel> Consensus()
        {
            bool replaced = await ResolveConflicts();
            string message = replaced ? "was replaced" : "is authoritive";

            return new ConsensusModel
            {
                Message = $"Our chain {message}",
                Chain = _chain
            };
        }

        /// <summary>
        /// Create a brand new transaction
        /// </summary>
        /// <param name="sender">Sender's address</param>
        /// <param name="recipient">Recipient's address</param>
        /// <param name="amount">Amount to transfer</param>
        /// <returns>Index of the last block</returns>
        public int CreateTransaction(string sender, string recipient, int amount)
        {
            var transaction = new Transaction
            {
                Sender = sender,
                Recipient = recipient,
                Amount = amount
            };

            _currentTransactions.Add(transaction);

            return LastBlock is not null ? LastBlock.Index + 1 : 0;
        }

        private void RegisterNode(string address)
        {
            if (!string.IsNullOrEmpty(address))
                _nodes.Add(new Node { Address = new Uri(address) });
        }

        private bool IsValidChain(List<Block> chain)
        {
            Block lastBlock = chain.First();

            for (int i = 1; i < chain.Count; i++)
            {
                Block? block = chain.ElementAt(i);
                Debug.WriteLine($"{lastBlock}");
                Debug.WriteLine($"{block}");
                Debug.WriteLine("----------------------------");

                // Check that the hash of the block is correct
                if (block.PreviousHash != GetHash(lastBlock))
                    return false;

                // Check that the Proof of Work is correct
                if (lastBlock.PreviousHash is not null &&
                    !IsValidProof(lastBlock.Proof, block.Proof, lastBlock.PreviousHash))
                    return false;

                lastBlock = block;
            }

            return true;
        }

        private async Task<bool> ResolveConflicts()
        {
            List<Block>? newChain = null;

            foreach (Node node in _nodes)
            {
                if (node.Address is null)
                    continue;

                var url = new Uri(node.Address, "/chain");

                try
                {
                    string responseBody = await client.GetStringAsync(url);

                    var data = JsonConvert.DeserializeObject<FullChain>(responseBody);

                    if (data is not null &&
                        data.Chain?.Count > _chain.Count &&
                        IsValidChain(data.Chain))
                    {
                        newChain = data.Chain;
                    }
                }
                catch (HttpRequestException e)
                {
                    Console.WriteLine("\nException Caught!");
                    Console.WriteLine("Message :{0} ", e.Message);
                }
            }

            if (newChain is not null)
            {
                _chain = newChain;
                return true;
            }

            return false;
        }

        private Block CreateNewBlock(int proof, string? previousHash = null)
        {
            var block = new Block
            {
                Index = _chain.Count,
                Timestamp = DateTime.UtcNow,
                Transactions = _currentTransactions.ToList(),
                Proof = proof,
                PreviousHash = previousHash ?? GetHash(_chain.Last())
            };

            _currentTransactions.Clear();
            _chain.Add(block);
            return block;
        }

        private int CreateProofOfWork(int lastProof, string previousHash)
        {
            int proof = 0;
            while (!IsValidProof(lastProof, proof, previousHash))
                proof++;

            return proof;
        }

        private bool IsValidProof(int lastProof, int proof, string previousHash)
        {
            string guess = $"{lastProof}{proof}{previousHash}";
            string result = GetSha256(guess);
            return result.StartsWith("0000");
        }

        private string GetHash(Block block)
        {
            string blockText = JsonConvert.SerializeObject(block);
            return GetSha256(blockText);
        }

        private string GetSha256(string data)
        {
            var sha256 = SHA256.Create();
            var hashBuilder = new StringBuilder();

            byte[] bytes = Encoding.Unicode.GetBytes(data);
            byte[] hash = sha256.ComputeHash(bytes);

            foreach (byte x in hash)
                hashBuilder.Append($"{x:x2}");

            return hashBuilder.ToString();
        }
    }
}
