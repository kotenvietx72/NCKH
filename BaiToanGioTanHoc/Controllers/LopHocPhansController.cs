using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using BaiToanGioTanHoc.Models;

namespace BaiToanGioTanHoc.Controllers
{
    [Authorize]
    public class LopHocPhansController : Controller
    {
        private Entities2 db = new Entities2();

        // GET: LopHocPhans

        public ActionResult Index(string searchMaLHP)
        {
            var lopHocPhans = db.LopHocPhans.Include(l => l.GiangVien).Include(l => l.LopDanhNghia).Include(l => l.MonHoc);

            if (!string.IsNullOrEmpty(searchMaLHP))
            {
                lopHocPhans = lopHocPhans.Where(l => l.MaLHP.Contains(searchMaLHP));
            }

            return View(lopHocPhans.ToList());
        }

        // GET: LopHocPhans/Details/5

        public ActionResult Details(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            LopHocPhan lopHocPhan = db.LopHocPhans.Find(id);
            if (lopHocPhan == null)
            {
                return HttpNotFound();
            }
            return View(lopHocPhan);
        }

        // GET: LopHocPhans/Create
        [Authorize(Roles = "Administrator")]
        public ActionResult Create()
        {
            ViewBag.MaGiangVien = new SelectList(db.GiangViens, "MaGiangVien", "HoTenGiangVien");
            ViewBag.MaLopDN = new SelectList(db.LopDanhNghias, "MaLopDN", "TenLopDN");
            ViewBag.MaMH = new SelectList(db.MonHocs, "MaMH", "TenMH");
            return View();
        }

        // POST: LopHocPhans/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator")]
        public ActionResult Create([Bind(Include = "MaLHP,MaMH,MaLopDN,SiSo,MaGiangVien")] LopHocPhan lopHocPhan)
        {
            if (ModelState.IsValid)
            {
                db.LopHocPhans.Add(lopHocPhan);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.MaGiangVien = new SelectList(db.GiangViens, "MaGiangVien", "HoTenGiangVien", lopHocPhan.MaGiangVien);
            ViewBag.MaLopDN = new SelectList(db.LopDanhNghias, "MaLopDN", "TenLopDN", lopHocPhan.MaLopDN);
            ViewBag.MaMH = new SelectList(db.MonHocs, "MaMH", "TenMH", lopHocPhan.MaMH);
            return View(lopHocPhan);
        }

        // GET: LopHocPhans/Edit/5
        [Authorize(Roles = "Administrator")]
        public ActionResult Edit(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            LopHocPhan lopHocPhan = db.LopHocPhans.Find(id);
            if (lopHocPhan == null)
            {
                return HttpNotFound();
            }
            ViewBag.MaGiangVien = new SelectList(db.GiangViens, "MaGiangVien", "HoTenGiangVien", lopHocPhan.MaGiangVien);
            ViewBag.MaLopDN = new SelectList(db.LopDanhNghias, "MaLopDN", "TenLopDN", lopHocPhan.MaLopDN);
            ViewBag.MaMH = new SelectList(db.MonHocs, "MaMH", "TenMH", lopHocPhan.MaMH);
            return View(lopHocPhan);
        }

        // POST: LopHocPhans/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator")]
        public ActionResult Edit([Bind(Include = "MaLHP,MaMH,MaLopDN,SiSo,MaGiangVien")] LopHocPhan lopHocPhan)
        {
            if (ModelState.IsValid)
            {
                db.Entry(lopHocPhan).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.MaGiangVien = new SelectList(db.GiangViens, "MaGiangVien", "HoTenGiangVien", lopHocPhan.MaGiangVien);
            ViewBag.MaLopDN = new SelectList(db.LopDanhNghias, "MaLopDN", "TenLopDN", lopHocPhan.MaLopDN);
            ViewBag.MaMH = new SelectList(db.MonHocs, "MaMH", "TenMH", lopHocPhan.MaMH);
            return View(lopHocPhan);
        }

        // GET: LopHocPhans/Delete/5
        [Authorize(Roles = "Administrator")]
        public ActionResult Delete(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            LopHocPhan lopHocPhan = db.LopHocPhans.Find(id);
            if (lopHocPhan == null)
            {
                return HttpNotFound();
            }
            return View(lopHocPhan);
        }

        // POST: LopHocPhans/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator")]
        public ActionResult DeleteConfirmed(string id)
        {
            LopHocPhan lopHocPhan = db.LopHocPhans.Find(id);
            db.LopHocPhans.Remove(lopHocPhan);
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
