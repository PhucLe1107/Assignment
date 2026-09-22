using System.ComponentModel.DataAnnotations;

namespace Assignment.Models
{
    public class GradeSheetViewModel
    {
        [Required(ErrorMessage = "Vui lòng chọn lớp học")]
        public int ClassId { get; set; }

        public string? ClassName { get; set; }
        public string? ClassCode { get; set; }
        public string? CourseName { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn hoặc nhập kỳ kiểm tra")]
        [Display(Name = "Kỳ kiểm tra")]
        public string ExamType { get; set; } = "GiuaKy";

        public List<GradeStudentItem> Students { get; set; } = new();
    }

    public class GradeStudentItem
    {
        public int EnrollmentId { get; set; }
        public int? GradeId { get; set; }
        public string StudentCode { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;

        [Range(0, 990, ErrorMessage = "Điểm không hợp lệ (0 - 990)")]
        public double? ListeningScore { get; set; }

        [Range(0, 990, ErrorMessage = "Điểm không hợp lệ (0 - 990)")]
        public double? ReadingScore { get; set; }

        [Range(0, 990, ErrorMessage = "Điểm không hợp lệ (0 - 990)")]
        public double? WritingScore { get; set; }

        [Range(0, 990, ErrorMessage = "Điểm không hợp lệ (0 - 990)")]
        public double? SpeakingScore { get; set; }

        public double? OverallScore { get; set; }

        public string? TeacherFeedback { get; set; }
    }
}