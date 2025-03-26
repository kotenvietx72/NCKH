using BaiToanGioTanHoc.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
//using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace ChuongTrinhChinh
{
    internal class XuLiDuLieu
    {
        private readonly Entities2 db;
        public int SoCong { get; private set; }
        public double ThoiGianXuLi1SV { get; private set; }

        public int GioiHanMinSinhVien { get; private set; }
        public int GioiHanMaxSinhVien { get; private set; }

        public double SegmentTime { get; private set; }
        public XuLiDuLieu(Entities2 db) 
        {
            this.db = db;
            SoCong = db.CongSoatVes.FirstOrDefault()?.SoCong ?? 0;
            ThoiGianXuLi1SV = db.CongSoatVes.FirstOrDefault()?.ThoiGianXuLi1SV ?? 0;

            // Gán giá trị trong constructor thay vì lúc khai báo
            GioiHanMinSinhVien = (int)(ThoiGianXuLi1SV / SoCong * 240);
            GioiHanMaxSinhVien = (int)(ThoiGianXuLi1SV / SoCong * 300);

            SegmentTime = 0;
        }

        /// <summary>
        /// Sắp xếp lớp tăng dần theo thời gian ra khỏi cổng + thời gian xử lí           
        /// </summary>
        /// <param name="classRooms"></param>
        // Done
        public void SapXep(List<LichHoc> classRooms)
        {
            classRooms = classRooms.OrderBy(c => c.TimeToGate() + c.ExitTime()).ToList();
        }

        /// <summary>
        /// Hàm đếm xem đợt hiện tại có bao nhiêu tòa         
        /// </summary>
        /// <param name="batchScheduler"></param>
        /// <returns></returns>
        // Done
        public int IsValidBatch(BatchScheduler batchScheduler)
        {
            int dem = 0;
            if(batchScheduler.classrooms.Any(c => c.PhongHoc.TenPhong.Contains("A8")))
                dem++;
            if(batchScheduler.classrooms.Any(c => c.PhongHoc.TenPhong.Contains("A9")))
                dem++;
            if (batchScheduler.classrooms.Any(c => c.PhongHoc.TenPhong.Contains("A10")))
                dem++;
            return dem;
        }

        /// <summary>
        /// Hàm đếm các lớp trong danh sách có bao nhiêu tòa     
        /// </summary>
        /// <param name="classRooms"></param>
        /// <returns></returns>
        // Done
        public int CountBuilding(List<LichHoc> classRooms) {
            int dem = 0;
            if (classRooms.Any(c => !c.Check && c.PhongHoc.TenPhong.Contains("A8")))
                dem++;

            if (classRooms.Any(c => !c.Check && c.PhongHoc.TenPhong.Contains("A9")))
                dem++;
            if (classRooms.Any(c => !c.Check && c.PhongHoc.TenPhong.Contains("A10")))
                dem++;
            return dem;
        }

        /// <summary>
        /// Hàm đếm số lượng sinh viên trong danh sách
        /// </summary>
        /// <param name="classRooms"></param>
        /// <returns></returns>
        public int CountStudent(List<LichHoc> classRooms)
        {
            int totalStudents = 0;
            foreach (var classRoom in classRooms)
                totalStudents += classRoom.LopHocPhan.SiSo ?? 0;
            return totalStudents;
        }

        /// <summary>
        /// Lấy danh sách các lớp đến tiết được xử lí     
        /// </summary>
        /// <param name="classRooms"></param>
        /// <returns></returns>
        // Done
        public List<LichHoc> GetClassRoomsSession(List<LichHoc> classRooms)
        {
            int SoTietConLai = 6 - (int)(SegmentTime / 300);
            return classRooms.Where(c => c.GetSessionCount() >= SoTietConLai && !c.Check).ToList();
        }

        /// <summary>
        /// Hàm tính SegmentTime bằng cách tính thời gian bắt đầu xử lí của đợt sau
        /// </summary>
        /// <param name="bestBatches"></param>
        // Done
        public void setTime1(List<BatchScheduler> bestBatches)
        {
            if (bestBatches.Count < 2)
                return;
                
            int lastIndex = bestBatches.Count - 1;
            int secondLastIndex = lastIndex - 1;

            double minTimePrev = bestBatches[secondLastIndex].classrooms.Min(c => c.TimeToGate()) ?? 0;
            double minTimeCurrent = bestBatches[lastIndex].classrooms.Min(c => c.TimeToGate()) ?? 0;
            double processingTimePrev = bestBatches[secondLastIndex].ProcessingTime() ?? 0;

            SegmentTime += processingTimePrev + minTimePrev - minTimeCurrent;
        }

        /// <summary>
        /// Hàm tính SegmentTime khi không có lớp nào đang xử lí 
        /// </summary>
        /// <param name="SelectedClasses"></param>
        // Done
        public void setTime2(List<LichHoc> SelectedClasses)
        {
            if (SegmentTime < 300 && SelectedClasses.All(c => c.Check))
            {
                SegmentTime = 300;
                return;
            }
            if (SegmentTime < 600 && SelectedClasses.All(c => c.Check))
            {
                SegmentTime = 600;
                return;
            }
            if (SegmentTime < 900 && SelectedClasses.All(c => c.Check))
            {
                SegmentTime = 900;
                return;
            }
            if (SegmentTime < 1200 && SelectedClasses.All(c => c.Check))
            {
                SegmentTime = 1200;
                return;
            }
        }

        /// <summary>
        /// Hàm tìm các đợt tối ưu trong nhánh cận
        /// </summary>
        /// <param name="bestBatches"></param>
        /// <param name="classrooms"></param>
        public void TimCacDotToiUu(List<BatchScheduler> bestBatches, List<LichHoc> classrooms) {
            List<LichHoc> SelectedClasses;                                                                              // Khởi tạo biến lưu các lớp đến giờ được xử lí
            while (classrooms.Any(c => !c.Check) && SegmentTime <= 1200)                                                // Lặp đến khi xử lí hết các lớp trong danh sách hoặc chưa đến giờ xử lí
            {                                              
                double SegmentTimeOld = SegmentTime;                                                                    // Tạo biến SegmentTimeOld trước khi setTime
                var NhomDangXet = new BatchScheduler(db);                                                                 // Tạo 1 batch lưu nhóm đang xét
                var NhomDuocChon = new BatchScheduler(db);                                                                // Tạo 1 batch lưu nhóm được chọn
                
                SelectedClasses = GetClassRoomsSession(classrooms);                                                     // Lấy danh sách các lớp đến giờ xử lí
                SapXep(SelectedClasses);

                NhanhCan(NhomDangXet, 0, NhomDuocChon);                                                                 // Gọi thuật toán nhánh cận, với nhóm mới và bắt đầu từ 0
                if (NhomDuocChon.classrooms.Count > 0)                                                                  // Nếu có đợt tối ưu, lưu lại
                {
                    bestBatches.Add(NhomDuocChon.DeepCopy());
                    foreach (var room in NhomDuocChon.classrooms)
                        classrooms.First(x => x.MaLichHoc == room.MaLichHoc).Check = true;                              // Đánh dấu các lớp đã được chọn, không xử lí các lớp này trong các đợt tiếp theo  
                    
                    setTime1(bestBatches);                                                                              // setTime cho SegmentTime
                    
                    if (bestBatches.Count > 1 && SegmentTime - bestBatches[bestBatches.Count - 2].TimeCheck > 300)      // Nếu thời gian thực hiện hai đợt cách nhau quá 5 phút, lưu biến TimeCheck của đợt gần nhất bằng SegmentTimeOld
                        bestBatches[bestBatches.Count - 1].TimeCheck = SegmentTimeOld;
                    else
                        bestBatches[bestBatches.Count - 1].TimeCheck = SegmentTime;
                    continue;                                                                                           // Tính xong TimeCheck thì bỏ qua, tiếp tục lặp lại                                                                                    // Tính xong TimeCheck thì bỏ qua, tiếp tục lặp lại
                }
                setTime2(SelectedClasses);                                                                              // Nếu không lớp đến giờ xử lí, lập tức nhảy đến khoảng thời gian tiếp theo để xử lí tiếp

            }

            if (classrooms.Any(c => !c.Check))                                                                          // Xử lí các lớp chưa được xử lí
            {
                var NhomConLai = new BatchScheduler(db);
                var roomsToCheck = classrooms.Where(c => !c.Check).ToList();

                foreach (var room in roomsToCheck)
                    NhomConLai.classrooms.Add(room);

                bestBatches.Add(NhomConLai.DeepCopy());
                bestBatches.Last().TimeCheck = 1200;
            }

            TinhThoiGianTanHoc(bestBatches, db);                                                                            // Tính thời gian tan học

            /// <summary>
            /// Sử dụng nhánh cận để tìm ra nhóm tối ưu
            /// </summary>
            /// <param name="NhomDangXet"></param>
            /// <param name="index">Vị trí lớp đang xét trong danh sách</param>
            void NhanhCan(BatchScheduler NhomDangXet, int index, BatchScheduler NhomDuocChon)
            {
                int totalStudents = CountStudent(SelectedClasses);
                int totalStudentsNhomDangXet = NhomDangXet.Count_Student();
                // Nếu nhóm đang xét có tổng số lượng sinh viên lớn hơn GioiHanMax thì dừng nhánh này
                if (totalStudentsNhomDangXet > GioiHanMaxSinhVien)
                    return;

                // Nếu nhánh hiện tại có thời gian đợi lớn hơn NhomDuocChon => cắt nhánh này luôn
                if (NhomDuocChon.classrooms.Count > 0 && NhomDangXet.WaitTime() > NhomDuocChon.WaitTime())
                    return;

                // Nếu số lượng sinh viên còn lại trong danh sách các lớp đến giờ xử lí quá ít, tiếp tục xử lí
                if (totalStudents < GioiHanMinSinhVien)
                {
                    foreach (var classRoom in SelectedClasses)
                        NhomDangXet.classrooms.Add(classRoom);
                    NhomDuocChon.classrooms = NhomDangXet.DeepCopy().classrooms;
                    return;
                }

                // Nếu nhóm đang xét hợp lệ và tốt hơn nhóm hiện tại => lưu lại nhóm tốt hơn
                if ((totalStudentsNhomDangXet >= GioiHanMinSinhVien && IsValidBatch(NhomDangXet) == 3 && CountBuilding(SelectedClasses) == 3) || (totalStudentsNhomDangXet >= GioiHanMinSinhVien && IsValidBatch(NhomDangXet) == 2 && CountBuilding(SelectedClasses) == 2) || (totalStudentsNhomDangXet >= GioiHanMinSinhVien && IsValidBatch(NhomDangXet) == 1 && CountBuilding(SelectedClasses) == 1))
                {
                    // Nếu NhomDangXet có tỉ lệ thời gian xử lí/ tổng số lớp < đợt hiện tại => Lưu NhomDangXet thay thế bestBatches
                    if (NhomDuocChon.classrooms.Count == 0 || NhomDangXet.WaitTime() / NhomDangXet.classrooms.Count < NhomDuocChon.WaitTime() / NhomDuocChon.classrooms.Count)
                        NhomDuocChon.classrooms = NhomDangXet.DeepCopy().classrooms;
                    // Nếu NhomDangXet có tỉ lệ thời gian xử lí/ tổng số lớp = đợt hiện tại => Xét tiếp thời gian xử lí
                    if (NhomDangXet.WaitTime() / NhomDangXet.classrooms.Count == NhomDuocChon.WaitTime() / NhomDuocChon.classrooms.Count)
                    {
                        if (NhomDangXet.ProcessingTime() / NhomDangXet.classrooms.Count < NhomDuocChon.ProcessingTime() / NhomDuocChon.classrooms.Count)
                            NhomDuocChon.classrooms = NhomDangXet.DeepCopy().classrooms;
                    }
                    return;
                }

                for (int i = index; i < SelectedClasses.Count; i++)                             // Duyệt qua tất cả các lớp trong danh sách
                {
                    if (SelectedClasses[i].Check)                                                // Lớp đã được xử lí, bỏ qua
                        continue;
                    NhomDangXet.classrooms.Add(SelectedClasses[i]);                             // Thêm nhóm vào lớp
                    NhanhCan(NhomDangXet, i + 1, NhomDuocChon);                                 // Gọi đệ quy để thử các lớp tiếp theo, chọn các lớp sau để tránh chọn lớp cũ và trùng tổ hợp
                    NhomDangXet.classrooms.RemoveAt(NhomDangXet.classrooms.Count - 1);          // Quay lui: Bỏ lớp cuối cùng được thêm vào để thử các tổ hợp khác
                }
            }

        }   

        /// <summary>
        /// Tính thời gian tan học cho các lớp mới được thêm vào danh sách
        /// </summary>
        /// <param name="bestBatches"></param>
        // Done
        public void TinhThoiGianTanHoc(List<BatchScheduler> bestBatches, Entities2 db) {
            foreach (var batch in bestBatches) {
                {
                    batch.SetGioTanHocBanDau();
                    TimeSpan newDismissalTime;
                    // Đợt nào có thời gian tan học quá 11h55, set tan học lúc 11h55
                    if (batch.TimeCheck > 1200)
                        newDismissalTime = batch.DismissalTimeBatch.Add(TimeSpan.FromSeconds(1200));
                    else
                        newDismissalTime = batch.DismissalTimeBatch.Add(TimeSpan.FromSeconds(Math.Round(batch.TimeCheck)));
                    // Set thời gian tan học cho các lớp ở trong đợt
                    foreach (var classRooms in batch.classrooms)
                    {
                        classRooms.GioTanHoc = newDismissalTime;
                        var dbClass = db.LichHocs.FirstOrDefault(l => l.MaLichHoc == classRooms.MaLichHoc);
                        if (dbClass != null)
                            dbClass.GioTanHoc = newDismissalTime;
                    }
                }
            }
            db.SaveChanges();
        }
    }
}
