using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Http;

namespace Assignment.Models.Entities
{
    public class Course
    {
        [Key]
        public int CourseId { get; set; }

        [Required(ErrorMessage = "Tên khóa học không được để trống")]
        [StringLength(100, ErrorMessage = "Tên khóa học không quá 100 ký tự")]
        [Display(Name = "Tên khóa học")]
        public string CourseName { get; set; } = string.Empty;

        [StringLength(255)]
        [Display(Name = "Ảnh bìa khóa học")]
        public string? ThumbnailUrl { get; set; }

        [NotMapped]
        [Display(Name = "Chọn tệp ảnh bìa")]
        public IFormFile? ThumbnailFile { get; set; }

        [Required(ErrorMessage = "Học phí chuẩn không được để trống")]
        [Range(0, 100000000, ErrorMessage = "Học phí phải lớn hơn hoặc bằng 0")]
        [Display(Name = "Học phí niêm yết (VNĐ)")]
        public decimal BaseTuitionFee { get; set; }

        [Required(ErrorMessage = "Tổng số buổi không được để trống")]
        [Range(1, 200, ErrorMessage = "Số buổi học phải từ 1 đến 200")]
        [Display(Name = "Tổng số buổi học")]
        public int TotalSessions { get; set; }

        [Required(ErrorMessage = "Giáo trình chi tiết không được để trống")]
        [Display(Name = "Giáo trình chi tiết")]
        [Column(TypeName = "ntext")]
        public string? DescriptionHtml { get; set; }

        [Display(Name = "Đang tuyển sinh")]
        public bool IsActive { get; set; } = true;

        public virtual ICollection<Class>? Classes { get; set; }
    }
}
