using ArtemisBanking.Infrastructure.Persistence;
using ArtemisBanking.Infrastructure.Persistence.DependencyInjection;
using ArtemisBanking.Infrastructure.Identity.DependencyInjection;
using ArtemisBanking.Infrastructure.Identity;
using ArtemisBanking.Infrastructure.Shared.DependencyInjection;
using AutoMapper;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddPersistenceInfrastructure(builder.Configuration);
builder.Services.AddIdentityInfrastructure(builder.Configuration);
builder.Services.AddSharedInfrastructure(builder.Configuration);

builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ArtemisBankingDbContext>();
    db.Database.EnsureCreated();
}

await IdentitySeeder.SeedDefaultUsersAndRolesAsync(app.Services);

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
