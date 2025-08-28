// Shared DTOs for TestDataGenerator - eliminates cross-project dependencies
namespace TestDataGenerator.Shared;

#if CI_BUILD
// CI Build - use local models instead of project references

public class RegisterDto
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? ProfilePicture { get; set; }
}

public class User
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string? UserName { get; set; }
    public string? NormalizedUserName { get; set; }
    public string? Email { get; set; }
    public string? NormalizedEmail { get; set; }
    public bool EmailConfirmed { get; set; }
    public string? PasswordHash { get; set; }
    public string? SecurityStamp { get; set; }
    public string? ConcurrencyStamp { get; set; }
    public string? PhoneNumber { get; set; }
    public bool PhoneNumberConfirmed { get; set; }
    public bool TwoFactorEnabled { get; set; }
    public DateTimeOffset? LockoutEnd { get; set; }
    public bool LockoutEnabled { get; set; }
    public int AccessFailedCount { get; set; }
    
    // Custom fields
    public DateTime? DateOfBirth { get; set; }
    public string? Bio { get; set; }
    public string? ProfilePicture { get; set; }
    public string? Gender { get; set; }
    public string? Location { get; set; }
    public string? Interests { get; set; }
    public DateTime? LastActive { get; set; }
}

public class UserProfile
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Bio { get; set; }
    public string? ProfilePictureUrl { get; set; }
    public string? Preferences { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? Gender { get; set; }
    public string? Location { get; set; }
    public string? Interests { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastActiveAt { get; set; }
    public bool IsVerified { get; set; }
}

#else
// Local Build - use actual project references
using AuthService.DTOs;
using AuthService.Models;
using UserService.Models;
#endif
