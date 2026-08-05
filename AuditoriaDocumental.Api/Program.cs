using Microsoft.EntityFrameworkCore;
using AuditoriaDocumental.Api.Datos;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.RateLimiting; // para el escudo antispam
using System.Threading.RateLimiting; // para configurar los tiempos del limite
using AuditoriaDocumental.Api.Servicios; // anadimos esto para que reconozca la clase de la ia

var builder = WebApplication.CreateBuilder(args);

// le decimos al proyecto que vamos a usar controladores para las rutas
// y configuramos el serializador de json para que ignore los bucles infinitos
builder.Services.AddControllers().AddJsonOptions(opciones =>
{
    opciones.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
});

// quitamos el bloqueo cors para que angular pueda conectarse
builder.Services.AddCors(opciones =>
{
    opciones.AddPolicy("PermitirAngular", policy =>
    {
        policy.WithOrigins("http://localhost:4200") // ruta de angular
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// agregamos los servicios necesarios para que funcione swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// configuracion de la base de datos
builder.Services.AddDbContext<AppDbContext>(opciones =>
    opciones.UseSqlServer(builder.Configuration.GetConnectionString("ConexionSQL")));

// registramos el cerebro de la ia para que el controlador pueda usarlo
builder.Services.AddScoped<ServicioExtraccionIA>();

// configuramos el limite de peticiones
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
        context.HttpContext.Response.StatusCode = 429; // codigo http 429
        await context.HttpContext.Response.WriteAsync("alcanzaste el limite para subir mas documentos.", token);
    };
});

var app = builder.Build();

// activamos la interfaz web de swagger en desarrollo
// quito el if para probas, ya que al desplegar azure lo quita

    app.UseSwagger();
    app.UseSwaggerUI();


// aplicamos el permiso de cors (debe ir antes del rate limiter y los controladores)
app.UseCors("PermitirAngular");

// activamos el limitador antispam
app.UseRateLimiter();

// activamos el mapeo de los controladores
app.MapControllers();

app.Run();