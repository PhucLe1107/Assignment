using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Assignment.Models.Entities
{
    public class Attendance
    {
        [Key]
        public int AttendanceId { get; set; }

        [Required]
        [Display(Name = "Đăng ký")]
        public int EnrollmentId { get; set; }

        [Required]
        [Display(Name = "Buổi học số")]
        public int SessionNumber { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Ngày điểm danh")]
        public DateTime AttendanceDate { get; set; }

        [Display(Name = "Có mặt")]
        public bool IsPresent { get; set; } = true;

        [StringLength(255)]
        [Display(Name = "Ghi chú")]
        public string Note { get; set; }

        [ForeignKey("EnrollmentId")]
        public virtual Enrollment Enrollment { get; set; }
    }
}
