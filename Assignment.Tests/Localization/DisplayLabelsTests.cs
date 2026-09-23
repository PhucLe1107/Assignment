using Assignment.Localization;

namespace Assignment.Tests.Localization;

public class DisplayLabelsTests
{
    [Theory]
    [InlineData("DangHoc", "Đang học")]
    [InlineData("BaoLuu", "Bảo lưu")]
    [InlineData("HoanThanh", "Hoàn thành")]
    [InlineData("NghiHoc", "Nghỉ học")]
    [InlineData("MaLa", "MaLa")]
    [InlineData(null, "")]
    public void LearningStatus_MapsCodeToVietnameseLabel(string? code, string expected) =>
        Assert.Equal(expected, DisplayLabels.LearningStatus(code));

    [Theory]
    [InlineData("GiuaKy", "Giữa kỳ")]
    [InlineData("CuoiKy", "Cuối khóa")]
    [InlineData("PlacementTest", "Đầu vào")]
    [InlineData("IELTS Mock 1", "IELTS Mock 1")] // kỳ thi do người dùng tự nhập được giữ nguyên
    public void ExamType_MapsKnownCodesAndKeepsCustomNames(string code, string expected) =>
        Assert.Equal(expected, DisplayLabels.ExamType(code));

    [Theory]
    [InlineData("Admin", "Admin")]
    [InlineData("GiaoVu", "Giáo vụ")]
    [InlineData("SinhVien", "Sinh viên")]
    public void Role_MapsCodeToLabel(string code, string expected) =>
        Assert.Equal(expected, DisplayLabels.Role(code));
}
