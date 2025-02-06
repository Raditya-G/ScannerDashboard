using System;
using System.ComponentModel.DataAnnotations;
namespace ScannerDashboard.Models
{
    public class ScanData
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string ScannedText { get; set; }

        public DateTime ScanTime { get; set; } = DateTime.Now;
    }
}
