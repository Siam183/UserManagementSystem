using UserManagement.Models;
namespace UserManagement.ViewModels;

public class UserManagementViewModel
{
    public List<User> Users { get; set;} = new();
    public List<int> SelectedUserIds { get; set;} = new();

}