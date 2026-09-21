using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Assignment.Models.Entities
{
    public class Role
    {
        [Key]
        public int RoleId { get; set; }

        [Required(ErrorMessage = "Tên vai trò không được để trống")]
        [StringLength(50, ErrorMessage = "Tên vai trò không quá 50 ký tự")]
        [Display(Name = "Tên vai trò")]
        public string RoleName { get; set; }

        public virtual ICollection<User> Users { get; set; }
    }
}
