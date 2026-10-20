using Microsoft.EntityFrameworkCore;
using VSG.MarketData.Infrastructure.Binance;
using VSG.MarketData.Infrastructure.Persistence;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers().AddXmlSerializerFormatters();

builder.Services.AddOpenApi();

builder.Services.AddDbContext<MarketDataDbContext>(
    options => options.UseSqlite(builder.Configuration.GetConnectionString("MarketData")
                                 ?? "Data Source=marketdata.db"));

builder.Services.AddHostedService<BinanceTradeStreamService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider
        .GetRequiredService<MarketDataDbContext>();

    db.Database.Migrate();
}

app.Run();
