using Assignment.Models.Data;
using Assignment.Models.Entities;
using System;
using System.Linq;

namespace EnglishCenter.Models.Data
{
    public static class DbInitializer
    {
        public static void Initialize(EnglishCenterDbContext context)
        {
            context.Database.EnsureCreated();

            if (context.Roles.Any())
            {
                return;
            }

            var roles = new Role[]
            {
                new Role { RoleName = "Admin" },
                new Role { RoleName = "GiaoVu" },
                new Role { RoleName = "SinhVien" }
            };
            context.Roles.AddRange(roles);
            context.SaveChanges();

            var users = new User[]
            {
                new User
                {
                    Username = "admin",
                    Password = BCrypt.Net.BCrypt.HashPassword("admin@123"),
                    RoleId = roles[0].RoleId,
                    IsActive = true
                },
                new User
                {
                    Username = "giaovu01",
                    Password = BCrypt.Net.BCrypt.HashPassword("giaovu@123"),
                    RoleId = roles[1].RoleId,
                    IsActive = true
                },
                new User
                {
                    Username = "sv2026001",
                    Password = BCrypt.Net.BCrypt.HashPassword("123456"),
                    RoleId = roles[2].RoleId,
                    IsActive = true
                },
                new User
                {
                    Username = "sv2026002",
                    Password = BCrypt.Net.BCrypt.HashPassword("123456"),
                    RoleId = roles[2].RoleId,
                    IsActive = true
                }
            };
            context.Users.AddRange(users);
            context.SaveChanges();

            var students = new Student[]
            {
                new Student
                {
                    StudentCode = "SV2026001",
                    FullName = "Nguyễn Văn An",
                    DateOfBirth = new DateTime(2003, 5, 15),
                    Gender = true,
                    Email = "an.nguyen@gmail.com",
                    PhoneNumber = "0912345678",
                    Address = "Cầu Giấy, Hà Nội",
                    AvatarUrl = "/uploads/avatars/default.png",
                    EntryLevel = "Mất gốc",
                    UserId = users[2].UserId
                },
                new Student
                {
                    StudentCode = "SV2026002",
                    FullName = "Trần Thị Mai",
                    DateOfBirth = new DateTime(2004, 10, 20),
                    Gender = false,
                    Email = "mai.tran@gmail.com",
                    PhoneNumber = "0987654321",
                    Address = "Đống Đa, Hà Nội",
                    AvatarUrl = "/uploads/avatars/default.png",
                    EntryLevel = "B1 Pre-IELTS",
                    UserId = users[3].UserId
                },
                new Student
                {
                    StudentCode = "SV2026003",
                    FullName = "Lê Hoàng Long",
                    DateOfBirth = new DateTime(2002, 1, 8),
                    Gender = true,
                    Email = "long.le@gmail.com",
                    PhoneNumber = "0901234567",
                    Address = "Thanh Xuân, Hà Nội",
                    AvatarUrl = "/uploads/avatars/default.png",
                    EntryLevel = "TOEIC 500"
                }
            };
            context.Students.AddRange(students);
            context.SaveChanges();

            var courses = new Course[]
            {
                new Course
                {
                    CourseName = "Tiếng Anh Giao Tiếp Phản Xạ Cơ Bản",
                    ThumbnailUrl = "/uploads/courses/course-comm.jpg",
                    BaseTuitionFee = 3500000,
                    TotalSessions = 24,
                    DescriptionHtml = "<h4>Mục tiêu khóa học:</h4><ul><li>Lấy lại nền tảng phát âm chuẩn IPA.</li><li>Tự tin giao tiếp các chủ đề thường nhật.</li></ul>",
                    IsActive = true
                },
                new Course
                {
                    CourseName = "Luyện Thi IELTS Intensive 6.5+",
                    ThumbnailUrl = "/uploads/courses/course-ielts.jpg",
                    BaseTuitionFee = 6800000,
                    TotalSessions = 36,
                    DescriptionHtml = "<h4>Mục tiêu khóa học:</h4><ul><li>Nâng band điểm toàn diện 4 kỹ năng.</li><li>Cung cấp chiến thuật làm bài Listening & Reading đạt tối thiểu 7.0.</li></ul>",
                    IsActive = true
                }
            };
            context.Courses.AddRange(courses);
            context.SaveChanges();

            var classes = new Class[]
            {
                new Class
                {
                    ClassCode = "GT-K01",
                    ClassName = "Giao Tiếp K01 (Tối 2-4-6)",
                    CourseId = courses[0].CourseId,
                    LecturerName = "Thầy John Smith",
                    Schedule = "Thứ 2 - 4 - 6 (18:00 - 20:00)",
                    Room = "Phòng Lab 101",
                    StartDate = DateTime.Now.AddDays(-10),
                    EndDate = DateTime.Now.AddDays(50),
                    MaxCapacity = 15
                },
                new Class
                {
                    ClassCode = "IELTS-K02",
                    ClassName = "IELTS Chuyên Sâu K02 (Tối 3-5-7)",
                    CourseId = courses[1].CourseId,
                    LecturerName = "Cô Nguyễn Mai Hương (8.5 IELTS)",
                    Schedule = "Thứ 3 - 5 - 7 (19:30 - 21:30)",
                    Room = "Phòng Lab 202",
                    StartDate = DateTime.Now.AddDays(-5),
                    EndDate = DateTime.Now.AddDays(70),
                    MaxCapacity = 12
                }
            };
            context.Classes.AddRange(classes);
            context.SaveChanges();

            var enrollments = new Enrollment[]
            {
                new Enrollment
                {
                    StudentId = students[0].StudentId,
                    ClassId = classes[0].ClassId,
                    EnrollmentDate = DateTime.Now.AddDays(-15),
                    ActualFee = 3500000,
                    PaidAmount = 3500000,
                    PaymentStatus = "DaNopDu",
                    LearningStatus = "DangHoc"
                },
                new Enrollment
                {
                    StudentId = students[1].StudentId,
                    ClassId = classes[1].ClassId,
                    EnrollmentDate = DateTime.Now.AddDays(-12),
                    ActualFee = 6500000,
                    PaidAmount = 3000000,
                    PaymentStatus = "ConNo",
                    LearningStatus = "DangHoc"
                },
                new Enrollment
                {
                    StudentId = students[2].StudentId,
                    ClassId = classes[0].ClassId,
                    EnrollmentDate = DateTime.Now.AddDays(-10),
                    ActualFee = 3500000,
                    PaidAmount = 1000000,
                    PaymentStatus = "DatCoc",
                    LearningStatus = "DangHoc"
                }
            };
            context.Enrollments.AddRange(enrollments);
            context.SaveChanges();

            var attendances = new Attendance[]
            {
                new Attendance
                {
                    EnrollmentId = enrollments[0].EnrollmentId,
                    SessionNumber = 1,
                    AttendanceDate = DateTime.Now.AddDays(-8),
                    IsPresent = true,
                    Note = "Đến đúng giờ"
                },
                new Attendance
                {
                    EnrollmentId = enrollments[0].EnrollmentId,
                    SessionNumber = 2,
                    AttendanceDate = DateTime.Now.AddDays(-6),
                    IsPresent = true,
                    Note = "Phát biểu tích cực"
                },
                new Attendance
                {
                    EnrollmentId = enrollments[1].EnrollmentId,
                    SessionNumber = 1,
                    AttendanceDate = DateTime.Now.AddDays(-4),
                    IsPresent = false,
                    Note = "Vắng có phép (bận việc đột xuất)"
                }
            };
            context.Attendances.AddRange(attendances);
            context.SaveChanges();

            var grades = new Grade[]
            {
                new Grade
                {
                    EnrollmentId = enrollments[0].EnrollmentId,
                    ExamType = "GiuaKy",
                    ListeningScore = 7.5,
                    ReadingScore = 8.0,
                    WritingScore = 6.5,
                    SpeakingScore = 7.0,
                    OverallScore = 7.25,
                    TeacherFeedback = "Học viên có phản xạ giao tiếp rất tốt, cần cải thiện thêm ngữ pháp phần Writing."
                },
                new Grade
                {
                    EnrollmentId = enrollments[1].EnrollmentId,
                    ExamType = "PlacementTest",
                    ListeningScore = 6.0,
                    ReadingScore = 6.5,
                    WritingScore = 5.5,
                    SpeakingScore = 6.0,
                    OverallScore = 6.0,
                    TeacherFeedback = "Nền tảng từ vựng khá tốt, phát âm cần tự nhiên hơn."
                }
            };
            context.Grades.AddRange(grades);
            context.SaveChanges();
        }
    }
}