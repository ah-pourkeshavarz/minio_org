using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// افزودن سرویس‌های ضروری
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// فعال کردن Swagger فقط در حالت توسعه
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// حذف یا کامنت کردن خط https redirection برای جلوگیری از ارور
// app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
