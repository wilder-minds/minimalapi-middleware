#region Wiring Up
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

var builder = WebApplication.CreateBuilder(args);

bool isTesting = builder.Configuration.GetValue<bool>("IsTesting", true);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSingleton<BechdelDataService>();

builder.Services.AddResponseCaching();
builder.Services.AddResponseCompression();
builder.Services.AddRequestLocalization(cfg => {
  cfg.DefaultRequestCulture = new RequestCulture("us-en");
});
builder.Services.AddAuthentication(cfg =>
{
  cfg.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
  cfg.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
  .AddJwtBearer();
builder.Services.AddAuthorization();

builder.Services.AddCors(cfg =>
{
  cfg.AddDefaultPolicy(cfg =>
  {
    cfg.WithMethods("GET");
    cfg.AllowAnyHeader();
    cfg.AllowAnyOrigin();
  });

  cfg.AddPolicy("partners", cfg =>
  {
    cfg.WithOrigins("https://somepartnername.com");
    cfg.AllowAnyMethod();
  });
});

builder.Services.AddSwaggerGen(setup =>
{
  if (!isTesting)
  {
    var path = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, @"BechdelDataServer.xml"));
    setup.IncludeXmlComments(path);
  }
  setup.SwaggerDoc("v1", new OpenApiInfo()
  {
    Description = "Bechdel Test API using data from FiveThirtyEight.com",
    Title = "Bechdel Test API",
    Version = "v1"
  });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
  app.UseDeveloperExceptionPage();
}

app.UseCors();

app.UseResponseCaching();
app.UseResponseCompression();
app.UseRequestLocalization();
app.UseSwagger();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("api/films",
  [ResponseCache(Duration = 5000)]
async (BechdelDataService ds,
         int? page,
         int? pageSize) =>
  {
    FilmResult data = await ds.LoadAllFilmsAsync();
    if (data.Results is null)
    {
      return Results.NotFound();
    }
    return Results.Ok(data);
  });
  //.RequireAuthorization();

app.MapGet("api/films/{year:int}",
  async (BechdelDataService ds,
         int? page,
         int? pageSize,
         int year) =>
{
  FilmResult data =
    await ds.LoadAllFilmsByYearAsync(year);
  if (data.Results is null)
  {
    return Results.NotFound();
  }
  return Results.Ok(data);
}).Produces<FilmResult>(200, "application/json")
  .ProducesProblem(404)
  .WithName("GetAllFilms")
  .WithTags("films");

app.MapGet("api/years",
  async (BechdelDataService ds) =>
  {
    var data = await ds.LoadFilmYears();
    if (data is null)
    {
      return Results.NotFound();
    }
    return Results.Ok(data);
  }).AllowAnonymous();
    //.RequireHost("localhost");

app.UseSwaggerUI(c =>
{
  c.SwaggerEndpoint("/swagger/v1/swagger.json", "Bechdel Test Results v1");
  c.RoutePrefix = string.Empty;
});

app.Run();

return 0;
#endregion



// To enable access to the Top Level Class
public partial class Program { }
