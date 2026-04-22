using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TradingJournal.Application.DTOs.Auth;
using TradingJournal.Application.Interfaces;

namespace TradingJournal.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly IAuthService _authService;
    private readonly ILogger<AdminController> _logger;

    public AdminController(IUserRepository userRepository, IAuthService authService, ILogger<AdminController> logger)
    {
        _userRepository = userRepository;
        _authService = authService;
        _logger = logger;
    }

    /// <summary>Get all users (Admin only)</summary>
    [HttpGet("users")]
    public async Task<ActionResult<List<UserInfoDto>>> GetAllUsers()
    {
        try
        {
            _logger.LogInformation("Admin fetching all users.");
            var users = await _userRepository.GetAllAsync();
            var result = users.Select(u => new UserInfoDto
            {
                Id = u.Id,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Email = u.Email,
                Role = u.Role.ToString(),
                AccountBalance = u.AccountBalance,
                AllowedSections = u.AllowedSections
            }).ToList();
            
            _logger.LogInformation("Successfully fetched {Count} users.", result.Count);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching all users from Admin interface.");
            return StatusCode(500, new { error = "An error occurred while fetching users." });
        }
    }

    /// <summary>Create a new user (Admin only)</summary>
    [HttpPost("users")]
    public async Task<ActionResult<UserInfoDto>> CreateUser([FromBody] AdminCreateUserDto dto)
    {
        try
        {
            _logger.LogInformation("Admin creating user: {Email}", dto.Email);
            var result = await _authService.AdminCreateUserAsync(dto);
            _logger.LogInformation("Successfully created user: {Email}", dto.Email);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user from Admin interface.");
            return StatusCode(500, new { error = "An error occurred while creating the user." });
        }
    }

    /// <summary>Update an existing user (Admin only)</summary>
    [HttpPut("users/{id}")]
    public async Task<ActionResult<UserInfoDto>> UpdateUser(Guid id, [FromBody] AdminUpdateUserDto dto)
    {
        try
        {
            _logger.LogInformation("Admin updating user: {Id}", id);
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
            {
                return NotFound(new { error = "User not found." });
            }

            user.FirstName = dto.FirstName;
            user.LastName = dto.LastName;
            user.Email = dto.Email;
            
            if (Enum.TryParse<TradingJournal.Domain.Enums.UserRole>(dto.Role, out var role))
            {
                user.Role = role;
            }
            user.AccountBalance = dto.AccountBalance;
            user.AllowedSections = dto.Role == "Admin" ? new List<string>() : dto.AllowedSections;

            await _userRepository.UpdateAsync(user);

            return Ok(new UserInfoDto
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                Role = user.Role.ToString(),
                AccountBalance = user.AccountBalance,
                AllowedSections = user.AllowedSections
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user.");
            return StatusCode(500, new { error = "An error occurred while updating the user." });
        }
    }
}
