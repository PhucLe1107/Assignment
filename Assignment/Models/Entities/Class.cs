using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Assignment.Models.Entities
{
    public class Class
    {
        [Key]
        public int ClassId { get; set; }

        [Required(ErrorMessage = "Mã lớp không được để trống")]
        [StringLength(20, ErrorMessage = "Mã lớp không quá 20 ký tự")]
        [Display(Name = "Mã lớp")]
        public string ClassCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tên lớp không được để trống")]
        [StringLength(100)]
        [Display(Name = "Tên lớp học")]
        public string ClassName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn khóa học")]
        [Display(Name = "Khóa học")]
        public int CourseId { get; set; }

        [StringLength(100)]
        [Display(Name = "Giáo viên đứng lớp")]
        public string LecturerName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Lịch học không được để trống")]
        [StringLength(100)]
        [Display(Name = "Thời khóa biểu")]
        public string Schedule { get; set; } = string.Empty;

        [StringLength(50)]
        [Display(Name = "Phòng học")]
        public string Room { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ngày khai giảng không được để trống")]
        [DataType(DataType.Date)]
        [Display(Name = "Ngày khai giảng")]
        public DateTime StartDate { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Ngày bế giảng (Dự kiến)")]
        public DateTime? EndDate { get; set; }

        [Range(1, 100, ErrorMessage = "Sĩ số phải từ 1 đến 100 học viên")]
        [Display(Name = "Sĩ số tối đa")]
        public int MaxCapacity { get; set; } = 20;

        [ForeignKey("CourseId")]
        public virtual Course Course { get; set; } = null!;

        public virtual ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    }
}
