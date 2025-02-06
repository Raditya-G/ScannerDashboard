using Microsoft.AspNetCore.Mvc;
using ScannerDashboard.Data;
using ScannerDashboard.Models;
using OfficeOpenXml;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QRCoder;
using System.Drawing.Imaging;
using System.Drawing;
using System.Linq;
using QRCoder;
using System.Security.Cryptography.X509Certificates;

namespace ScannerDashboard.Controllers
{
    public class ScanController : Controller
    {
        private readonly AppDbContext _context;

        public ScanController(AppDbContext context)
        {
            _context = context;
            ExcelPackage.LicenseContext = LicenseContext.Commercial; // Atau LicenseContext.NonCommercial jika hanya untuk penggunaan non-komersial
        }

        public IActionResult Index()
        {
            var scans = _context.ScanRecords.OrderBy(s => s.Id).ToList(); // Mengurutkan ID dari kecil ke besar
            return View(scans);
        }

        [HttpPost]
        public async Task<IActionResult> Add(string scannedText)
        {
            if (!string.IsNullOrEmpty(scannedText))
            {
                var scanData = new ScanData { ScannedText = scannedText, ScanTime = DateTime.Now };
                _context.ScanRecords.Add(scanData);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var scanData = await _context.ScanRecords.FindAsync(id);
            if (scanData != null)
            {
                _context.ScanRecords.Remove(scanData);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult GenerateQRCode()
        {
            // Ambil semua data dari ScanRecords
            var scansToExport = _context.ScanRecords.ToList();

            if (scansToExport.Any())
            {
                // Buat IDQR berdasarkan tanggal dan counter
                string datePart = DateTime.Now.ToString("yyyyMMdd"); // Format: YYYYMMDD
                int lastIDQRNumber = _context.ScanQR
                    .Where(q => q.IDQR.StartsWith($"NF{datePart}"))
                    .Count() + 1; // Cari jumlah IDQR dengan tanggal yang sama, lalu +1

                string newIDQR = $"NF{datePart}-{lastIDQRNumber:D3}"; // Contoh hasil: QR20250131-001

                // Pindahkan data ke tabel ScanQR dengan IDQR yang sama
                foreach (var scan in scansToExport)
                {
                    var newScanQR = new ScanQR
                    {
                        ScannedText = scan.ScannedText,
                        ScanTime = scan.ScanTime,
                        IDQR = newIDQR // Set IDQR yang sama untuk semua entry hari ini
                    };

                    _context.ScanQR.Add(newScanQR);
                }

                // Simpan data ke ScanQR
                _context.SaveChanges();

                // Hapus semua data dari ScanRecords
                _context.ScanRecords.RemoveRange(scansToExport);
                _context.SaveChanges();

                // Reset ID di ScanRecords agar kembali ke 1
                _context.Database.ExecuteSqlRaw("DBCC CHECKIDENT ('ScanRecords', RESEED, 0)");

                return GenerateQRCodeImage(newIDQR);
            }

            return RedirectToAction("Index");
        }

        public IActionResult GenerateQRCodeImage(string idqr)
        {
            QRCodeGenerator qrGenerator = new QRCodeGenerator();
            QRCodeData qrCodeData = qrGenerator.CreateQrCode(idqr, QRCodeGenerator.ECCLevel.Q);
            PngByteQRCode qrCode = new PngByteQRCode(qrCodeData);
            byte[] qrCodeBytes = qrCode.GetGraphic(20);

            return File(qrCodeBytes, "image/png", $"{idqr}.png");
        }

        public IActionResult DaftarScan()
        {
            // Ambil semua data ScanQR
            var scanData = _context.ScanQR
                                   .GroupBy(s => s.IDQR)  // Group berdasarkan IDQR
                                   .Select(g => g.FirstOrDefault())  // Ambil hanya satu data per IDQR
                                   .ToList();  // Convert ke list

            return View(scanData);  // Kirim data unik ke view
        }



        // Menampilkan detail scan berdasarkan IDQR yang diklik
        public IActionResult DetailScan(string idqr)
        {
            Console.WriteLine($"Received IDQR: {idqr}");  // Periksa nilai idqr

            var scanData = _context.ScanQR.Where(s => s.IDQR == idqr).ToList();

            /*if (scanData == null)
            {
                Console.WriteLine("Data not found!");  // Debugging jika data tidak ditemukan
                return NotFound();
            }*/

            return View(scanData);
        }

        [HttpPost]
        public IActionResult DeleteIDQR(string idqr)
        {
            if (!string.IsNullOrEmpty(idqr))
            {
                var recordsToDelete = _context.ScanQR.Where(s => s.IDQR == idqr).ToList();

                if (recordsToDelete.Any())
                {
                    _context.ScanQR.RemoveRange(recordsToDelete);
                    _context.SaveChanges();
                }
            }

            return RedirectToAction("DaftarScan");
        }

        [HttpPost]
        public IActionResult ConvertExcel(string idqr)
        {
            var ExceltoDownload = _context.ScanQR.Where(s => s.IDQR == idqr).ToList();

            var stream = new MemoryStream();
            using (var package = new ExcelPackage(stream))
            {
                var worksheet = package.Workbook.Worksheets.Add("Scanned Data");

                // Menambahkan header kolom
                worksheet.Cells[1, 1].Value = "ID";
                worksheet.Cells[1, 2].Value = "Scanned Text";
                worksheet.Cells[1, 3].Value = "Timestamp";

                // Menambahkan data ke dalam worksheet
                int row = 2;
                foreach (var scan in ExceltoDownload)
                {
                    worksheet.Cells[row, 1].Value = scan.Id;
                    worksheet.Cells[row, 2].Value = scan.ScannedText;
                    worksheet.Cells[row, 3].Value = scan.ScanTime;
                    row++;
                }
                package.Save();
            }
            stream.Position = 0;

            string fileName = idqr + ".xlsx";
            return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        public IActionResult QRScan()
        {
            return View();
        }

        [HttpGet]
        public IActionResult GetScanQR(string idqr)
        {
            var scanData = _context.ScanQR.Where(s => s.IDQR == idqr).ToList();
            return Json(scanData);
        }

    }
}
