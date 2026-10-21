using Microsoft.Extensions.FileProviders;
using PortfolioAPI.Infrastructure.Services;

namespace PortfolioAPI.Pipelines
{
    /// <summary>
    /// Static serving of locally-uploaded files — extracted from Program.cs.
    /// Only used when the active provider stores on local disk; harmless on
    /// Cloudinary (the folder just has no local files).
    /// </summary>
    public static class UploadsPipeline
    {
        /// <summary>
        /// Serve uploaded files (UploadController) at {FileStorage:BaseUrl} —
        /// /api/files by default. Both frontends reach this path through their
        /// own /api proxy, so stored cover URLs stay relative in the database
        /// and load over HTTPS on every frontend without any hardcoded API host.
        /// </summary>
        public static IApplicationBuilder UseLocalUploads(this IApplicationBuilder app)
        {
            var configuration = app.ApplicationServices.GetRequiredService<IConfiguration>();
            var uploadsRoot = UploadStorage.ResolveRoot(configuration);

            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(uploadsRoot),
                RequestPath = UploadStorage.PublicBase(configuration),
                // Uploads use random (guid) filenames, so they are immutable — cache hard.
                OnPrepareResponse = ctx => ctx.Context.Response.Headers.CacheControl = "public, max-age=31536000, immutable"
            });

            return app;
        }
    }
}
