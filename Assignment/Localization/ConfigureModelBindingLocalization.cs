using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace Assignment.Localization;

/// <summary>
/// Dịch các thông báo lỗi model binding mặc định của framework
/// (vd: "The field X must be a number.", "The value 'abc' is not valid for X.").
/// </summary>
public sealed class ConfigureModelBindingLocalization : IConfigureOptions<MvcOptions>
{
    private readonly IStringLocalizerFactory _localizerFactory;

    public ConfigureModelBindingLocalization(IStringLocalizerFactory localizerFactory)
    {
        _localizerFactory = localizerFactory;
    }

    public void Configure(MvcOptions options)
    {
        var L = _localizerFactory.Create(typeof(SharedResource));
        var messages = options.ModelBindingMessageProvider;

        messages.SetValueMustBeANumberAccessor(name => L["{0} phải là một số.", name]);
        messages.SetNonPropertyValueMustBeANumberAccessor(() => L["Giá trị phải là một số."]);
        messages.SetValueMustNotBeNullAccessor(value => L["Giá trị '{0}' không hợp lệ.", value]);
        messages.SetValueIsInvalidAccessor(value => L["Giá trị '{0}' không hợp lệ.", value]);
        messages.SetNonPropertyAttemptedValueIsInvalidAccessor(value => L["Giá trị '{0}' không hợp lệ.", value]);
        messages.SetAttemptedValueIsInvalidAccessor((value, name) => L["Giá trị '{0}' không hợp lệ cho {1}.", value, name]);
        messages.SetUnknownValueIsInvalidAccessor(name => L["Giá trị nhập cho {0} không hợp lệ.", name]);
        messages.SetNonPropertyUnknownValueIsInvalidAccessor(() => L["Giá trị nhập không hợp lệ."]);
        messages.SetMissingBindRequiredValueAccessor(name => L["Thiếu giá trị cho {0}.", name]);
        messages.SetMissingKeyOrValueAccessor(() => L["Vui lòng nhập giá trị."]);
        messages.SetMissingRequestBodyRequiredValueAccessor(() => L["Thiếu dữ liệu gửi lên."]);
    }
}
