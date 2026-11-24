using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CutiApp.DTOs;
using CutiApp.Exceptions;
using CutiApp.Services;

namespace CutiApp.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/leave-requests")]
    public class LeaveRequestsController : ControllerBase
    {
        private readonly ILeaveService _leaveService;

        public LeaveRequestsController(ILeaveService leaveService)
        {
            _leaveService = leaveService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            List<LeaveRequestResponse> result = await _leaveService.GetAllAsync();
            return Ok(result);
        }

        [HttpGet("mine")]
        public async Task<IActionResult> GetMine()
        {
            List<LeaveRequestResponse> result = await _leaveService.GetMyRequestsAsync(GetUserId());
            return Ok(result);
        }

        [HttpGet("balance")]
        public async Task<IActionResult> GetBalance()
        {
            try
            {
                LeaveBalanceResponse result = await _leaveService.GetMyBalanceAsync(GetUserId());
                return Ok(result);
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateLeaveRequestDto dto)
        {
            try
            {
                LeaveRequestResponse result = await _leaveService.CreateAsync(GetUserId(), dto);
                return StatusCode(StatusCodes.Status201Created, result);
            }
            catch (BadRequestException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpPut("{id}/approve")]
        public async Task<IActionResult> Approve(int id)
        {
            try
            {
                await _leaveService.ApproveAsync(id);
                return Ok(new { message = "Pengajuan cuti disetujui" });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidStatusException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (InsufficientBalanceException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        [HttpPut("{id}/reject")]
        public async Task<IActionResult> Reject(int id)
        {
            try
            {
                await _leaveService.RejectAsync(id);
                return Ok(new { message = "Pengajuan cuti ditolak" });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidStatusException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Cancel(int id)
        {
            try
            {
                await _leaveService.CancelAsync(GetUserId(), id);
                return Ok(new { message = "Pengajuan cuti dibatalkan" });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidStatusException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        private int GetUserId()
        {
            string userId = User.FindFirst(ClaimTypes.NameIdentifier)!.Value;
            return int.Parse(userId);
        }
    }
}