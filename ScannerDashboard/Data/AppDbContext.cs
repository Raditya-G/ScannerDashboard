using Microsoft.EntityFrameworkCore;
using ScannerDashboard.Models;
using System.Collections.Generic;

namespace ScannerDashboard.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
        public DbSet<ScanData> ScanRecords { get; set; }
        public DbSet<ScanQR> ScanQR { get; set; }
    }
}
