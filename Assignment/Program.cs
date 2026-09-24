using Assignment;
using Assignment.Localization;
using Assignment.Models.Data;
using Assignment.Routing;
using Assignment.Storage;
using Amazon.Runtime;
using Amazon.S3;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

// URL chữ thường, action nhiều chữ dùng gạch ngang: /attendance/take-attendance
// (không lowercase query string để giữ nguyên giá trị như examType=GiuaKy)
builder.Services.AddRouting(options =>
{
    options.LowercaseUrls = true;
    options.ConstraintMap["slugify"] = typeof(SlugifyParameterTransformer);
});

builder.Services
    .AddControllersWithViews(options =>
    {
        options.ModelMetadataDetailsProviders.Add(new LocalizedValidationMetadataProvider());
    })
    .AddViewLocalization(LanguageViewLocationExpanderFormat.Suffix)
    .AddDataAnnotationsLocalization(options =>
    {
        options.DataAnnotationLocalizerProvider = (_, factory) => factory.Create(typeof(SharedResource));
    });

builder.Services.AddSingleton<IConfigureOptions<MvcOptions>, ConfigureModelBindingLocalization>();

builder.Services
    .AddOptions<R2Options>()
    .Bind(builder.Configuration.GetSection(R2Options.SectionName))
    .ValidateDataAnnotations()
    .Validate(
        options => Uri.TryCreate(options.PublicBaseUrl, UriKind.Absolute, out var uri)
                   && uri.Scheme == Uri.UriSchemeHttps
                   && !uri.Host.EndsWith(".r2.cloudflarestorage.com", StringComparison.OrdinalIgnoreCase),
        "R2:PublicBaseUrl must be a public custom domain or r2.dev URL, not the S3 API endpoint.")
    .ValidateOnStart();

builder.Services.AddSingleton<IAmazonS3>(serviceProvider =>
{
    var options = serviceProvider.GetRequiredService<IOptions<R2Options>>().Value;
    var credentials = new BasicAWSCredentials(options.AccessKeyId, options.SecretAccessKey);

    return new AmazonS3Client(credentials, new AmazonS3Config
    {
        ServiceURL = $"https://{options.AccountId}.r2.cloudflarestorage.com",
        AuthenticationRegion = "auto",
        ForcePathStyle = true
    });
});
builder.Services.AddSingleton<IImageStorage, R2ImageStorage>();

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var supportedCultures = new[]
    {
        new CultureInfo("vi-VN"),
        new CultureInfo("en-US")
    };

    options.DefaultRequestCulture = new RequestCulture("vi-VN");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
    options.RequestCultureProviders =
    [
        new CookieRequestCultureProvider(),
        new QueryStringRequestCultureProvider()
    ];
});

builder.Services.AddDbContext<EnglishCenterDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/account/login";
        options.AccessDeniedPath = "/account/denied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
    });

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<EnglishCenterDbContext>();
        EnglishCenter.Models.Data.DbInitializer.Initialize(context);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Đã xảy ra lỗi trong quá trình nạp dữ liệu mẫu vào Database.");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/home/error");
}
app.UseRouting();

app.UseRequestLocalization();
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller:slugify=Home}/{action:slugify=Index}/{id?}")
    .WithStaticAssets();


app.Run();
