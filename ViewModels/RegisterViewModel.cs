using System.ComponentModel.DataAnnotations;
namespace UserManagement.ViewModels;

public class RegisterViewModel
{
    [Required]
    public string Name {get; set;} = "";

    [Required]
    [EmailAddress]
    public string Email {get; set;} = "";

    [Required]
    public string Password {get; set;} = "";
}