using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TradingJournal.Application.DTOs.Accounts;
using TradingJournal.Application.Interfaces;
using System.Security.Claims;

namespace TradingJournal.API.Controllers;

[Authorize]
[ApiController]
[Route("api/accounts")]
public class TradingAccountController : ControllerBase
{
    private readonly ITradingAccountService _accountService;
    private readonly ILogger<TradingAccountController> _logger;

    public TradingAccountController(ITradingAccountService accountService, ILogger<TradingAccountController> logger)
    {
        _accountService = accountService;
        _logger = logger;
    }

    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private bool IsAdmin => User.IsInRole("Admin");

    [HttpGet]
    public async Task<ActionResult<List<TradingAccountDto>>> GetAccounts([FromQuery] Guid? targetUserId)
    {
        var userId = IsAdmin && targetUserId.HasValue ? targetUserId.Value : CurrentUserId;
        _logger.LogInformation("Getting trading accounts for user {UserId}", userId);
        var accounts = await _accountService.GetUserAccountsAsync(userId);
        return Ok(accounts);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TradingAccountDto>> GetAccount(Guid id, [FromQuery] Guid? targetUserId)
    {
        var userId = IsAdmin && targetUserId.HasValue ? targetUserId.Value : CurrentUserId;
        var account = await _accountService.GetAccountByIdAsync(id, userId);
        if (account == null) return NotFound();
        return Ok(account);
    }

    [HttpPost]
    public async Task<ActionResult<TradingAccountDto>> CreateAccount([FromBody] CreateTradingAccountDto dto)
    {
        var userId = IsAdmin && dto.UserId.HasValue ? dto.UserId.Value : CurrentUserId;
        _logger.LogInformation("Creating new trading account for user {UserId}", userId);
        var created = await _accountService.CreateAccountAsync(dto, userId);
        return CreatedAtAction(nameof(GetAccount), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<TradingAccountDto>> UpdateAccount(Guid id, [FromBody] UpdateTradingAccountDto dto)
    {
        if (id != dto.Id) return BadRequest("ID mismatch");
        var userId = IsAdmin && dto.UserId.HasValue ? dto.UserId.Value : CurrentUserId;
        
        _logger.LogInformation("Updating trading account {AccountId} for user {UserId}", id, userId);
        try
        {
            var updated = await _accountService.UpdateAccountAsync(dto, userId);
            return Ok(updated);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating trading account {AccountId}", id);
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAccount(Guid id, [FromQuery] Guid? targetUserId)
    {
        var userId = IsAdmin && targetUserId.HasValue ? targetUserId.Value : CurrentUserId;
        _logger.LogInformation("Deleting trading account {AccountId} for user {UserId}", id, userId);
        await _accountService.DeleteAccountAsync(id, userId);
        return NoContent();
    }
}
