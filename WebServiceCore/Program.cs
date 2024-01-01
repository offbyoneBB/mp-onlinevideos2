using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using WebServiceCore.Models;
using WebServiceCore.Services;

namespace WebServiceCore
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddRazorPages();

            builder.Services.AddDbContext<OnlineVideosDataContext>(opt =>
               opt.UseSqlite("Data Source=Onlinevideos.db3"));

            builder.Services.AddControllers();
            builder.Services.AddSwaggerGen();
            builder.Services.AddOnlineVideosServices();
            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(opt =>
                {
                    // Add cookie options
                });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            else
            {
                // Seed the database with dummy data for testing
                using (var scope = app.Services.CreateScope())
                    SeedData.Initialize(scope.ServiceProvider);
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllerRoute("default", "{controller}/{action}");
            app.MapRazorPages();

            app.Run();
        }
    }
}
