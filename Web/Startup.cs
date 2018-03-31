using System;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;

namespace Web
{
    public class Startup
    {
        private static readonly Regex Uppercase = new Regex("[A-Z]");

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.Use(async (context, next) => 
            {
                if (context.Request.Path.HasValue)
                {
                    if (Uppercase.IsMatch(context.Request.Path))
                    {
                        var response = context.Response;
                        response.StatusCode = StatusCodes.Status301MovedPermanently;
                        response.Headers[HeaderNames.Location] = context.Request.Path.Value.ToLower();
                        return;
                    }
                }

                await next();
            });

            app.UseDefaultFiles();
            app.UseStaticFiles();
        }
    }
}
