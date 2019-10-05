using Microsoft.EntityFrameworkCore;
using SubBox.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SubBox.Data
{
    public class AppDbContext : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Data Source=saves.db");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //Hier combined Keys festlegen
        }

        public DbSet<Video> Videos { get; set; }

        public DbSet<Channel> Channels { get; set; }

    }
}
