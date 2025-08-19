using Microsoft.EntityFrameworkCore;
using Serilog;
using TWMDocImg.Application.Interfaces;
using TWMDocImg.Infrastructure.Messaging;
using TWMDocImg.Infrastructure.Messaging.Kafka.Extensions;
using TWMDocImg.Infrastructure.Persistence;
using TWMDocImg.Infrastructure.Messaging.EventBus;
using TWMDocImg.Application.Configurations;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("Logs/twm-doc-img-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

//bind FileUploadOptions from configuration
builder.Services.Configure<FileUploadOptions>(
    builder.Configuration.GetSection("FileUploadOptions"));

builder.Host.UseSerilog();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// -- ���μh�P��¦�]�I�h�A�ȵ��U --

// ���U Kafka �Ͳ��� (Singleton)
builder.Services.AddSingleton<IFileQueueService, KafkaProducerService>();

// ���U��Ʈw�x�s�A�� (Scoped)
builder.Services.AddScoped<IDocumentStorageService, DocumentStorageService>();

// ���U EF Core DbContext
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Server=(localdb)\\mssqllocaldb;Database=TWMDocImgDb;Trusted_Connection=True;";
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

// 註冊 Kafka 自動發現處理與背景消費者
builder.Services.AddKafkaHandlers();

// 註冊事件匯流排
builder.Services.AddEventBus();

// 註冊業務服務
builder.Services.AddBusinessServices();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// �[�J Serilog �ШD��x
app.UseSerilogRequestLogging();

app.UseAuthorization();

app.MapControllers();

try
{
    Log.Information("���ε{���Ұʤ�...");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "���ε{���Ұʥ���");
}
finally
{
    Log.CloseAndFlush();
}