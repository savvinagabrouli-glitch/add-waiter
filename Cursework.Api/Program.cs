using Cursework.Api.Hubs;
using Cursework.Api.Realtime;
using Cursework.Application.Interfaces;
using Cursework.Application.Realtime;
using Cursework.Application.Security;
using Cursework.Application.Services;
using Cursework.Persistence.Db;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

QuestPDF.Settings.License = LicenseType.Community;

var cs = builder.Configuration.GetConnectionString("Default")
         ?? throw new InvalidOperationException("ConnectionStrings:Default is missing");

builder.Services
    .AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var logger = context.HttpContext
                                .RequestServices
                                .GetRequiredService<ILogger<Program>>();

            var errors = string.Join("; ",
                context.ModelState.Values.SelectMany(v => v.Errors)
                      .Select(e => e.ErrorMessage));

            logger.LogWarning("Invalid model: {Errors}", errors);

            return new BadRequestObjectResult(context.ModelState);
        };
    });


// EF Core + application-сервисы
builder.Services.AddDbContext<AppDbContext>(o => o.UseSqlServer(cs));
builder.Services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
builder.Services.AddScoped<IStaffService, StaffService>();
builder.Services.AddScoped<ITableService, TableService>();
builder.Services.AddScoped<IMenuService, MenuService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IOrderDetailsService, OrderDetailsService>();
builder.Services.AddScoped<ICallWaiterService, CallWaiterService>();

// Реалтайм-уведомления (Application-интерфейсы -> API-реализации)
builder.Services.AddScoped<IOrderNotifications, OrderNotifications>();
builder.Services.AddScoped<ICallWaiterNotifications, CallWaiterNotifications>();
builder.Services.AddScoped<ITableNotifications, TableNotifications>();
builder.Services.AddScoped<IStaffNotifications, StaffNotifications>();

// SignalR
builder.Services.AddSignalR();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseStaticFiles();
app.MapControllers();
app.MapHub<NotificationsHub>("/hubs/notifications");
app.Run();
