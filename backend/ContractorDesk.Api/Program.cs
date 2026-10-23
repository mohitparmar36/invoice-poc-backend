using System.Text.Json.Serialization;
using ContractorDesk.Api.Data;
using ContractorDesk.Api.Models.Invoice;
using ContractorDesk.Api.Models.Quotation;
using ContractorDesk.Api.Services.Calculations;
using ContractorDesk.Api.Services.Documents;
using ContractorDesk.Api.Services.Pdf;
using ContractorDesk.Api.Validation;
using FluentValidation;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Infrastructure;

QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

const string FrontendCorsPolicy = "FrontendCors";

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontendCorsPolicy, policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://127.0.0.1:5173")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddScoped<IValidator<QuotationRequest>, QuotationRequestValidator>();
builder.Services.AddScoped<IValidator<InvoiceRequest>, InvoiceRequestValidator>();
builder.Services.AddScoped<ITotalsCalculator, TotalsCalculator>();
builder.Services.AddScoped<IQuotationPdfGenerator, QuotationPdfGenerator>();
builder.Services.AddScoped<IInvoicePdfGenerator, InvoicePdfGenerator>();

// Resolve a relative SQLite path against the content root so the database location doesn't depend on
// the directory the API happens to be launched from.
var sqlite = new SqliteConnectionStringBuilder(builder.Configuration.GetConnectionString("Default"));
sqlite.DataSource = Path.Combine(builder.Environment.ContentRootPath, sqlite.DataSource);
Directory.CreateDirectory(Path.GetDirectoryName(sqlite.DataSource)!);

builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite(sqlite.ConnectionString));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<IDocumentStore, DocumentStore>();

var app = builder.Build();

// Create the SQLite database on first run. There are no migrations yet; if the schema changes,
// switch to EF Core migrations rather than editing an existing database by hand.
using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreated();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors(FrontendCorsPolicy);

app.UseAuthorization();

app.MapControllers();

app.Run();
