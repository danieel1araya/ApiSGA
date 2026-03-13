using ApiSGA.Data;
using ApiSGA.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddScoped<BitacoraService>();


// 1) Agregar DbContext con SQL Server
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2) CONFIGURAR CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFront", policy =>
    {
        // Para pruebas puedes dejar AllowAnyOrigin.
        // Luego lo podés cambiar a WithOrigins("http://localhost:5173", "http://localhost:3000", "https://tu-front.com")
        policy
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// 3) ACTIVAR CORS ANTES DE MapControllers
app.UseCors("AllowFront");

app.UseAuthorization();

app.MapControllers();

app.Run();
