# HỆ THỐNG QUẢN LÝ SINH VIÊN TRUNG TÂM TIẾNG ANH

## 1. Cấu hình Database (appsettings.json)

Mở file appsettings.json ở thư mục gốc và sửa lại thông số cho khớp với SQL Server trên máy bạn:

* Máy dùng SQL Server Authentication (có tài khoản sa trong thư mục Security > Logins):
"DefaultConnection": "Server=TÊN_SERVER_DATABASE;Database=EnglishCenterDb;Persist Security Info=True;User Id=sa;Password=MẬT_KHẨU_SQL_CỦA_TÀI KHOẢN_SA;TrustServerCertificate=True;MultipleActiveResultSets=true"
(Ví dụ: Server=.\\SQLEXPRESS hoặc Server=.)

* Máy dùng Windows Authentication (không dùng mật khẩu):
"DefaultConnection": "Server=TÊN_SERVER_DATABASE;Database=EnglishCenterDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"

Lưu ý: Chỉnh xong chuỗi kết nối chỉ cần nhấn F5 chạy dự án, hệ thống sẽ tự động tạo database EnglishCenterDb và tự động nạp sẵn dữ liệu mẫu.

---

## 2. Dữ liệu mẫu bảng User (Tài khoản test đăng nhập)

Mật khẩu trong CSDL đã được mã hóa băm bằng BCrypt. Khi đăng nhập trên giao diện web, sử dụng Mật khẩu gốc sau:

| Tên đăng nhập | Mật khẩu | Vai trò | Chú thích |
| :--- | :--- | :--- | :--- |
| admin | admin@123 | Admin | Quản trị viên |
| giaovu01 | giaovu@123 | Giáo vụ | Cán bộ giáo vụ |
| sv2026001 | 123456 | Sinh viên | Học viên Nguyễn Văn An |
| sv2026002 | 123456 | Sinh viên | Học viên Trần Thị Mai |