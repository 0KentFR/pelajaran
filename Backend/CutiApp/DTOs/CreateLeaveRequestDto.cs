using System.ComponentModel.DataAnnotations;

namespace CutiApp.DTOs
{
    public class CreateLeaveRequestDto
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        [Required]
        [MinLength(10)]
        [MaxLength(500)]
        public string Reason { get; set; } = string.Empty;
    }
}