using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Assignment.Models.Entities
{
    public class Enrollment
    {
        [Key]
        public int EnrollmentId { get; set; }

        [Required]
        [Display(Name = "Sinh viên")]
        public int StudentId { get; set; }

        [Required]
        [Display(Name = "Lớp học")]
        public int ClassId { get; set; }

        [DataType(DataType.DateTime)]
        [Display(Name = "Ngày đăng ký")]
        public DateTime EnrollmentDate { get; set; } = DateTime.Now;

        [Required]
        [Display(Name = "Học phí thực tế (VNĐ)")]
        [Range(0, 100000000)]
        [Precision(18, 2)]
        public decimal ActualFee { get; set; }

        [Display(Name = "Số tiền đã nộp (VNĐ)")]
        [Range(0, 100000000)]
        [Precision(18, 2)]
        public decimal PaidAmount { get; set; } = 0;

        [StringLength(30)]
        [Display(Name = "Tình trạng học phí")]
        public string PaymentStatus { get; set; } = "ConNo";

        [StringLength(30)]
        [Display(Name = "Trạng thái học tập")]
        public string LearningStatus { get; set; } = "DangHoc";

        [ForeignKey("StudentId")]
        public virtual Student Student { get; set; } = null!;

        [ForeignKey("ClassId")]
        public virtual Class Class { get; set; } = null!;

        public virtual ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();
        public virtual ICollection<Grade> Grades { get; set; } = new List<Grade>();
    }
}
