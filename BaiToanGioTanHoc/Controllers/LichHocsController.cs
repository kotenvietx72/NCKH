using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using BaiToanGioTanHoc.Models;
using ChuongTrinhChinh;
using ClosedXML.Excel;
using DocumentFormat.OpenXml.Office2010.ExcelAc;
using PagedList;

namespace BaiToanGioTanHoc.Controllers
{
    [Authorize]
    public class LichHocsController : Controller
    {
        private Entities2 db = new Entities2();


        // GET: LichHocs
        public ActionResult Index(DateTime? searchDate, string sortOrder, string sortBySession, int page = 1)
        {
            var list = db.LichHocs
                .Include(l => l.LopHocPhan)
                .Include(l => l.PhongHoc)
                .Where(l => l.TietKetThuc == 6 || l.TietKetThuc == 12).ToList(); 

            foreach(var classroom in list)
            {
                XuLiDuLieu a = new XuLiDuLieu(db);
                a.TinhTGXuongCSV(classroom);
                a.TGQuaCSV(classroom);
            } 
            

            if (searchDate.HasValue)
            {
                list = list.Where(l => l.NgayHoc == searchDate.Value).ToList();
            }

            // Xếp theo buổi
            if (sortBySession == "morning")
            {
                list = list.Where(l => l.TietKetThuc == 6).ToList(); // Buổi sáng
            }
            else if (sortBySession == "afternoon")
            {
                list = list.Where(l => l.TietKetThuc == 12).ToList(); // Buổi chiều
            }
            switch (sortOrder)
            {
                case "desc":
                    list = list.OrderByDescending(l => l.GioTanHoc).ThenByDescending(l => l.ThoiGianXuongToiCong).ToList(); 
                    break;
                default:
                    list = list.OrderBy(l => l.GioTanHoc).ThenBy(l => l.ThoiGianXuongToiCong).ToList();
                    break;
            }

            int pageSize = 25;
            int pageNumber = page;

            return View(list.ToPagedList(pageNumber, pageSize));
        }
        public ActionResult ExportToExcel(DateTime? searchDate, string sortOrder, string sortBySession)
        {
            var lichHocs = db.LichHocs.Include(l => l.LopHocPhan).Include(l => l.PhongHoc);

            if (searchDate.HasValue)
            {
                lichHocs = lichHocs.Where(l => l.NgayHoc == searchDate.Value);
            }

            // Xếp theo buổi
            if (sortBySession == "morning")
            {
                lichHocs = lichHocs.Where(l => l.TietKetThuc <= 6); // Buổi sáng
            }
            else if (sortBySession == "afternoon")
            {
                lichHocs = lichHocs.Where(l => l.TietKetThuc <= 12); // Buổi chiều
            }

            // Sắp xếp theo giờ kết thúc
            switch (sortOrder)
            {
                case "desc":
                    lichHocs = lichHocs.OrderByDescending(l => l.GioTanHoc);
                    break;
                default:
                    lichHocs = lichHocs.OrderBy(l => l.GioTanHoc);
                    break;
            }

            var data = lichHocs.ToList();

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("LichHoc");

                worksheet.Cell(1, 1).Value = "Mã Lớp Học Phần";
                worksheet.Cell(1, 2).Value = "Tên Môn Học";
                worksheet.Cell(1, 3).Value = "Tên Lớp Danh Nghĩa";
                worksheet.Cell(1, 4).Value = "Sĩ Số";
                worksheet.Cell(1, 5).Value = "Ngày Học";
                worksheet.Cell(1, 6).Value = "Tiết Bắt Đầu";
                worksheet.Cell(1, 7).Value = "Tiết Kết Thúc";
                worksheet.Cell(1, 8).Value = "Giờ Tan Học";
                worksheet.Cell(1, 9).Value = "Tên Phòng";
                worksheet.Cell(1, 10).Value = "Tên Giảng Viên";

                int row = 2;
                foreach (var item in data)
                {
                    worksheet.Cell(row, 1).Value = item.MaLHP;
                    worksheet.Cell(row, 2).Value = item.LopHocPhan.MonHoc.TenMH;
                    worksheet.Cell(row, 3).Value = item.LopHocPhan.LopDanhNghia.TenLopDN;
                    worksheet.Cell(row, 4).Value = item.LopHocPhan.SiSo;
                    worksheet.Cell(row, 5).Value = item.NgayHoc.ToString("dd/MM/yyyy");
                    worksheet.Cell(row, 6).Value = item.TietBatDau;
                    worksheet.Cell(row, 7).Value = item.TietKetThuc;
                    worksheet.Cell(row, 8).Value = item.GioTanHoc.HasValue
                    ? $"{(int)item.GioTanHoc.Value.TotalHours:D2}:{item.GioTanHoc.Value.Minutes:D2}"
                    : "";
                    worksheet.Cell(row, 9).Value = item.PhongHoc.TenPhong;
                    worksheet.Cell(row, 10).Value = item.LopHocPhan.GiangVien.HoTenGiangVien;
                    row++;
                }

                worksheet.Columns().AdjustToContents();

                var memoryStream = new MemoryStream();
                workbook.SaveAs(memoryStream);
                memoryStream.Position = 0;

                return File(memoryStream,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    "LichHoc.xlsx");
            }
        
        }

        // GET: LichHocs/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            LichHoc lichHoc = db.LichHocs.Find(id);
            if (lichHoc == null)
            {
                return HttpNotFound();
            }
            return View(lichHoc);
        }

        // GET: LichHocs/Create
        [Authorize(Roles = "Administrator")]
        public ActionResult Create()
        {
            ViewBag.MaLHP = new SelectList(db.LopHocPhans, "MaLHP", "MaLHP");
            ViewBag.MaPhong = new SelectList(db.PhongHocs, "MaPhong", "TenPhong");
            return View();
        }

        // POST: LichHocs/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator")]
        public ActionResult Create([Bind(Include = "MaLichHoc,MaLHP,MaPhong,NgayHoc,TietBatDau,TietKetThuc,GioTanHoc")] LichHoc lichHoc)
        {
            if (ModelState.IsValid)
            {
                db.LichHocs.Add(lichHoc);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.MaLHP = new SelectList(db.LopHocPhans, "MaLHP", "MaLHP", lichHoc.MaLHP);
            ViewBag.MaPhong = new SelectList(db.PhongHocs, "MaPhong", "TenPhong", lichHoc.MaPhong);
            return View(lichHoc);
        }

        // GET: LichHocs/Edit/5
        [Authorize(Roles = "Administrator")]
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            LichHoc lichHoc = db.LichHocs.Find(id);
            if (lichHoc == null)
            {
                return HttpNotFound();
            }
            ViewBag.MaLHP = new SelectList(db.LopHocPhans, "MaLHP", "MaLHP", lichHoc.MaLHP);
            ViewBag.MaPhong = new SelectList(db.PhongHocs, "MaPhong", "TenPhong", lichHoc.MaPhong);
            return View(lichHoc);
        }

        // POST: LichHocs/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator")]
        public ActionResult Edit([Bind(Include = "MaLichHoc,MaLHP,MaPhong,NgayHoc,TietBatDau,TietKetThuc,GioTanHoc")] LichHoc lichHoc)
        {
            if (ModelState.IsValid)
            {
                db.Entry(lichHoc).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.MaLHP = new SelectList(db.LopHocPhans, "MaLHP", "MaLHP", lichHoc.MaLHP);
            ViewBag.MaPhong = new SelectList(db.PhongHocs, "MaPhong", "TenPhong", lichHoc.MaPhong);
            return View(lichHoc);
        }

        // GET: LichHocs/Delete/5
        [Authorize(Roles = "Administrator")]
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            LichHoc lichHoc = db.LichHocs.Find(id);
            if (lichHoc == null)
            {
                return HttpNotFound();
            }
            return View(lichHoc);
        }

        // POST: LichHocs/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator")]
        public ActionResult DeleteConfirmed(int id)
        {
            LichHoc lichHoc = db.LichHocs.Find(id);
            db.LichHocs.Remove(lichHoc);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
