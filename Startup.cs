using System;
using System.Collections.Generic;
using System.Net;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ScrubbyWeb.Security;
using ScrubbyWeb.Services;
using ScrubbyWeb.Services.Interfaces;
using ScrubbyWeb.Services.Mongo;
using ScrubbyWeb.Services.SQL;
using Tgstation.Auth;

namespace ScrubbyWeb
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDistributedMemoryCache();

            services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(5);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

            services.AddMvc().AddRazorRuntimeCompilation().AddNewtonsoftJson();

            // Forward along data from docker
            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                options.KnownNetworks.Add(new IPNetwork(IPAddress.Parse("::ffff:172.18.0.0"), 16));
            });

            services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = TgDefaults.AuthenticationScheme;
                })
                .AddCookie(options =>
                {
                    options.LoginPath = "/login";
                    options.Cookie.Name = "Scrubby";
                    options.ExpireTimeSpan = TimeSpan.FromDays(7);
                    options.SlidingExpiration = true;
                    options.Cookie.IsEssential = true;
                })
                .AddTgstation(options =>
                {
                    var tgAuthSection = Configuration.GetSection("Authentication:Tgstation");
                    options.ClientId = tgAuthSection["ClientId"];
                    options.ClientSecret = tgAuthSection["ClientSecret"];
                    options.CallbackPath = "/auth";
                    options.Scope.Add("user.groups");
                });

            services.AddSingleton<MongoAccess>();
            services.AddTransient<IPlayerService, SqlPlayerService>();
            services.AddSingleton<ISuicideService, SqlSuicideService>();
            services.AddSingleton<IRoundService, SqlRoundService>();
            services.AddTransient<IConnectionService, SqlConnectionService>();
            services.AddSingleton<IRuntimeService, SqlRuntimeService>();
            services.AddSingleton<IAnnouncementService, SqlAnnouncementService>();
            services.AddTransient<ICKeyService, SqlCKeyService>();
            services.AddTransient<IUserService, MongoUserService>();
            services.AddTransient<INewscasterService, MongoNewscasterService>();
            services.AddTransient<IScrubbyService, SqlScrubbyService>();
            services.AddTransient<IFileService, SqlFileService>();
            services.AddTransient<IIconService, MongoIconService>();
            services.AddTransient<BYONDDataService>();
            services.AddSingleton<IClaimsTransformation, ScrubbyUserClaimsTransformation>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILogger<Startup> logger)
        {
            app.UseForwardedHeaders();

            if (env.IsDevelopment())
                app.UseDeveloperExceptionPage();
            else
                app.UseHsts();

            app.UseStatusCodePagesWithReExecute("/error/{0}");
            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseSession();
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}