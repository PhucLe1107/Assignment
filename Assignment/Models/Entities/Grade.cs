using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Assignment.Models.Entities
{
    public class Grade
    {
        [Key]
        public int GradeId { get; set; }

        [Required]
        [Display(Name = "Đăng ký")]
        public int EnrollmentId { get; set; }

        [Required(ErrorMessage = "Loại kỳ thi không được để trống")]
        [StringLength(50)]
        [Display(Name = "Kỳ kiểm tra")]
        public string ExamType { get; set; }

        [Range(0, 10, ErrorMessage = "Điểm phải từ 0 đến 10")]
        [Display(Name = "Điểm Nghe (Listening)")]
        public double? ListeningScore { get; set; }

        [Range(0, 10, ErrorMessage = "Điểm phải từ 0 đến 10")]
        [Display(Name = "Điểm Đọc (Reading)")]
        public double? ReadingScore { get; set; }

        [Range(0, 10, ErrorMessage = "Điểm phải từ 0 đến 10")]
        [Display(Name = "Điểm Viết (Writing)")]
        public double? WritingScore { get; set; }

        [Range(0, 10, ErrorMessage = "Điểm phải từ 0 đến 10")]
        [Display(Name = "Điểm Nói (Speaking)")]
        public double? SpeakingScore { get; set; }

        [Display(Name = "Điểm trung bình")]
        public double? OverallScore { get; set; }

        [StringLength(500)]
        [Display(Name = "Nhận xét của giáo viên")]
        public string TeacherFeedback { get; set; }

        [ForeignKey("EnrollmentId")]
        public virtual Enrollment Enrollment { get; set; }
    }
}
