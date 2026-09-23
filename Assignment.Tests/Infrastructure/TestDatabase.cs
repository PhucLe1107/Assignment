using Assignment.Models.Data;
using Assignment.Models.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Assignment.Tests.Infrastructure;

/// <summary>
/// Database SQLite in-memory cho từng test. Dùng SQLite thay vì provider InMemory của EF
/// vì controller có gọi BeginTransactionAsync và cần kiểm tra ràng buộc khóa ngoại thật.
/// </summary>
public sealed class TestDatabase : IDisposable
{
    private readonly SqliteConnection _connection;

    public TestDatabase()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        using var context = CreateContext();
        context.Database.EnsureCreated();
    }

    /// <summary>Mỗi lần gọi trả về một DbContext mới trên cùng database, giống một request mới.</summary>
    public EnglishCenterDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<EnglishCenterDbContext>().UseSqlite(_connection).Options);

    public void Dispose() => _connection.Dispose();

    public SeedData Seed()
    {
        using var context = CreateContext();

        var admin = new Role { RoleName = "Admin" };
        var giaoVu = new Role { RoleName = "GiaoVu" };
        var sinhVien = new Role { RoleName = "SinhVien" };
        context.Roles.AddRange(admin, giaoVu, sinhVien);

        var ielts = new Course
        {
            CourseName = "IELTS Intensive 6.5+",
            BaseTuitionFee = 6_500_000,
            TotalSessions = 36,
            DescriptionHtml = "<p>IELTS</p>"
        };
        var toeic = new Course
        {
            CourseName = "TOEIC 650+",
            BaseTuitionFee = 4_000_000,
            TotalSessions = 24,
            DescriptionHtml = "<p>TOEIC</p>"
        };
        context.Courses.AddRange(ielts, toeic);

        var ieltsClass = NewClass("IELTS-01", ielts, maxCapacity: 2);
        var toeicClass = NewClass("TOEIC-01", toeic, maxCapacity: 20);
        context.Classes.AddRange(ieltsClass, toeicClass);

        var an = NewStudent("SV001", "Nguyễn Văn An", "an@example.com", "0900000001");
        var binh = NewStudent("SV002", "Trần Thị Bình", "binh@example.com", "0900000002");
        var cuong = NewStudent("SV003", "Lê Văn Cường", "cuong@example.com", "0900000003");
        context.Students.AddRange(an, binh, cuong);

        var anIelts = new Enrollment { Student = an, Class = ieltsClass, ActualFee = 6_500_000 };
        var anToeic = new Enrollment { Student = an, Class = toeicClass, ActualFee = 4_000_000 };
        context.Enrollments.AddRange(anIelts, anToeic);

        context.SaveChanges();

        return new SeedData(admin.RoleId, giaoVu.RoleId, sinhVien.RoleId,
            ielts.CourseId, toeic.CourseId, ieltsClass.ClassId, toeicClass.ClassId,
            an.StudentId, binh.StudentId, cuong.StudentId, anIelts.EnrollmentId, anToeic.EnrollmentId);
    }

    public static Class NewClass(string code, Course course, int maxCapacity = 20) => new()
    {
        ClassCode = code,
        ClassName = code,
        Course = course,
        LecturerName = "John Smith",
        Schedule = "2-4-6",
        Room = "P101",
        StartDate = new DateTime(2026, 10, 1),
        MaxCapacity = maxCapacity
    };

    public static Student NewStudent(string code, string name, string email, string phone) => new()
    {
        StudentCode = code,
        FullName = name,
        Email = email,
        PhoneNumber = phone
    };
}

public sealed record SeedData(
    int AdminRoleId, int GiaoVuRoleId, int SinhVienRoleId,
    int IeltsCourseId, int ToeicCourseId,
    int IeltsClassId, int ToeicClassId,
    int AnId, int BinhId, int CuongId,
    int AnIeltsEnrollmentId, int AnToeicEnrollmentId);
