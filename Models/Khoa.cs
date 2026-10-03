using System.ComponentModel.DataAnnotations;

namespace Lesson6.Models;

public class Khoa
{
    public int Id { get; set; }

    [Required, StringLength(100)]
    [Display(Name = "Tên khoa")]
    public string TenKhoa { get; set; } = string.Empty;

    public ICollection<SinhVien> SinhViens { get; set; } = new List<SinhVien>();
}
