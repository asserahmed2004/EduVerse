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
            
            modelBuilder.Entity<Enrollment>()
                .HasKey(e => new { e.CourseId, e.StudentId });
            modelBuilder.Entity<Payment>()
                .HasKey(p => new { p.CourseId, p.StudentId });
            modelBuilder.Entity<Rating>()
                .HasKey(r => new { r.CourseId, r.StudentId });

            modelBuilder.Entity<Course>()
                .HasOne<AppUser>()
                .WithMany()
                .HasForeignKey(c => c.OrgId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Course>()
                .HasOne<Organization>()
                .WithMany()
                .HasForeignKey(c => c.OrganizationId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Course>()
                .HasOne<AppUser>()
                .WithMany()
                .HasForeignKey(c => c.InstructorId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<AppUser>()
                .HasOne<Organization>()
                .WithMany()
                .HasForeignKey(u => u.OrganizationId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Session>()
                .HasOne<AppUser>()
                .WithMany()
                .HasForeignKey(s => s.TrainerId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Enrollment>()
                .HasOne<AppUser>()
                .WithMany()
                .HasForeignKey(e => e.StudentId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Payment>()
                .HasOne<AppUser>()
                .WithMany()
                .HasForeignKey(p => p.StudentId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Rating>()
                .HasOne<AppUser>()
                .WithMany()
                .HasForeignKey(r => r.StudentId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<AssignmentSubmission>()
                .HasOne<AppUser>()
                .WithMany()
                .HasForeignKey(asub => asub.StudentId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<StudentSessionProgress>()
                .HasIndex(p => new { p.StudentId, p.SessionId })
                .IsUnique();

            modelBuilder.Entity<CertificateRecord>()
                .HasIndex(c => c.CertificateCode)
                .IsUnique();

            modelBuilder.Entity<AttendanceRecord>()
                .HasIndex(a => new { a.StudentId, a.SessionId })
                .IsUnique();


        }


        public DbSet<Assignment> Assignments { get; set; }

        public DbSet<AssignmentSubmission> AssignmentSubmissions { get; set; }

        public DbSet<Category> Categories { get; set; }

        

        public DbSet<Course> Courses { get; set; }

        public DbSet<CourseCategory> CourseCategories { get; set; }



        public DbSet<Enrollment> Enrollments { get; set; }

        public DbSet<Payment> Payments { get; set; }

        public DbSet<Rating> Ratings { get; set; }



        public DbSet<Session> Sessions { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<EmailConfirmation> EmailConfirmations { get; set; }
        public DbSet<ActivityLog> ActivityLogs { get; set; }
        public DbSet<Organization> Organizations { get; set; }
        public DbSet<StudentSessionProgress> StudentSessionProgresses { get; set; }
        public DbSet<CertificateRecord> CertificateRecords { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<AttendanceRecord> AttendanceRecords { get; set; }
        public DbSet<SessionMaterial> SessionMaterials { get; set; }
        
        
        }
}
