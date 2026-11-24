using CutiApp.DTOs;

namespace CutiApp.Services
{
    public interface ILeaveService
    {
        Task<List<LeaveRequestResponse>> GetAllAsync();
        Task<List<LeaveRequestResponse>> GetMyRequestsAsync(int userId);
        Task<LeaveBalanceResponse> GetMyBalanceAsync(int userId);
        Task<LeaveRequestResponse> CreateAsync(int userId, CreateLeaveRequestDto dto);
        Task ApproveAsync(int id);
        Task RejectAsync(int id);
        Task CancelAsync(int userId, int id);
    }
}