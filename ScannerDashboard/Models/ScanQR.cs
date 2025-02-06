namespace ScannerDashboard.Models
{
    public class ScanQR
    {
        public int Id { get; set; } // ID yang sama seperti di ScanRecords
        public string ScannedText { get; set; } // Data teks yang dipindahkan
        public DateTime ScanTime { get; set; } // Timestamp saat data dipindahkan
        public string IDQR { get; set; }
    }
}
