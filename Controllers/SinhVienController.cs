using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Lesson6.Data;
using Lesson6.Models;

namespace Lesson6.Controllers;

public class SinhVienController : Controller
{
    private const int PageSize = 5;

    // Quy định upload ảnh
    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png" };
    private static readonly string[] AllowedContentTypes = { "image/jpeg", "image/png" };
    private const long MaxFileSize = 5 * 1024 * 1024; // 5 MB / ảnh
    private const string UploadFolder = "uploads/sinhvien";

    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;

    public SinhVienController(AppDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    private async Task LoadKhoasAsync(int? selected = null)
    {
        var khoas = await _db.Khoas.OrderBy(k => k.TenKhoa).ToListAsync();
        ViewBag.Khoas = new SelectList(khoas, "Id", "TenKhoa", selected);
    }

    // ===== Xử lý upload ảnh =====

    // Kiểm tra từng file: đuôi file, content-type, dung lượng và nội dung thật (jpg/png)
    private static async Task<List<string>> ValidateImagesAsync(IEnumerable<IFormFile>? files)
    {
        var errors = new List<string>();
        foreach (var file in files ?? Enumerable.Empty<IFormFile>())
        {
            if (file.Length == 0) continue;

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(ext) || !AllowedContentTypes.Contains(file.ContentType.ToLowerInvariant()))
            {
                errors.Add($"File \"{file.FileName}\" không hợp lệ. Chỉ cho phép ảnh .jpg hoặc .png");
                continue;
            }
            if (file.Length > MaxFileSize)
            {
                errors.Add($"File \"{file.FileName}\" vượt quá 5 MB");
                continue;
            }
            if (!await HasImageSignatureAsync(file))
                errors.Add($"File \"{file.FileName}\" không phải ảnh jpg/png thật");
        }
        return errors;
    }

    // Đọc vài byte đầu file để chắc chắn đúng là JPEG/PNG (tránh đổi đuôi .exe thành .jpg)
    private static async Task<bool> HasImageSignatureAsync(IFormFile file)
    {
        var header = new byte[8];
        await using var stream = file.OpenReadStream();
        var read = await stream.ReadAsync(header.AsMemory(0, header.Length));
        if (read < 4) return false;

        bool isJpeg = header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF;
        bool isPng = header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47;
        return isJpeg || isPng;
    }

    // Lưu file vào wwwroot/uploads/sinhvien với tên ngẫu nhiên, trả về tên file đã lưu
    private async Task<List<string>> SaveImagesAsync(IEnumerable<IFormFile>? files)
    {
        var saved = new List<string>();
        var folder = Path.Combine(_env.WebRootPath, UploadFolder);
        Directory.CreateDirectory(folder);

        foreach (var file in files ?? Enumerable.Empty<IFormFile>())
        {
            if (file.Length == 0) continue;
            var fileName = Guid.NewGuid().ToString("N") + Path.GetExtension(file.FileName).ToLowerInvariant();
            await using var fs = new FileStream(Path.Combine(folder, fileName), FileMode.Create);
            await file.CopyToAsync(fs);
            saved.Add(fileName);
        }
        return saved;
    }

    // Tách link ảnh (mỗi dòng một link), kiểm tra phải là http/https hợp lệ
    private static List<string> ParseLinks(string? text, List<string> errors)
    {
        var links = new List<string>();
        foreach (var line in (text ?? "").Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
        {
            var link = line.Trim();
            if (link.Length == 0) continue;

            if (link.Length > 500)
                errors.Add($"Link quá dài (tối đa 500 ký tự): {link[..40]}...");
            else if (!Uri.TryCreate(link, UriKind.Absolute, out var uri) ||
                     (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
                errors.Add($"Link không hợp lệ (phải bắt đầu bằng http:// hoặc https://): {link}");
            else
                links.Add(link);
        }
        return links;
    }

    private void DeleteImageFiles(IEnumerable<string> fileNames)
    {
        foreach (var name in fileNames)
        {
            if (SinhVienAnh.IsLink(name)) continue; // ảnh dạng link thì không có file để xóa
            var path = Path.Combine(_env.WebRootPath, UploadFolder, Path.GetFileName(name));
            if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
        }
    }

    // READ: danh sách + tìm kiếm + lọc theo khoa + phân trang
    public async Task<IActionResult> Index(string? search, int? khoaId, int page = 1)
    {
        var query = _db.SinhViens.Include(s => s.Khoa).Include(s => s.Anhs).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();
            query = query.Where(s => s.HoTen.Contains(search) || s.MaSV.Contains(search));
        }
        if (khoaId.HasValue)
            query = query.Where(s => s.KhoaId == khoaId.Value);

        var total = await query.CountAsync();
        var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)PageSize));
        page = Math.Clamp(page, 1, totalPages);

        var list = await query.OrderBy(s => s.MaSV)
                              .Skip((page - 1) * PageSize)
                              .Take(PageSize)
                              .ToListAsync();

        await LoadKhoasAsync(khoaId);
        ViewBag.Search = search;
        ViewBag.KhoaId = khoaId;
        ViewBag.Page = page;
        ViewBag.TotalPages = totalPages;
        ViewBag.Total = total;
        return View(list);
    }

    // READ: chi tiết
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();
        var sv = await _db.SinhViens.Include(s => s.Khoa).Include(s => s.Anhs)
                          .FirstOrDefaultAsync(s => s.Id == id);
        return sv == null ? NotFound() : View(sv);
    }

    // CREATE
    public async Task<IActionResult> Create()
    {
        await LoadKhoasAsync();
        return View(new SinhVien());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SinhVien sv, List<IFormFile> hinhAnhs, string? linkAnhs)
    {
        if (await _db.SinhViens.AnyAsync(s => s.MaSV == sv.MaSV))
            ModelState.AddModelError(nameof(sv.MaSV), "Mã sinh viên đã tồn tại");

        foreach (var error in await ValidateImagesAsync(hinhAnhs))
            ModelState.AddModelError(nameof(hinhAnhs), error);

        var linkErrors = new List<string>();
        var links = ParseLinks(linkAnhs, linkErrors);
        foreach (var error in linkErrors)
            ModelState.AddModelError(nameof(linkAnhs), error);

        if (ModelState.IsValid)
        {
            var savedFiles = await SaveImagesAsync(hinhAnhs);
            sv.Anhs = savedFiles.Concat(links).Select(f => new SinhVienAnh { TenFile = f }).ToList();
            try
            {
                _db.Add(sv);
                await _db.SaveChangesAsync();
            }
            catch
            {
                DeleteImageFiles(savedFiles); // lỗi DB thì dọn file vừa lưu
                throw;
            }
            TempData["Success"] = "Thêm sinh viên thành công";
            return RedirectToAction(nameof(Index));
        }
        ViewBag.LinkAnhs = linkAnhs;
        await LoadKhoasAsync(sv.KhoaId);
        return View(sv);
    }

    // UPDATE
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();
        var sv = await _db.SinhViens.Include(s => s.Anhs).FirstOrDefaultAsync(s => s.Id == id);
        if (sv == null) return NotFound();
        await LoadKhoasAsync(sv.KhoaId);
        return View(sv);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, SinhVien sv, List<IFormFile> hinhAnhs, string? linkAnhs, int[]? xoaAnhIds)
    {
        if (id != sv.Id) return NotFound();

        if (await _db.SinhViens.AnyAsync(s => s.MaSV == sv.MaSV && s.Id != sv.Id))
            ModelState.AddModelError(nameof(sv.MaSV), "Mã sinh viên đã tồn tại");

        foreach (var error in await ValidateImagesAsync(hinhAnhs))
            ModelState.AddModelError(nameof(hinhAnhs), error);

        var linkErrors = new List<string>();
        var links = ParseLinks(linkAnhs, linkErrors);
        foreach (var error in linkErrors)
            ModelState.AddModelError(nameof(linkAnhs), error);

        if (ModelState.IsValid)
        {
            var savedFiles = new List<string>();
            try
            {
                _db.Update(sv);

                // Xóa các ảnh được tick chọn
                var filesToDelete = new List<string>();
                if (xoaAnhIds is { Length: > 0 })
                {
                    var anhXoa = await _db.SinhVienAnhs
                        .Where(a => a.SinhVienId == id && xoaAnhIds.Contains(a.Id))
                        .ToListAsync();
                    filesToDelete = anhXoa.Select(a => a.TenFile).ToList();
                    _db.SinhVienAnhs.RemoveRange(anhXoa);
                }

                // Thêm ảnh mới
                savedFiles = await SaveImagesAsync(hinhAnhs);
                foreach (var f in savedFiles.Concat(links))
                    _db.SinhVienAnhs.Add(new SinhVienAnh { SinhVienId = id, TenFile = f });

                await _db.SaveChangesAsync();
                DeleteImageFiles(filesToDelete); // chỉ xóa file thật sau khi DB đã lưu thành công

                TempData["Success"] = "Cập nhật sinh viên thành công";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                DeleteImageFiles(savedFiles);
                if (!await _db.SinhViens.AnyAsync(s => s.Id == id)) return NotFound();
                throw;
            }
            catch
            {
                DeleteImageFiles(savedFiles);
                throw;
            }
        }

        // Form lỗi: nạp lại ảnh hiện có để hiển thị
        sv.Anhs = await _db.SinhVienAnhs.AsNoTracking().Where(a => a.SinhVienId == id).ToListAsync();
        ViewBag.LinkAnhs = linkAnhs;
        await LoadKhoasAsync(sv.KhoaId);
        return View(sv);
    }

    // DELETE
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();
        var sv = await _db.SinhViens.Include(s => s.Khoa).Include(s => s.Anhs)
                          .FirstOrDefaultAsync(s => s.Id == id);
        return sv == null ? NotFound() : View(sv);
    }

    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var sv = await _db.SinhViens.Include(s => s.Anhs).FirstOrDefaultAsync(s => s.Id == id);
        if (sv != null)
        {
            var files = sv.Anhs.Select(a => a.TenFile).ToList();
            _db.SinhViens.Remove(sv);
            await _db.SaveChangesAsync();
            DeleteImageFiles(files);
            TempData["Success"] = "Đã xóa sinh viên";
        }
        return RedirectToAction(nameof(Index));
    }
}