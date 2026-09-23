using Assignment.Routing;

namespace Assignment.Tests.Routing;

public class SlugifyParameterTransformerTests
{
    private readonly SlugifyParameterTransformer _transformer = new();

    [Theory]
    [InlineData("Course", "course")]
    [InlineData("TakeAttendance", "take-attendance")]
    [InlineData("StudentPortal", "student-portal")]
    [InlineData("GetClassDefaultFee", "get-class-default-fee")]
    [InlineData("Test2Result", "test2-result")]
    [InlineData("", null)]
    [InlineData(null, null)]
    public void TransformOutbound_ProducesLowercaseKebabCase(string? input, string? expected) =>
        Assert.Equal(expected, _transformer.TransformOutbound(input));
}
