using System.ComponentModel.DataAnnotations;

namespace CutiApp.Models;

public class User
{
    public int Id { get; set; }

    [MaxLength(50)]
    public string Username { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Password { get; set; } = string.Empty;

    [MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    [MaxLength(20)]
    public string Role { get; set; } = string.Empty;

    public List<LeaveRequest> LeaveRequests { get; set; } = new();
    public LeaveBalance? LeaveBalances { get; set; }
}