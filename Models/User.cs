using Microsoft.EntityFrameworkCore;
namespace UserManagement.Models;

[Index(nameof(Email), IsUnique = true)]
public class User
{
    public int Id { get; set;}
    public string Name { get; set;} = string.Empty;
    public string Email { get; set;} = string.Empty;
    public string PasswordHash { get; set;} = string.Empty;
    public UserStatus Status { get; set;} = UserStatus.Unverified;
    public DateTime RegistrationDate { get; set;}
    public DateTime? LastLogin { get; set;}
    public string? VerificationToken { get; set; }
   

}