using System.ComponentModel.DataAnnotations;

namespace Lesson6.Models;

public class SinhVien
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập mã sinh viên")]
    [StringLength(20, ErrorMessage = "Mã sinh viên tối đa 20 ký tự")]
    [Display(Name = "Mã SV")]
    public string MaSV { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập họ tên")]
    [StringLength(100, ErrorMessage = "Họ tên tối đa 100 ký tự")]
    [Display(Name = "Họ và tên")]
    public string HoTen { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng chọn ngày sinh")]
    [DataType(DataType.Date)]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
    [Display(Name = "Ngày sinh")]
    public DateTime NgaySinh { get; set; } = new DateTime(2004, 1, 1);

    [Display(Name = "Giới tính")]
    public string GioiTinh { get; set; } = "Nam";

    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    [StringLength(100)]
    [Display(Name = "Email")]
    public string? Email { get; set; }

    [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
    [StringLength(15)]
    [Display(Name = "Số điện thoại")]
    public string? SoDienThoai { get; set; }

    [StringLength(200)]
    [Display(Name = "Địa chỉ")]
    public string? DiaChi { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn khoa")]
    [Display(Name = "Khoa")]
    public int KhoaId { get; set; }

    public Khoa? Khoa { get; set; }
}
