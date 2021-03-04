using System;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ScrubbyWeb.Services;
using Terryberry.DataProtection.MongoDb;

namespace ScrubbyWeb
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public static void ConfigureServices(IServiceCollection services)
        {
            services.AddDistributedMemoryCache();

            services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(5);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);

            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/login";
                    options.Cookie.Name = "Scrubby";
                    options.Cookie.Expiration = TimeSpan.FromDays(7);
                    options.SlidingExpiration = true;
                    options.Cookie.IsEssential = true;
                })
                .AddAPIKeySupport(options => { });

            services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = "/login";
                options.Cookie.Name = "Scrubby";
                options.Cookie.Expiration = TimeSpan.FromDays(7);
                options.SlidingExpiration = true;
                options.Cookie.IsEssential = true;
            });

            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            services.AddSingleton<MongoAccess>();
            services.AddSingleton<PlayerService>();
            services.AddSingleton<SuicideService>();
            services.AddSingleton<RoundService>();
            services.AddSingleton<ConnectionService>();
            services.AddSingleton<RuntimeService>();
            services.AddSingleton<LogMessageService>();
            services.AddSingleton<AnnouncementService>();

            var provider = services.BuildServiceProvider();

            services.AddDataProtection()
                .SetApplicationName("ScrubbyWeb")
                .SetDefaultKeyLifetime(TimeSpan.FromDays(7))
                .PersistKeysToMongoDb(provider.GetService<MongoAccess>().DB, "scrubby_dataprotection")
                .AddKeyCleanup();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public static void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
                app.UseDeveloperExceptionPage();
            else
                app.UseHsts();

            app.UseStatusCodePagesWithRedirects("/error/{0}");
            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseSession();
            app.UseCookiePolicy();
            app.UseAuthentication();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    "default",
                    "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}