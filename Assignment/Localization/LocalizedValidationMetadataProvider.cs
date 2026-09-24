using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

namespace Assignment.Localization;

/// <summary>
/// Gán thông báo mặc định (tiếng Việt, cũng là key trong SharedResource) cho các validation attribute
/// không khai báo ErrorMessage, kể cả [Required] ngầm định của framework, để chúng được dịch theo ngôn ngữ.
/// </summary>
public sealed class LocalizedValidationMetadataProvider : IValidationMetadataProvider
{
    public void CreateValidationMetadata(ValidationMetadataProviderContext context)
    {
        var validators = context.ValidationMetadata.ValidatorMetadata;

        // Kiểu giá trị không nullable (int, bool, decimal...) được framework coi là bắt buộc nhưng tự tạo
        // RequiredAttribute không có ErrorMessage. Thêm sẵn attribute để có thể gán thông báo ở dưới.
        var modelType = context.Key.ModelType;
        if (context.Key.MetadataKind == ModelMetadataKind.Property
            && modelType.IsValueType
            && Nullable.GetUnderlyingType(modelType) == null
            && !validators.OfType<RequiredAttribute>().Any())
        {
            validators.Add(new RequiredAttribute());
        }

        foreach (var attribute in validators.OfType<ValidationAttribute>())
        {
            if (!string.IsNullOrEmpty(attribute.ErrorMessage) || attribute.ErrorMessageResourceType != null)
            {
                continue;
            }

            var message = attribute switch
            {
                RequiredAttribute => "Vui lòng nhập {0}.",
                StringLengthAttribute { MinimumLength: > 0 } => "{0} phải có từ {2} đến {1} ký tự.",
                StringLengthAttribute => "{0} không được vượt quá {1} ký tự.",
                MaxLengthAttribute => "{0} không được vượt quá {1} ký tự.",
                RangeAttribute => "{0} phải nằm trong khoảng {1} đến {2}.",
                EmailAddressAttribute => "{0} không đúng định dạng email.",
                PhoneAttribute => "{0} không đúng định dạng số điện thoại.",
                _ => null
            };

            if (message != null)
            {
                attribute.ErrorMessage = message;
            }
        }
    }
}
