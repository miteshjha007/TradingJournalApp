using Microsoft.AspNetCore.Mvc;
using TradingJournal.Application.DTOs.Auth;
using TradingJournal.Application.Interfaces;

namespace TradingJournal.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>Register a new user</summary>
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponseDto>> Register([FromBody] RegisterDto dto)
    {
        try
        {
            _logger.LogInformation("Attempting to register user with email: {Email}", dto.Email);
            var result = await _authService.RegisterAsync(dto);
            _logger.LogInformation("Successfully registered user: {Email}", dto.Email);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while registering user: {Email}", dto.Email);
            return StatusCode(500, new { error = "An error occurred during registration. Please try again." });
        }
    }

    /// <summary>Login and receive JWT</summary>
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginDto dto)
    {
        try
        {
            _logger.LogInformation("Login attempt for email: {Email}", dto.Email);
            var result = await _authService.LoginAsync(dto);
            _logger.LogInformation("Successful login for email: {Email}", dto.Email);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("Invalid login attempt for email: {Email}", dto.Email);
            return Unauthorized(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during login for email: {Email}", dto.Email);
            return StatusCode(500, new { error = "An error occurred during login. Please try again." });
        }
    }

    /// <summary>Login with Google</summary>
    [HttpPost("google")]
    public async Task<ActionResult<AuthResponseDto>> GoogleLogin([FromBody] GoogleAuthDto dto)
    {
        try
        {
            _logger.LogInformation("Attempting Google login.");
            var result = await _authService.GoogleLoginAsync(dto.IdToken);
            _logger.LogInformation("Successfully logged in with Google for user: {Email}", result.User.Email);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("Invalid Google login attempt.");
            return Unauthorized(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during Google login.");
            return StatusCode(500, new { error = "An error occurred during Google login. Please try again." });
        }
    }


    /// <summary>Refresh access token</summary>
    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponseDto>> Refresh([FromBody] RefreshTokenDto dto)
    {
        try
        {
            _logger.LogInformation("Attempting to refresh token.");
            var result = await _authService.RefreshTokenAsync(dto.RefreshToken);
            _logger.LogInformation("Successfully refreshed token.");
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while refreshing token.");
            return StatusCode(500, new { error = "An error occurred during token refresh." });
        }
    }
}
