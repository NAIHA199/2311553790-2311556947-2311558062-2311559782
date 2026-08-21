using System;
using System.ComponentModel.DataAnnotations;

namespace LibraryAdvanced.Models;

public partial class User
{
    [Key]
    public int Id { get; set; }
    [Required]
    public string Username { get; set; } = null!;
    [Required]
    public string Password { get; set; } = null!;
    
    [Required]
    public string DisplayName { get; set; } = null!;
    [EmailAddress]
    public string? Email { get; set; }

    public int RoleId { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime? UpdatedAt { get; set; }

    public virtual Role Role { get; set; } = null!;

    public string? PasswordResetToken { get; set; }
    public DateTime? ResetTokenExpires { get; set; }
}
