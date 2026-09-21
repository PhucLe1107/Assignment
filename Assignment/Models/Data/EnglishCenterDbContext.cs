using Assignment.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Assignment.Models.Data
{
    public class EnglishCenterDbContext : DbContext
    {
        public EnglishCenterDbContext(DbContextOptions<EnglishCenterDbContext> options) : base(options)
        {
        }

        public DbSet<Role> Roles { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Student> Students { get; set; }
        public DbSet<Course> Courses { get; set; }
        public DbSet<Class> Classes { get; set; }
        public DbSet<Enrollment> Enrollments { get; set; }
        public DbSet<Attendance> Attendances { get; set; }
        public DbSet<Grade> Grades { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Role>().ToTable(nameof(Role));
            modelBuilder.Entity<User>().ToTable(nameof(User));
            modelBuilder.Entity<Student>().ToTable(nameof(Student));
            modelBuilder.Entity<Course>().ToTable(nameof(Course));
            modelBuilder.Entity<Class>().ToTable(nameof(Class));
            modelBuilder.Entity<Enrollment>().ToTable(nameof(Enrollment));
            modelBuilder.Entity<Attendance>().ToTable(nameof(Attendance));
            modelBuilder.Entity<Grade>().ToTable(nameof(Grade));

            modelBuilder.Entity<Enrollment>()
                .HasOne(e => e.Student)
                .WithMany(s => s.Enrollments)
                .HasForeignKey(e => e.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Enrollment>()
                .HasOne(e => e.Class)
                .WithMany(c => c.Enrollments)
                .HasForeignKey(e => e.ClassId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
