using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity.Validation;
using OfficeOpenXml;
using BaiToanGioTanHoc.Models;
using ChuongTrinhChinh;
using System.Data;
using System.Data.Entity;
using System.Diagnostics;
using System.Threading.Tasks;

namespace BaiToanGioTanHoc.Controllers
{
    public class ExcelController : Controller
    {
        // GET: Excel
        private readonly Entities2 _context;

        public ExcelController()
        {
            _context = new Entities2();
        }
        public ActionResult UploadExcel()
        {
            return View();
        }

        [HttpPost]
        public ActionResult UploadExcel(HttpPostedFileBase file)
        {
            if (file == null || file.ContentLength == 0)
            {
                return Json(new { success = false, message = "Vui lòng chọn một file Excel." });
            }

            try
            {
                using (var transaction = _context.Database.BeginTransaction()) // Dùng transaction để đảm bảo tính toàn vẹn dữ liệu
                {
                    try
                    {
                        // Bước 1: Xóa toàn bộ dữ liệu hiện có
                        _context.LichHocs.RemoveRange(_context.LichHocs);
                        _context.LopHocPhans.RemoveRange(_context.LopHocPhans);
                        _context.GiangViens.RemoveRange(_context.GiangViens);
                        _context.MonHocs.RemoveRange(_context.MonHocs);
                        _context.LopDanhNghias.RemoveRange(_context.LopDanhNghias);
                        _context.PhongHocs.RemoveRange(_context.PhongHocs);
                        _context.SaveChanges(); // Lưu lại để tránh lỗi khóa ngoại

                        using (var package = new ExcelPackage(file.InputStream))
                        {
                            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                            foreach (var worksheet in package.Workbook.Worksheets)
                            {
                                if (worksheet.Dimension == null) continue;

                                int rowCount = worksheet.Dimension.Rows;
                                for (int row = 2; row <= rowCount; row++)
                                {
                                    if (worksheet.Cells[row, 2].Text.Trim() == "") continue;

                                    var maLHP = worksheet.Cells[row, 2]?.Text?.Trim() ?? "";
                                    var tenMonHoc = worksheet.Cells[row, 3]?.Text?.Trim() ?? "";
                                    var lopHoc = worksheet.Cells[row, 4]?.Text?.Trim() ?? "";
                                    var tietHoc = worksheet.Cells[row, 6]?.Text?.Trim() ?? "";
                                    var ngayHoc = worksheet.Cells[row, 7]?.Text?.Trim() ?? "";
                                    var siSo = worksheet.Cells[row, 9]?.Text?.Trim() ?? "";
                                    var phongHoc = worksheet.Cells[row, 10]?.Text?.Trim() ?? "";
                                    var dayNha = worksheet.Cells[row, 12]?.Text?.Trim() ?? "";
                                    var giangVien = worksheet.Cells[row, 13]?.Text?.Trim() ?? "";

                                    if (string.IsNullOrEmpty(maLHP) || string.IsNullOrEmpty(tenMonHoc))
                                    {
                                        continue;
                                    }

                                    var tietValues = tietHoc.Split('→');
                                    int tietBatDau = int.Parse(tietValues[0].Trim());
                                    int tietKetThuc = int.Parse(tietValues[1].Trim());
                                    // Thêm môn học
                                    var monHoc = _context.MonHocs.FirstOrDefault(m => m.TenMH == tenMonHoc);
                                    if (monHoc == null)
                                    {
                                        monHoc = new MonHoc { TenMH = tenMonHoc };
                                        _context.MonHocs.Add(monHoc);
                                        _context.SaveChanges();
                                    }

                                    // Thêm giảng viên
                                    var giangVienEntity = _context.GiangViens.FirstOrDefault(g => g.HoTenGiangVien == giangVien);
                                    if (giangVienEntity == null)
                                    {
                                        giangVienEntity = new GiangVien { HoTenGiangVien = giangVien };
                                        _context.GiangViens.Add(giangVienEntity);
                                        _context.SaveChanges();
                                    }

                                    // Thêm phòng học
                                    var phongHocEntity = _context.PhongHocs.FirstOrDefault(p => p.TenPhong == phongHoc);
                                    if (phongHocEntity == null)
                                    {
                                        phongHocEntity = new PhongHoc { TenPhong = phongHoc, DayNha = dayNha };
                                        _context.PhongHocs.Add(phongHocEntity);
                                        _context.SaveChanges();
                                    }

                                    // Thêm lớp danh nghĩa
                                    var lopDanhNghia = _context.LopDanhNghias.FirstOrDefault(h => h.TenLopDN == lopHoc);
                                    if (lopDanhNghia == null)
                                    {
                                        lopDanhNghia = new LopDanhNghia { TenLopDN = lopHoc };
                                        _context.LopDanhNghias.Add(lopDanhNghia);
                                        _context.SaveChanges();
                                    }

                                    // Kiểm tra xem MaLHP đã tồn tại chưa
                                    var existingLopHocPhan = _context.LopHocPhans.FirstOrDefault(l => l.MaLHP == maLHP);
                                    LopHocPhan lopHocPhanEntity;

                                    if (existingLopHocPhan == null)
                                    {
                                        // Nếu chưa tồn tại, thêm mới
                                        var lopHocPhan = new LopHocPhan
                                        {
                                            MaLHP = maLHP,
                                            MaLopDN = lopDanhNghia.MaLopDN,
                                            SiSo = int.Parse(siSo),
                                            MaGiangVien = giangVienEntity.MaGiangVien,
                                            MaMH = monHoc.MaMH
                                        };
                                        _context.LopHocPhans.Add(lopHocPhan);
                                        _context.SaveChanges();

                                        lopHocPhanEntity = lopHocPhan; // Gán biến để sử dụng sau
                                    }
                                    else
                                    {
                                        // Nếu đã tồn tại, có thể cập nhật thông tin (nếu cần)
                                        existingLopHocPhan.SiSo = int.Parse(siSo);
                                        existingLopHocPhan.MaGiangVien = giangVienEntity.MaGiangVien;
                                        _context.SaveChanges();

                                        lopHocPhanEntity = existingLopHocPhan; // Gán biến để sử dụng sau
                                    }
                                    // Sau khi có chắc chắn MaLHP, tạo lịch học
                                    var lichHoc = new LichHoc
                                    {
                                        MaLHP = lopHocPhanEntity.MaLHP, // Sử dụng biến đã xác định
                                        MaPhong = phongHocEntity.MaPhong,
                                        NgayHoc = DateTime.Parse(ngayHoc),
                                        TietBatDau = tietBatDau,
                                        TietKetThuc = tietKetThuc
                                    };
                                    _context.LichHocs.Add(lichHoc);
                                    _context.SaveChanges();

                                }
                                _context.SaveChanges();
                            }
                        }

                        transaction.Commit();

                        var ListNgayHoc = _context.LichHocs.Select(lh => lh.NgayHoc).Distinct().OrderBy(ngay => ngay).ToList();
                        Task.Run(() => XuLiLichDaLuong(ListNgayHoc));

                        return Json(new { success = true, message = "Dữ liệu từ Excel đã được cập nhật thành công." });
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        return Json(new
                        {
                            success = false,
                            message = "Lỗi khi nhập dữ liệu",
                            details = ex.ToString()
                        });
                    }

                }
            }
            catch (DbEntityValidationException ex)
            {
                var errors = ex.EntityValidationErrors
                    .SelectMany(e => e.ValidationErrors)
                    .Select(e => $"{e.PropertyName}: {e.ErrorMessage}")
                    .ToList();

                return Json(new { success = false, message = "Validation Error", details = errors });
            }
        }
        private async Task GenerateDismissalTime(DateTime? searchDate, int TietKetThuc)
        {
            await Task.Run(() =>
            {
                using (var context = new Entities2()) // Tạo một instance mới
                {
                    var a = context.LichHocs.Include(l => l.LopHocPhan).Include(l => l.PhongHoc).Where(l => l.NgayHoc == searchDate && l.TietKetThuc == TietKetThuc);

                    List<LichHoc> ClassRooms = a.ToList();

                    foreach (var classroom in ClassRooms)
                        classroom.InjectDbContext(context);

                    List<BatchScheduler> bestBatches = new List<BatchScheduler>();
                    XuLiDuLieu xuLiDuLieu = new XuLiDuLieu(context);

                    xuLiDuLieu.TimCacDotToiUu(bestBatches, ClassRooms);
                }
            });
        }

        private async Task RunGenerateDismissalTimeAsync(DateTime searchDate)
        {
            var task1 = GenerateDismissalTime(searchDate, 6);  // Tiết 6
            var task2 = GenerateDismissalTime(searchDate, 12); // Tiết 12

            await Task.WhenAll(task1, task2); // Chờ cả 2 task hoàn thành
        }

        private async void XuLiLichDaLuong(List<DateTime> ListNgayHoc)
        {
            var tasks = new List<Task>();

            foreach (var lh in ListNgayHoc)
            {
                tasks.Add(RunGenerateDismissalTimeAsync(lh)); // Gọi task xử lý mỗi ngày học
            }

            await Task.WhenAll(tasks); // Chờ tất cả các task hoàn thành
        }
    }
}