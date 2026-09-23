# Kế hoạch Waterfall - English Center MVC

## 1. Mục tiêu và phạm vi

Kế hoạch này áp dụng cho hệ thống quản lý học viên trung tâm tiếng Anh xây dựng bằng ASP.NET Core MVC, EF Core và SQL Server. Phạm vi chức năng gồm:

- Đăng nhập, đăng xuất và phân quyền `Admin`, `GiaoVu`, `SinhVien`.
- Quản lý khóa học, lớp học, ghi danh, điểm danh và điểm số.
- Tìm kiếm, lọc, phân trang và kiểm tra dữ liệu đầu vào.
- Quy tắc sĩ số, học phí, dữ liệu liên kết và tính điểm IELTS/TOEIC.
- Chuẩn bị tài liệu chạy ứng dụng, kiểm thử và bàn giao.

Ngoài phạm vi: thanh toán trực tuyến, gửi email/SMS, cổng học viên riêng, API công khai, kiểm thử tải và triển khai production thực tế.

## 2. Baseline kỹ thuật

- Solution: `Assignment.slnx`.
- Runtime: .NET 10, ASP.NET Core MVC.
- Persistence: EF Core với SQL Server; dữ liệu mẫu được tạo bằng `EnsureCreated` và `DbInitializer`.
- Xác thực: cookie authentication, mật khẩu BCrypt.
- Build baseline: `dotnet build Assignment.slnx --no-restore` thành công.
- Nợ kỹ thuật đã biết: chưa có test project, chưa có EF migrations; còn cảnh báo nullability, package advisory mức thấp và `using` trùng trong initializer.

## 3. Lịch Waterfall dự kiến

| Tuần | Pha | Công việc chính | Deliverable | Gate |
| --- | --- | --- | --- | --- |
| 1 | Requirements | Chốt actor, phạm vi, yêu cầu chức năng/phi chức năng, acceptance criteria | SRS baseline, danh sách requirement trong workbook | Requirement được duyệt và gắn mã REQ |
| 1 | Analysis | Use case, business rule, ràng buộc dữ liệu, rủi ro | Rule catalog, use-case flow, RTM bản đầu | Mỗi rule có requirement và test coverage dự kiến |
| 2 | Design | Kiến trúc MVC/EF, entity relationship, navigation flow, authorization matrix | Thiết kế kỹ thuật, ma trận quyền | Thiết kế đủ để triển khai không còn quyết định nghiệp vụ mở |
| 2-3 | Implementation | Course -> Class -> Enrollment -> Attendance -> Grade -> Auth polish | Build chạy được theo từng module | Mỗi module build pass và qua smoke test trước khi sang module kế |
| 3-4 | Testing | Smoke, functional, validation, authorization, regression, retest | Workbook QA hoàn chỉnh, bug log, test summary | Smoke 100%, functional >= 95%, không còn Blocker/Critical |
| 4 | Deployment & Handover | Chuẩn hóa hướng dẫn chạy, cấu hình mẫu, release note, sign-off | Gói bàn giao và biên bản kiểm thử | Người nhận chạy được app và xác nhận test summary |

## 4. Yêu cầu và quy tắc nghiệp vụ

### Actor và quyền

| Chức năng | Admin | GiaoVu | SinhVien | Chưa đăng nhập |
| --- | --- | --- | --- | --- |
| Đăng nhập/đăng xuất | Có | Có | Có | Chỉ đăng nhập |
| Course, Class, Enrollment | Toàn quyền | Toàn quyền | Bị từ chối | Chuyển đến đăng nhập |
| Attendance, Grade | Toàn quyền | Toàn quyền | Bị từ chối | Chuyển đến đăng nhập |

### Quy tắc chính

- Không cho phép trùng `ClassCode` khi tạo lớp.
- Không cho phép cùng một học viên ghi danh hai lần vào cùng một lớp.
- Không cho phép ghi danh khi số học viên đã đạt `MaxCapacity`.
- `PaymentStatus = DaNopDu` khi `PaidAmount >= ActualFee` và `ActualFee > 0`; các trường hợp còn lại là `ConNo`.
- Không xóa khóa học đã có lớp, lớp đã có ghi danh, hoặc ghi danh đã có điểm danh/điểm số.
- Sổ điểm danh và sổ điểm chỉ hiển thị ghi danh có `LearningStatus = DangHoc`.
- TOEIC: tổng điểm bằng Listening + Reading khi có ít nhất một trong hai điểm.
- IELTS: trung bình các kỹ năng đã nhập và làm tròn về band `.0`/`.5`; phần lẻ `< 0.25` xuống `.0`, `< 0.75` về `.5`, còn lại lên số nguyên kế tiếp.
- Khóa học khác: trung bình các kỹ năng đã nhập, làm tròn một chữ số thập phân.

## 5. Chiến lược triển khai theo module

1. Course: xác thực dữ liệu, ảnh mặc định/upload, tìm kiếm, phân trang, chặn xóa khi có lớp.
2. Class: mã lớp duy nhất, khóa học đang hoạt động, sĩ số, tìm kiếm/lọc, chặn xóa khi có ghi danh.
3. Enrollment: học phí mặc định, chống trùng, kiểm soát sĩ số, tự tính trạng thái học phí, bảo vệ dữ liệu liên kết.
4. Attendance: lọc học viên đang học, tạo/cập nhật theo lớp và buổi, tìm kiếm/lọc/xóa.
5. Grade: tạo/cập nhật theo kỳ, validation điểm, công thức IELTS/TOEIC, tìm kiếm/lọc/xóa.
6. Auth polish: return URL nội bộ, cookie remember-me, logout, denied page và kiểm tra quyền cho toàn bộ route quản trị.

Mỗi module chỉ qua gate khi build thành công, smoke test liên quan đạt 100% và không còn lỗi Blocker/Critical của module.

## 6. Chiến lược kiểm thử

- Workbook chính: `docs/EnglishCenter_QA_TestCases.xlsx`.
- Test data seed: `admin`, `giaovu01`, `sv2026001`, `sv2026002` theo mật khẩu trong `Assignment/README.md`.
- Trình duyệt mục tiêu: Chrome và Edge phiên bản ổn định mới nhất; viewport desktop tối thiểu 1366x768.
- Môi trường: Development, SQL Server sạch có thể tạo lại từ seed.
- Thứ tự chạy: build -> smoke -> functional theo module -> authorization -> regression -> retest bug.
- Actual Result, Status, Tester và Date được cập nhật trực tiếp trong sheet `Test Cases`.
- Mỗi test fail phải có bản ghi trong `Bug Log` và liên kết ngược bằng `Linked TC`.

### Entry criteria

- Requirement và rule đã được chốt mã REQ.
- Build thành công và database test khởi tạo được.
- Seed account đăng nhập được; dữ liệu dùng cho test được nhận diện rõ.
- Không có blocker môi trường ngăn chạy các luồng chính.

### Exit criteria

- Build pass.
- Smoke checklist pass 100%.
- Tối thiểu 95% test functional đã chạy đạt Pass; test N/A không tính vào mẫu số.
- Không còn bug Blocker/Critical mở.
- Bug High/Medium/Low còn lại có owner, trạng thái và quyết định chấp nhận/defer rõ ràng.
- `Test Summary` được QA và người phụ trách dự án sign-off.

## 7. Triển khai và bàn giao

1. Cấu hình `DefaultConnection` bằng secret/local settings; không commit mật khẩu thật.
2. Restore/build bằng `dotnet restore` và `dotnet build Assignment.slnx`.
3. Chạy ứng dụng, xác nhận database được tạo và dữ liệu seed có mặt.
4. Chạy `Smoke Checklist`, sau đó chạy regression theo release candidate.
5. Chốt `Bug Log`, `Test Summary`, release note và người sign-off.
6. Bàn giao source, hướng dẫn chạy, workbook QA và danh sách nợ kỹ thuật.

## 8. Rủi ro và kiểm soát

| Rủi ro | Ảnh hưởng | Kiểm soát |
| --- | --- | --- |
| `EnsureCreated` không quản lý thay đổi schema | Khó nâng cấp database | Lập kế hoạch chuyển sang EF migrations trước production |
| Chưa có automated tests | Regression thủ công tốn thời gian | Ưu tiên unit test công thức điểm và integration test authorization/business rule |
| Upload ảnh chưa giới hạn dung lượng/định dạng phía server | Rủi ro bảo mật và dung lượng | Bổ sung whitelist MIME/extension, giới hạn kích thước, test upload xấu |
| Nullable warnings | Có thể phát sinh lỗi runtime | Xử lý theo module và bật gate không tăng warning mới |
| Package advisory | Rủi ro dependency | Kiểm tra bản vá tương thích trước release |
| Dữ liệu seed dùng chung | Test phụ thuộc thứ tự | Reset database hoặc tạo data riêng có tiền tố QA cho từng vòng chạy |

