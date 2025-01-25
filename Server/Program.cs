using Microsoft.Extensions.FileProviders;
using System.IO;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddGrpc();
builder.Services.AddCors();

var app = builder.Build();

app.MapGrpcService<DetectionServiceImpl>();

app.UseCors(policy =>
    policy.AllowAnyOrigin()
          .AllowAnyMethod()
          .AllowAnyHeader());


app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/hls"))
    {
        context.Response.Headers.Append("Access-Control-Allow-Origin", "*");
    }
    await next();
});

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), "hls")),
    RequestPath = "/hls",
    ServeUnknownFileTypes = true, 
    DefaultContentType = "application/x-mpegURL"
});
app.Urls.Add("http://localhost:50051");
app.Urls.Add("http://localhost:5000");
app.UseRouting();

app.Run();

