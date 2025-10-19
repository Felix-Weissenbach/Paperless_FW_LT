
using Microsoft.EntityFrameworkCore;

namespace Paperless
{
    public class Program // Note: swagger will be available at http://localhost:8081/swagger/index.html
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddDbContext<DAL.PaperlessDbContext>(options =>
                options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))); 
            builder.Services.AddScoped<DAL.IDocumentRepository, DAL.DocumentRepository>();

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            using (var scope = app.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<DAL.PaperlessDbContext>();
                dbContext.Database.Migrate(); // Apply any pending migrations
            }

            // Configure the HTTP request pipeline.
            app.UseSwagger();
            app.UseSwaggerUI();

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            var logDir = Path.Combine(AppContext.BaseDirectory, "Logging"); //ensure logging directory exists
            Directory.CreateDirectory(logDir);

            app.Run();
        }
    }
}
