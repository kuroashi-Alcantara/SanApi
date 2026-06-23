using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SanApi.Datos;
using System.Text;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// --- CONFIGURACIÓN DE JWT ---
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });
// -----------------------------

// Registro del contexto
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddControllers();

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    // Configuración del botón de Autorización (JWT)
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Autenticación JWT usando el esquema Bearer. \r\n\r\n Escribe 'Bearer' [espacio] y luego tu token en la caja de texto.\r\n\r\nEjemplo: \"Bearer eyJhbGci...\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// 1. REGISTRAR CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("PoliticaDesarrollo", app =>
    {
        app.AllowAnyOrigin()  // Permite cualquier dominio (localhost:3000, localhost:4200, etc)
           .AllowAnyHeader()  // Permite cualquier cabecera (Authorization, Content-Type, etc)
           .AllowAnyMethod(); // Permite cualquier método (GET, POST, PUT, DELETE)
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//  LÍNEA PARA SERVIR IMÁGENES
app.UseStaticFiles();

// Solo aplica la redirección HTTPS si la aplicación NO está en desarrollo local
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// 2. ACTIVAR CORS 
app.UseCors("PoliticaDesarrollo");

app.UseAuthentication(); // 1. Primero verifica quién eres 
app.UseAuthorization();  // 2. Luego verifica qué puedes hacer 


app.MapControllers();

app.Run();
