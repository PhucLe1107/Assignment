using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Assignment.Models.Entities
{
    public class Student
    {
        [Key]
        public int StudentId { get; set; }

        [Required(ErrorMessage = "Mã sinh viên không được để trống")]
        [StringLength(20, ErrorMessage = "Mã sinh viên không quá 20 ký tự")]
        [Display(Name = "Mã sinh viên")]
        public string StudentCode { get; set; }

        [Required(ErrorMessage = "Họ và tên không được để trống")]
        [StringLength(100, ErrorMessage = "Họ và tên không quá 100 ký tự")]
        [Display(Name = "Họ và tên")]
        public string FullName { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Ngày sinh")]
        public DateTime? DateOfBirth { get; set; }

        [Display(Name = "Giới tính")]
        public bool Gender { get; set; }

        [Required(ErrorMessage = "Email không được để trống")]
        [EmailAddress(ErrorMessage = "Địa chỉ email không đúng định dạng")]
        [StringLength(100)]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Số điện thoại không được để trống")]
        [RegularExpression(@"^0\d{9}$", ErrorMessage = "Số điện thoại phải gồm 10 chữ số và bắt đầu bằng số 0")]
        [StringLength(15)]
        [Display(Name = "Số điện thoại")]
        public string PhoneNumber { get; set; }

        [StringLength(200)]
        [Display(Name = "Địa chỉ")]
        public string Address { get; set; }

        [StringLength(255)]
        [Display(Name = "Ảnh đại diện")]
        public string AvatarUrl { get; set; }

        [StringLength(50)]
        [Display(Name = "Trình độ đầu vào")]
        public string EntryLevel { get; set; }

        [Display(Name = "Tài khoản liên kết")]
        public int? UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; }

        public virtual ICollection<Enrollment> Enrollments { get; set; }
    }
}
