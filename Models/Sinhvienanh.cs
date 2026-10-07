using System.ComponentModel.DataAnnotations;

namespace Lesson6.Models;

public class SinhVienAnh
{
    public int Id { get; set; }

    public int SinhVienId { get; set; }

    // Hoặc là tên file đã upload (nằm trong wwwroot/uploads/sinhvien),
    // hoặc là link ảnh đầy đủ (http/https)
    [Required, StringLength(500)]
    public string TenFile { get; set; } = string.Empty;

    public SinhVien? SinhVien { get; set; }

    public bool LaLink => IsLink(TenFile);

    public string DuongDan => LaLink ? TenFile : $"/uploads/sinhvien/{TenFile}";

    public static bool IsLink(string value) =>
        value.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
        value.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
}