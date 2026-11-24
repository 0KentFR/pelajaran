using Microsoft.EntityFrameworkCore.Storage;
using CutiApp.Data;
using CutiApp.DTOs;
using CutiApp.Exceptions;
using CutiApp.Models;
using CutiApp.Repositories;

namespace CutiApp.Services
{
    public class LeaveService : ILeaveService
    {
        private const string StatusPending = "Pending";
        private const string StatusApproved = "Approved";
        private const string StatusRejected = "Rejected";

        private readonly AppDbContext _context;
        private readonly IRepository<LeaveRequest> _leaveRepo;
        private readonly IRepository<LeaveBalance> _balanceRepo;
        private readonly IRepository<User> _userRepo;

        public LeaveService(
            AppDbContext context,
            IRepository<LeaveRequest> leaveRepo,
            IRepository<LeaveBalance> balanceRepo,
            IRepository<User> userRepo)
        {
            _context = context;
            _leaveRepo = leaveRepo;
            _balanceRepo = balanceRepo;
            _userRepo = userRepo;
        }

        public async Task<List<LeaveRequestResponse>> GetAllAsync()
        {
            List<LeaveRequest> requests = await _leaveRepo.GetAllAsync(null, r => r.User);

            return requests
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => ToResponse(r))
                .ToList();
        }

        public async Task<List<LeaveRequestResponse>> GetMyRequestsAsync(int userId)
        {
            List<LeaveRequest> requests = await _leaveRepo.GetAllAsync(r => r.UserId == userId, r => r.User);

            return requests
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => ToResponse(r))
                .ToList();
        }

        public async Task<LeaveBalanceResponse> GetMyBalanceAsync(int userId)
        {
            LeaveBalance? balance = await _balanceRepo.FirstOrDefaultAsync(b => b.UserId == userId);

            if (balance == null)
            {
                throw new NotFoundException("Saldo cuti tidak ditemukan");
            }

            return new LeaveBalanceResponse
            {
                RemainingDays = balance.RemainingDays
            };
        }

        public async Task<LeaveRequestResponse> CreateAsync(int userId, CreateLeaveRequestDto dto)
        {
            if (dto.StartDate == default(DateTime) || dto.EndDate == default(DateTime))
            {
                throw new BadRequestException("Tanggal mulai dan tanggal selesai wajib diisi");
            }

            if (dto.EndDate.Date < dto.StartDate.Date)
            {
                throw new BadRequestException("Tanggal selesai tidak boleh sebelum tanggal mulai");
            }

            User? user = await _userRepo.GetByIdAsync(userId);

            if (user == null)
            {
                throw new NotFoundException("User tidak ditemukan");
            }

            LeaveRequest request = new LeaveRequest
            {
                UserId = userId,
                StartDate = dto.StartDate.Date,
                EndDate = dto.EndDate.Date,
                Reason = dto.Reason,
                Status = StatusPending,
                CreatedAt = DateTime.UtcNow,
                User = user
            };

            await _leaveRepo.AddAsync(request);
            await _context.SaveChangesAsync();

            return ToResponse(request);
        }

        public async Task ApproveAsync(int id)
        {
            LeaveRequest? request = await _leaveRepo.GetByIdAsync(id);

            if (request == null)
            {
                throw new NotFoundException("Pengajuan cuti tidak ditemukan");
            }

            if (request.Status != StatusPending)
            {
                throw new InvalidStatusException("Pengajuan ini sudah berstatus " + request.Status + ", hanya yang Pending yang bisa disetujui");
            }

            LeaveBalance? balance = await _balanceRepo.FirstOrDefaultAsync(b => b.UserId == request.UserId);

            if (balance == null)
            {
                throw new NotFoundException("Saldo cuti pemilik pengajuan tidak ditemukan");
            }

            int days = CountDays(request.StartDate, request.EndDate);

            if (balance.RemainingDays < days)
            {
                throw new InsufficientBalanceException(
                    "Saldo cuti tidak cukup. Sisa " + balance.RemainingDays + " hari, diajukan " + days + " hari");
            }

            await using IDbContextTransaction transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                request.Status = StatusApproved;
                balance.RemainingDays -= days;

                _leaveRepo.Update(request);
                _balanceRepo.Update(balance);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task RejectAsync(int id)
        {
            LeaveRequest? request = await _leaveRepo.GetByIdAsync(id);

            if (request == null)
            {
                throw new NotFoundException("Pengajuan cuti tidak ditemukan");
            }

            if (request.Status != StatusPending)
            {
                throw new InvalidStatusException("Pengajuan ini sudah berstatus " + request.Status + ", hanya yang Pending yang bisa ditolak");
            }

            request.Status = StatusRejected;

            _leaveRepo.Update(request);
            await _context.SaveChangesAsync();
        }

        public async Task CancelAsync(int userId, int id)
        {
            LeaveRequest? request = await _leaveRepo.GetByIdAsync(id);

            if (request == null || request.UserId != userId)
            {
                throw new NotFoundException("Pengajuan cuti tidak ditemukan");
            }

            if (request.Status != StatusPending)
            {
                throw new InvalidStatusException("Pengajuan yang sudah " + request.Status + " tidak bisa dibatalkan");
            }

            _leaveRepo.Delete(request);
            await _context.SaveChangesAsync();
        }

        private static int CountDays(DateTime startDate, DateTime endDate)
        {
            return (endDate.Date - startDate.Date).Days + 1;
        }

        private static LeaveRequestResponse ToResponse(LeaveRequest request)
        {
            return new LeaveRequestResponse
            {
                Id = request.Id,
                UserId = request.UserId,
                FullName = request.User.FullName,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                Reason = request.Reason,
                Status = request.Status,
                CreatedAt = request.CreatedAt
            };
        }
    }
}