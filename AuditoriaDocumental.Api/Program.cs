using Microsoft.EntityFrameworkCore;
using AuditoriaDocumental.Api.Datos;
using Microsoft.AspNetCore.RateLimiting; // para el escudo antispam
using System.Threading.RateLimiting; // para configurar los tiempos del limite

var builder = WebApplication.CreateBuilder(args);

// le decimos al proyecto que vamos a usar controladores para las rutas
builder.Services.AddControllers();

// agregamos los servicios necesarios para que funcione swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// configuracion de la base de datos que hicimos antes
builder.Services.AddDbContext<AppDbContext>(opciones =>
    opciones.UseSqlServer(builder.Configuration.GetConnectionString("ConexionSQL")));

// configuramos el limite de peticiones para no arruinarnos con azure
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("limiteSubida", opt =>
    {
        opt.PermitLimit = 5; // maximo 5 peticiones
        opt.Window = TimeSpan.FromHours(1); // cada hora
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 0; // si se pasan del limite, se rechaza del tiron
    });

    // mensaje personalizado cuando alguien hace spam
    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = 429; // codigo http 429: too many requests
        await context.HttpContext.Response.WriteAsync("alcanzate el limite para subir mas documentos.", token);
    };
});

var app = builder.Build();

// activamos la interfaz web de swagger solo cuando estamos en desarrollo
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// activamos el limitador antispam justo antes de mapear los controladores
app.UseRateLimiter(); 

// activamos el mapeo de los controladores
app.MapControllers();

app.Run();