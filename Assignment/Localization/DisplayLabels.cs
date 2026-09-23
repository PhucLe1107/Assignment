namespace Assignment.Localization;

/// <summary>
/// Đổi mã lưu trong database (DangHoc, GiuaKy...) sang nhãn tiếng Việt, cũng là key trong SharedResource.
/// Dùng trong view: @L[DisplayLabels.LearningStatus(item.LearningStatus)].
/// </summary>
public static class DisplayLabels
{
    public static string LearningStatus(string? code) => code switch
    {
        "DangHoc" => "Đang học",
        "BaoLuu" => "Bảo lưu",
        "HoanThanh" => "Hoàn thành",
        "NghiHoc" => "Nghỉ học",
        _ => code ?? string.Empty
    };

    public static string Role(string? code) => code switch
    {
        "Admin" => "Admin",
        "GiaoVu" => "Giáo vụ",
        "SinhVien" => "Sinh viên",
        _ => code ?? string.Empty
    };

    // Kỳ kiểm tra có thể do người dùng tự nhập nên mã lạ được giữ nguyên
    public static string ExamType(string? code) => code switch
    {
        "GiuaKy" => "Giữa kỳ",
        "CuoiKy" => "Cuối khóa",
        "PlacementTest" => "Đầu vào",
        "ProgressTest1" => "Test tiến độ 1",
        "ProgressTest2" => "Test tiến độ 2",
        _ => code ?? string.Empty
    };
}
