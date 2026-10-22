using Microsoft.EntityFrameworkCore;
using StudentManagement.Api.Entities;

namespace StudentManagement.Api.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Course> Courses { get; set; }
        public DbSet<Enrollment> Enrollments { get; set; }
        public DbSet<CourseChapter> CourseChapters { get; set; }
        public DbSet<Lesson> Lessons { get; set; }
        public DbSet<LessonResource> LessonResources { get; set; }
        public DbSet<LessonProgress> LessonProgresses { get; set; }
        public DbSet<Assignment> Assignments { get; set; }
        public DbSet<Submission> Submissions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("Users");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Username).IsUnique();
                entity.Property(e => e.Username).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(200);
                entity.Property(e => e.PasswordHash).IsRequired();
                entity.Property(e => e.FullName).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Role).IsRequired().HasMaxLength(50);
            });

            // Course
            modelBuilder.Entity<Course>(entity =>
            {
                entity.ToTable("Courses");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Code).IsUnique();
                entity.Property(e => e.Code).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(250);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(50);

                entity.HasOne(e => e.Teacher)
                      .WithMany(u => u.TaughtCourses)
                      .HasForeignKey(e => e.TeacherId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Enrollment
            modelBuilder.Entity<Enrollment>(entity =>
            {
                entity.ToTable("Enrollments");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.CourseId, e.StudentId }).IsUnique();

                entity.HasOne(e => e.Course)
                      .WithMany(c => c.Enrollments)
                      .HasForeignKey(e => e.CourseId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Student)
                      .WithMany(u => u.Enrollments)
                      .HasForeignKey(e => e.StudentId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // CourseChapter
            modelBuilder.Entity<CourseChapter>(entity =>
            {
                entity.ToTable("CourseChapters");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(250);

                entity.HasOne(e => e.Course)
                      .WithMany(c => c.Chapters)
                      .HasForeignKey(e => e.CourseId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Lesson
            modelBuilder.Entity<Lesson>(entity =>
            {
                entity.ToTable("Lessons");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(250);

                entity.HasOne(e => e.Chapter)
                      .WithMany(c => c.Lessons)
                      .HasForeignKey(e => e.ChapterId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // LessonResource
            modelBuilder.Entity<LessonResource>(entity =>
            {
                entity.ToTable("LessonResources");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.FileName).IsRequired().HasMaxLength(255);
                entity.Property(e => e.FilePath).IsRequired().HasMaxLength(500);

                entity.HasOne(e => e.Lesson)
                      .WithMany(l => l.Resources)
                      .HasForeignKey(e => e.LessonId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // LessonProgress
            modelBuilder.Entity<LessonProgress>(entity =>
            {
                entity.ToTable("LessonProgresses");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.LessonId, e.StudentId }).IsUnique();

                entity.HasOne(e => e.Lesson)
                      .WithMany(l => l.Progresses)
                      .HasForeignKey(e => e.LessonId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Student)
                      .WithMany(u => u.LessonProgresses)
                      .HasForeignKey(e => e.StudentId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Assignment
            modelBuilder.Entity<Assignment>(entity =>
            {
                entity.ToTable("Assignments");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(250);
                entity.Property(e => e.MaxScore).HasColumnType("decimal(5,2)");

                entity.HasOne(e => e.Course)
                      .WithMany(c => c.Assignments)
                      .HasForeignKey(e => e.CourseId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Submission
            modelBuilder.Entity<Submission>(entity =>
            {
                entity.ToTable("Submissions");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.AssignmentId, e.StudentId });
                entity.Property(e => e.Score).HasColumnType("decimal(5,2)");
                entity.Property(e => e.Status).IsRequired().HasMaxLength(50);

                entity.HasOne(e => e.Assignment)
                      .WithMany(a => a.Submissions)
                      .HasForeignKey(e => e.AssignmentId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Student)
                      .WithMany(u => u.Submissions)
                      .HasForeignKey(e => e.StudentId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.GradedBy)
                      .WithMany()
                      .HasForeignKey(e => e.GradedById)
                      .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
