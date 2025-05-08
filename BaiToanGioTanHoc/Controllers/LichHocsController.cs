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
using ClosedXML.Excel;

namespace BaiToanGioTanHoc.Controllers
{
    [Authorize]
    public class LichHocsController : Controller
    {
        private Entities2 db = new Entities2();


        // GET: LichHocs
        public ActionResult Index(DateTime? searchDate, string sortOrder, string sortBySession)
        {
            var lichHocs = db.LichHocs
                .Include(l => l.LopHocPhan)
                .Include(l => l.PhongHoc)
                .Where(l => l.TietKetThuc == 6 || l.TietKetThuc == 12); 

            if (searchDate.HasValue)
            {
                lichHocs = lichHocs.Where(l => l.NgayHoc == searchDate.Value);
            }

            // Xếp theo buổi
            if (sortBySession == "morning")
            {
                lichHocs = lichHocs.Where(l => l.TietKetThuc == 6);
            }
            else if (sortBySession == "afternoon")
            {
                lichHocs = lichHocs.Where(l => l.TietKetThuc == 12);
            }

            switch (sortOrder)
            {
                case "desc":
                    lichHocs = lichHocs.OrderByDescending(l => l.GioTanHoc);
                    break;
                default:
                    lichHocs = lichHocs.OrderBy(l => l.GioTanHoc);
                    break;
            }

            return View(lichHocs.ToList());
        }
        public ActionResult ExportToExcel(DateTime? searchDate, string sortOrder, string sortBySession)
        {
            var lichHocs = db.LichHocs.Include(l => l.LopHocPhan).Include(l => l.PhongHoc);

            if (searchDate.HasValue)
            {
                lichHocs = lichHocs.Where(l => l.NgayHoc == searchDate.Value);
            }

            if (sortBySession == "morning")
            {
                lichHocs = lichHocs.Where(l => l.TietBatDau >= 1 && l.TietBatDau <= 3); // Buổi sáng
            }
            else if (sortBySession == "afternoon")
            {
                lichHocs = lichHocs.Where(l => l.TietBatDau >= 4 && l.TietBatDau <= 6); // Buổi chiều
            }
            else if (sortBySession == "evening")
            {
                lichHocs = lichHocs.Where(l => l.TietBatDau >= 7); // Buổi tối
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

                worksheet.Cell(1, 1).Value = "Mã Lịch Học";
                worksheet.Cell(1, 2).Value = "Ngày Học";
                worksheet.Cell(1, 3).Value = "Tiết Bắt Đầu";
                worksheet.Cell(1, 4).Value = "Tiết Kết Thúc";
                worksheet.Cell(1, 5).Value = "Giờ Tan Học";
                worksheet.Cell(1, 6).Value = "Mã Lớp Học Phần";
                worksheet.Cell(1, 7).Value = "Tên Phòng";

                int row = 2;
                foreach (var item in data)
                {
                    worksheet.Cell(row, 1).Value = item.MaLichHoc;
                    worksheet.Cell(row, 2).Value = item.NgayHoc.ToString("dd/MM/yyyy");
                    worksheet.Cell(row, 3).Value = item.TietBatDau;
                    worksheet.Cell(row, 4).Value = item.TietKetThuc;
                    worksheet.Cell(row, 5).Value = item.GioTanHoc.HasValue
                    ? $"{(int)item.GioTanHoc.Value.TotalHours:D2}:{item.GioTanHoc.Value.Minutes:D2}"
                    : "";
                    worksheet.Cell(row, 6).Value = item.LopHocPhan.MaLHP;
                    worksheet.Cell(row, 7).Value = item.PhongHoc.TenPhong;
                    row++;
                }

                worksheet.Columns().AdjustToContents();

                using (var memoryStream = new MemoryStream())
                {
                    workbook.SaveAs(memoryStream);
                    memoryStream.Position = 0;
                    return File(memoryStream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "LichHoc.xlsx");
                }
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
