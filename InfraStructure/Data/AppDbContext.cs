using Domain.Entities;

using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfraStructure.Data
{
    public class AppDbContext : IdentityDbContext<AppUser>
    {
        public AppDbContext(DbContextOptions options) : base(options)
        {
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<EmailConfirmation>().HasKey(ec => ec.Email);
            modelBuilder.Entity<CourseCategory>()
                .HasKey(cc => new { cc.CourseId, cc.CategoryId });
            modelBuilder.Entity<AssignmentSubmission>()
                .HasKey(asub => new { asub.StudentId, asub.AssignmentId });
            modelBuilder.Entity<Certificate>()
                .HasKey(c => new { c.CourseId, c.StudentId });
            modelBuilder.Entity<Enrollment>()
                .HasKey(e => new { e.CourseId, e.StudentId });
            modelBuilder.Entity<Payment>()
                .HasKey(p => new { p.CourseId, p.StudentId });
            modelBuilder.Entity<Rating>()
                .HasKey(r => new { r.CourseId, r.StudentId });


        }


        public DbSet<Assignment> Assignments { get; set; }

        public DbSet<AssignmentSubmission> AssignmentSubmissions { get; set; }

        public DbSet<Category> Categories { get; set; }

        public DbSet<Certificate> Certificates { get; set; }

        public DbSet<Course> Courses { get; set; }

        public DbSet<CourseCategory> CourseCategories { get; set; }



        public DbSet<Enrollment> Enrollments { get; set; }

        public DbSet<Payment> Payments { get; set; }

        public DbSet<Rating> Ratings { get; set; }



        public DbSet<Session> Sessions { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<EmailConfirmation> EmailConfirmations { get; set; }
        
        
        }
}
