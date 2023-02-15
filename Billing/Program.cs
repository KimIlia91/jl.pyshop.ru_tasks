using Billing.Data;
using Billing.Services;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Https;

namespace Billing
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddGrpc();
            builder.Services.AddGrpcReflection();
            builder.Services.AddDbContext<ApplicationDbContext>();
            builder.Services.AddAutoMapper(typeof(MappingConfig));
            builder.WebHost.ConfigureKestrel(options =>
            {
                options.ConfigureHttpsDefaults(o =>
                {
                    //На момент темтирования выключен сертификат TLS 
                    o.ClientCertificateMode = ClientCertificateMode.NoCertificate;
                });
                options.ListenLocalhost(5001, o => o.Protocols = HttpProtocols.Http2);
            });
            var app = builder.Build();

            app.MapGrpcService<BillingService>();

            IWebHostEnvironment env = app.Environment;
            if (env.IsDevelopment())
            {
                app.MapGrpcReflectionService();
            }
            app.Run();
        }
    }
}