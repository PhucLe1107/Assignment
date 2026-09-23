using System.ComponentModel.DataAnnotations;

namespace Assignment.Models
{
    public class UserCreateViewModel
    {
        [Required(ErrorMessage = "Tên đăng nhập không được để trống")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Tên đăng nhập từ 3 đến 50 ký tự")]
        [Display(Name = "Tên đăng nhập")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mật khẩu không được để trống")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu tối thiểu 6 ký tự")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu")]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Xác nhận mật khẩu")]
        [Compare("Password", ErrorMessage = "Mật khẩu xác nhận không trùng khớp.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn vai trò")]
        [Display(Name = "Vai trò hệ thống")]
        public int RoleId { get; set; }

        [Display(Name = "Học viên liên kết")]
        public int? StudentId { get; set; }

        [Display(Name = "Kích hoạt tài khoản ngay")]
        public bool IsActive { get; set; } = true;
    }

    public class UserEditViewModel
    {
        public int UserId { get; set; }

        [Display(Name = "Tên đăng nhập")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn vai trò")]
        [Display(Name = "Vai trò hệ thống")]
        public int RoleId { get; set; }

        [Display(Name = "Học viên liên kết")]
        public int? StudentId { get; set; }

        public string? LinkedStudentName { get; set; }
        public string? LinkedStudentCode { get; set; }

        [Display(Name = "Trạng thái hoạt động")]
        public bool IsActive { get; set; }

        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu mới tối thiểu 6 ký tự")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu mới (Bỏ trống nếu không muốn đổi)")]
        public string? NewPassword { get; set; }
    }
}