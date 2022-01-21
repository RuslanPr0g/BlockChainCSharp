using BlockChainApp.Tools.Models;
using BlockChainApp.Tools.Services;
using Microsoft.AspNetCore.Mvc;

namespace BlockChainApp.WebServer.Controllers;

[Route("api/[controller]")]
[ApiController]
public class BlockChainController : ControllerBase
{
    private readonly BlockChain _blockChain;

    public BlockChainController(BlockChain blockChain)
    {
        _blockChain = blockChain;
    }

    [HttpGet("mine")]
    public IActionResult Mine()
    {
        return Ok(_blockChain.Mine());
    }

    [HttpGet("chain")]
    public IActionResult Chain()
    {
        return Ok(_blockChain.GetFullChain());
    }

    [HttpGet("nodes/resolve")]
    public async Task<IActionResult> ResolveNodes()
    {
        return Ok(await _blockChain.Consensus());
    }

    [HttpPost("transactions/new")]
    //{ "Amount":123, "Recipient":"ebeabf5cc1d54abdbca5a8fe9493b479", "Sender":"31de2e0ef1cb4937830fcfd5d2b3b24f" }
    public IActionResult NewTransaction(Transaction transaction)
    {
        if (string.IsNullOrEmpty(transaction.Sender) || string.IsNullOrEmpty(transaction.Recipient))
            return BadRequest("Sender and Recipient addresses both must have values.");

        int blockId = _blockChain.CreateTransaction(transaction.Sender,
            transaction.Recipient,
            transaction.Amount);

        return Ok($"Your transaction will be included in block {blockId}");
    }

    [HttpPost("nodes/register")]
    //{ "Urls": ["localhost:54321", "localhost:54345", "localhost:12321"] }
    public IActionResult RegisterNodes(UrlsModel urlsModel)
    {
        if (urlsModel.Urls is null)
            return BadRequest("Please, provide urls to register.");

        return Ok(_blockChain.RegisterNodes(urlsModel.Urls));
    }
}
