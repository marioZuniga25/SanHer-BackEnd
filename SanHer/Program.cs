using SanHer;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSqlServer<SanHerContext>(builder.Configuration.GetConnectionString("cnSanHer"));

builder.Services.AddCors(options =>
{
    options.AddPolicy("NuevaPolitica", app =>
    {
        app.WithOrigins("http://localhost:4200", "http://localhost:5173", "https://reliable-figolla-ec510f.netlify.app")
           .AllowAnyMethod()
           .AllowAnyHeader()
           .AllowCredentials();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("NuevaPolitica");
app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
