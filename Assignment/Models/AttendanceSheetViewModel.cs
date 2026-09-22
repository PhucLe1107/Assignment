using System.ComponentModel.DataAnnotations;

namespace Assignment.Models
{
    public class AttendanceSheetViewModel
    {
        [Required(ErrorMessage = "Vui lòng chọn lớp học")]
        public int ClassId { get; set; }

        public string? ClassName { get; set; }
        public string? ClassCode { get; set; }
        public int TotalSessions { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập buổi học số")]
        [Range(1, 200, ErrorMessage = "Buổi học phải từ 1 đến 200")]
        public int SessionNumber { get; set; } = 1;

        [Required(ErrorMessage = "Vui lòng chọn ngày điểm danh")]
        [DataType(DataType.Date)]
        public DateTime AttendanceDate { get; set; } = DateTime.Today;

        public List<AttendanceStudentItem> Students { get; set; } = new();
    }

    public class AttendanceStudentItem
    {
        public int EnrollmentId { get; set; }
        public int? AttendanceId { get; set; }
        public string StudentCode { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public bool IsPresent { get; set; } = true;
        public string? Note { get; set; }
    }
}