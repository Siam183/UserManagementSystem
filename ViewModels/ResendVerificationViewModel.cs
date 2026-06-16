using System.ComponentModel.DataAnnotations;

namespace UserManagement.ViewModels;

public class ResendVerificationViewModel
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}