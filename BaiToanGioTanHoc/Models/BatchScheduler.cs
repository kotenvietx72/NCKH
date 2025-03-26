using BaiToanGioTanHoc.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChuongTrinhChinh
{
    internal class BatchScheduler
    {
        public List<LichHoc> classrooms { get; set; } = new List<LichHoc>();                        // Danh sách các lớp 
        public TimeSpan DismissalTimeBatch { get; set; }                                            // Thời gian tan học của một đợt
        public double TimeCheck { get; set; }                                                       // Thời gian được xử lí

        public BatchScheduler() { }

        public void SetGioTanHocBanDau()
        {
            if(classrooms != null)
            {
                if (classrooms[0].TietKetThuc == 6)
                    DismissalTimeBatch = new TimeSpan(11, 35, 0);
                if (classrooms[0].TietKetThuc == 12)
                    DismissalTimeBatch = new TimeSpan(17, 10, 0);
            }    
        }

        private readonly Entities2 db;
        public BatchScheduler(Entities2 db)
        {
            this.db = db;
        }

        public ThoiGianDiChuyen GetThoiGianDiChuyenHA8() => db.ThoiGianDiChuyens.FirstOrDefault(t => t.ToaNha == "HA8");

        public ThoiGianDiChuyen GetThoiGianDiChuyenHA9() => db.ThoiGianDiChuyens.FirstOrDefault(t => t.ToaNha == "HA9");

        public ThoiGianDiChuyen GetThoiGianDiChuyenHA10() => db.ThoiGianDiChuyens.FirstOrDefault(t => t.ToaNha == "HA10");

        public int GetSoCong() => (db.CongSoatVes.FirstOrDefault()).SoCong;

        public double? GetThoiGianXuLiSV() => (db.CongSoatVes.FirstOrDefault()).ThoiGianXuLi1SV;

        /// <summary>
        /// Hàm sao chép thông tin từ đợt này vào đợt khác        
        /// </summary>
        /// <returns></returns>
        // Done
        public BatchScheduler DeepCopy()
        {
            return new BatchScheduler(db)
            {
                classrooms = this.classrooms.Select(l => l.DeepCopy()).ToList(),
                DismissalTimeBatch = this.DismissalTimeBatch,
                TimeCheck = this.TimeCheck
            };
        }

        /// <summary>
        /// Hàm tính thời gian xử lí của i lớp đầu tiên trong list các lớp 
        /// </summary>
        /// <param name="room"></param>
        /// <param name="a"></param>
        /// <returns></returns>
        // Done
        public double? TotalTimeForFirst(int i)
        {
            if (i == 0) 
                return 0;
            return classrooms[i - 1].ExitTime() + TotalTimeForFirst(i - 1);
        }
        
        /// <summary>
        /// Hàm tính thời gian trống của a lớp đầu tiên trong đợt (Nếu có) 
        /// </summary>
        // Done
        public double? TinhTGTrong(int a)
        {
            double ThoiGianTrong = 0;
            for (int i = 1; i < a; i++)
            {
                double timeToGateI = classrooms[i].TimeToGate().GetValueOrDefault(); 
                double timeToGate0 = classrooms[0].TimeToGate().GetValueOrDefault();
                double totalTime = TotalTimeForFirst(i).GetValueOrDefault();

                if ((timeToGateI - timeToGate0) < totalTime + ThoiGianTrong)
                    ThoiGianTrong += 0;
                else
                    ThoiGianTrong += (timeToGateI - timeToGate0) - totalTime - ThoiGianTrong;
            }
            return ThoiGianTrong;
        }

        /// <summary>
        /// Hàm tính thời gian xử lí mỗi đợt
        /// </summary> 
        // Done
        public double? ProcessingTime() {
            classrooms = classrooms.OrderBy(x => x.TimeToGate()).ToList();
            double ThoiGianXuLi = 0;
            foreach (var classroom in classrooms)
                ThoiGianXuLi += classroom.ExitTime().GetValueOrDefault();
            return ThoiGianXuLi + TinhTGTrong(classrooms.Count);
        }

        /// <summary>
        /// Hàm tính thời gian chờ của 1 đợt
        /// </summary>
        /// <param name="classInformation"></param>
        /// <returns></returns>
        // Done
        public double? WaitTime()
        {
            classrooms = classrooms.OrderBy(x => x.TimeToGate()).ToList();
            double ThoiGianCho = 0;

            for (int i = 1; i < classrooms.Count; i++)
            {
                double totalTime = TotalTimeForFirst(i).GetValueOrDefault();
                double tgTrong = TinhTGTrong(i).GetValueOrDefault();
                double timeToGateI = classrooms[i].TimeToGate().GetValueOrDefault();
                double timeToGate0 = classrooms[0].TimeToGate().GetValueOrDefault();

                if (totalTime + tgTrong > (timeToGateI - timeToGate0))
                    ThoiGianCho += totalTime + tgTrong - (timeToGateI - timeToGate0);
                else
                    ThoiGianCho += 0;
            }

            return Math.Round(ThoiGianCho, 2);
        }

        /// <summary>
        /// Hàm tính tổng số sinh viên trong 1 đợt
        /// </summary>
        /// <param name="classrooms"></param>
        /// <returns></returns>
        // Done
        public int Count_Student()
        {
            if(classrooms.Count == 0) 
                return 0;
            return classrooms.Sum(c => c.LopHocPhan.SiSo) ?? 0;
        }

    }
}
