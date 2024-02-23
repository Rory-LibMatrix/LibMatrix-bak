using System.Net.Mime;
using LibMatrix;
using LibMatrix.HomeserverEmulator.Services;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c => {
    c.SwaggerDoc("v1", new OpenApiInfo() {
        Version = "v1",
        Title = "Rory&::LibMatrix.HomeserverEmulator",
        Description = "Partial Matrix implementation"
    });
    c.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "LibMatrix.HomeserverEmulator.xml"));
});
builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
builder.Services.AddSingleton<UserStore>();
builder.Services.AddSingleton<RoomStore>();


builder.Services.AddScoped<TokenService>();

builder.Services.AddRequestTimeouts(x => {
    x.DefaultPolicy = new RequestTimeoutPolicy {
        Timeout = TimeSpan.FromMinutes(10),
        WriteTimeoutResponse = async context => {
            context.Response.StatusCode = 504;
            context.Response.ContentType = "application/json";
            await context.Response.StartAsync();
            await context.Response.WriteAsJsonAsync(new MatrixException() {
                ErrorCode = "M_TIMEOUT",
                Error = "Request timed out"
            }.GetAsJson());
            await context.Response.CompleteAsync();
        }
    };
});
builder.Services.AddCors(options => {
    options.AddPolicy(
        "Open",
        policy => policy.AllowAnyOrigin().AllowAnyHeader());
});
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment() || true)
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("Open");
app.UseExceptionHandler(exceptionHandlerApp => {
    exceptionHandlerApp.Run(async context => {

        var exceptionHandlerPathFeature =
            context.Features.Get<IExceptionHandlerPathFeature>();

        if (exceptionHandlerPathFeature?.Error is MatrixException mxe) {
            context.Response.StatusCode = mxe.ErrorCode switch {
                "M_NOT_FOUND" => StatusCodes.Status404NotFound,
                "M_UNAUTHORIZED" => StatusCodes.Status401Unauthorized,
                _ => StatusCodes.Status500InternalServerError
            };
            context.Response.ContentType = MediaTypeNames.Application.Json;
            await context.Response.WriteAsync(mxe.GetAsJson()!);
        }
        else {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = MediaTypeNames.Application.Json;
            await context.Response.WriteAsync(new MatrixException() {
                ErrorCode = "M_UNKNOWN",
                Error = exceptionHandlerPathFeature?.Error.ToString()
            }.GetAsJson());
        }
    });
});

app.UseAuthorization();

app.MapControllers();

app.Run();
