# English Center MVC

Ứng dụng ASP.NET Core MVC quản lý khóa học, lớp học, ghi danh, điểm danh và điểm số cho trung tâm tiếng Anh.

## Yêu cầu

- .NET 10 SDK.
- SQL Server có database/user cho môi trường local.
- Chrome hoặc Edge để chạy bộ kiểm thử thủ công.

## Cấu hình local

Tạo `Assignment/appsettings.json` hoặc cấu hình user secrets với connection string `DefaultConnection`. Không commit mật khẩu thật.

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER;Database=EnglishCenterDb;User Id=YOUR_USER;Password=YOUR_PASSWORD;TrustServerCertificate=True;MultipleActiveResultSets=true"
  }
}
```

Ứng dụng hiện dùng `EnsureCreated`, vì vậy database và dữ liệu mẫu được tạo khi chạy lần đầu. Dự án chưa có EF migrations.

## Chạy ứng dụng

```bash
dotnet restore Assignment.slnx
dotnet build Assignment.slnx --no-restore
dotnet run --project Assignment/Assignment.csproj
```

URL local được in trong terminal theo profile đang dùng.

## Tài khoản seed

| Username | Password | Role |
| --- | --- | --- |
| `admin` | `admin@123` | Admin |
| `giaovu01` | `giaovu@123` | GiaoVu |
| `sv2026001` | `123456` | SinhVien |
| `sv2026002` | `123456` | SinhVien |

Các tài khoản trên chỉ dùng cho local/test.

## QA và bàn giao

- [Kế hoạch Waterfall](docs/WATERFALL_PLAN.md)
- [Excel test cases](docs/EnglishCenter_QA_TestCases.xlsx)
- Nguồn tái tạo workbook: `python3 scripts/generate_qa_workbook.py` (cần `openpyxl`).

Thứ tự kiểm thử chuẩn: build -> smoke -> functional -> authorization -> regression -> retest -> sign-off. Exit criteria là smoke pass 100%, functional pass tối thiểu 95%, không còn bug Blocker/Critical và không còn test bị Blocked hoặc chưa chạy.
