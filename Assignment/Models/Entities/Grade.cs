using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Assignment.Models.Entities
{
    public class Grade
    {
        [Key]
        public int GradeId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn thông tin ghi danh của học viên")]
        [Display(Name = "Hồ sơ ghi danh")]
        public int EnrollmentId { get; set; }

        [Required(ErrorMessage = "Kỳ kiểm tra không được để trống")]
        [StringLength(50, ErrorMessage = "Tên kỳ kiểm tra không quá 50 ký tự")]
        [Display(Name = "Kỳ kiểm tra")]
        public string ExamType { get; set; } = "GiuaKy";

        [Range(0, 990, ErrorMessage = "Điểm Nghe không hợp lệ (từ 0 đến 990)")]
        [Display(Name = "Điểm Nghe (Listening)")]
        public double? ListeningScore { get; set; }

        [Range(0, 990, ErrorMessage = "Điểm Đọc không hợp lệ (từ 0 đến 990)")]
        [Display(Name = "Điểm Đọc (Reading)")]
        public double? ReadingScore { get; set; }

        [Range(0, 990, ErrorMessage = "Điểm Viết không hợp lệ (từ 0 đến 990)")]
        [Display(Name = "Điểm Viết (Writing)")]
        public double? WritingScore { get; set; }

        [Range(0, 990, ErrorMessage = "Điểm Nói không hợp lệ (từ 0 đến 990)")]
        [Display(Name = "Điểm Nói (Speaking)")]
        public double? SpeakingScore { get; set; }

        [Range(0, 990, ErrorMessage = "Điểm tổng kết không hợp lệ (từ 0 đến 990)")]
        [Display(Name = "Điểm tổng kết / Trung bình")]
        public double? OverallScore { get; set; }

        [StringLength(500, ErrorMessage = "Nhận xét tối đa 500 ký tự")]
        [Display(Name = "Nhận xét của giáo viên")]
        public string? TeacherFeedback { get; set; } = string.Empty;

        [ForeignKey("EnrollmentId")]
        public virtual Enrollment? Enrollment { get; set; }
    }
}