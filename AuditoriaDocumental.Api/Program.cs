using Microsoft.EntityFrameworkCore;
using AuditoriaDocumental.Api.Datos;

var builder = WebApplication.CreateBuilder(args);

// le decimos al proyecto que vamos a usar controladores para las rutas
builder.Services.AddControllers();

// agregamos los servicios necesarios para que funcione swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// configuracion de la base de datos que hicimos antes
builder.Services.AddDbContext<AppDbContext>(opciones =>
    opciones.UseSqlServer(builder.Configuration.GetConnectionString("ConexionSQL")));

var app = builder.Build();

// activamos la interfaz web de swagger solo cuando estamos en desarrollo
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// activamos el mapeo de los controladores
app.MapControllers();

app.Run();