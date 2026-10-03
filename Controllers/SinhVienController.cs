using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Lesson6.Data;
using Lesson6.Models;

namespace Lesson6.Controllers;

public class SinhVienController : Controller
{
    private const int PageSize = 5;
    private readonly AppDbContext _db;

    public SinhVienController(AppDbContext db) => _db = db;

    private async Task LoadKhoasAsync(int? selected = null)
    {
        var khoas = await _db.Khoas.OrderBy(k => k.TenKhoa).ToListAsync();
        ViewBag.Khoas = new SelectList(khoas, "Id", "TenKhoa", selected);
    }

    // READ: danh sách + tìm kiếm + lọc theo khoa + phân trang
    public async Task<IActionResult> Index(string? search, int? khoaId, int page = 1)
    {
        var query = _db.SinhViens.Include(s => s.Khoa).AsQueryable();

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
        var sv = await _db.SinhViens.Include(s => s.Khoa).FirstOrDefaultAsync(s => s.Id == id);
        return sv == null ? NotFound() : View(sv);
    }

    // CREATE
    public async Task<IActionResult> Create()
    {
        await LoadKhoasAsync();
        return View(new SinhVien());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SinhVien sv)
    {
        if (await _db.SinhViens.AnyAsync(s => s.MaSV == sv.MaSV))
            ModelState.AddModelError(nameof(sv.MaSV), "Mã sinh viên đã tồn tại");

        if (ModelState.IsValid)
        {
            _db.Add(sv);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Thêm sinh viên thành công";
            return RedirectToAction(nameof(Index));
        }
        await LoadKhoasAsync(sv.KhoaId);
        return View(sv);
    }

    // UPDATE
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();
        var sv = await _db.SinhViens.FindAsync(id);
        if (sv == null) return NotFound();
        await LoadKhoasAsync(sv.KhoaId);
        return View(sv);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, SinhVien sv)
    {
        if (id != sv.Id) return NotFound();

        if (await _db.SinhViens.AnyAsync(s => s.MaSV == sv.MaSV && s.Id != sv.Id))
            ModelState.AddModelError(nameof(sv.MaSV), "Mã sinh viên đã tồn tại");

        if (ModelState.IsValid)
        {
            try
            {
                _db.Update(sv);
                await _db.SaveChangesAsync();
                TempData["Success"] = "Cập nhật sinh viên thành công";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _db.SinhViens.AnyAsync(s => s.Id == id)) return NotFound();
                throw;
            }
        }
        await LoadKhoasAsync(sv.KhoaId);
        return View(sv);
    }

    // DELETE
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();
        var sv = await _db.SinhViens.Include(s => s.Khoa).FirstOrDefaultAsync(s => s.Id == id);
        return sv == null ? NotFound() : View(sv);
    }

    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var sv = await _db.SinhViens.FindAsync(id);
        if (sv != null)
        {
            _db.SinhViens.Remove(sv);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Đã xóa sinh viên";
        }
        return RedirectToAction(nameof(Index));
    }
}

