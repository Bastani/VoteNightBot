using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VoteNightBot.Models;

namespace VoteNightBot.Services
{
    sealed class SqliteContext : DbContext
    {
        public DbSet<User> User { get; set; }
        public DbSet<Movie> Movie { get; set; }

        private readonly string _path;

        public SqliteContext()
        {
            _path = AppDomain.CurrentDomain.BaseDirectory + "Database.s3db";
            Configure();
        }

        private void Configure()
        {
            if (!File.Exists(_path))
            {
                File.Create(_path);
            }
            Database.EnsureCreated();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) =>
            optionsBuilder.UseSqlite("Data Source=" + _path + ";");

    }
}
