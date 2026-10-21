using System.Text.RegularExpressions;

namespace PortfolioAPI.Infrastructure.Services
{
    /// <summary>
    /// Delete rules for backend-owned local uploads.
    ///
    /// This lives outside <see cref="FileStorageService"/> on purpose: BOTH
    /// storage providers must be able to remove a local file. A project written
    /// before Cloudinary was enabled still points at <c>/api/files/...</c>, and
    /// replacing its cover has to clean the old file up even when Cloudinary is
    /// the active provider.
    ///
    /// Deletion is best-effort and never throws: a missing file, a permissions
    /// problem or a hostile URL must not fail the CRUD operation that triggered
    /// it.
    /// </summary>
    public static class LocalUploadFiles
    {
        /// <summary>
        /// Removes the exact file behind <paramref name="url"/>, plus the
        /// responsive variant that was written alongside it (both always share
        /// the same counter, so a newer replacement is never removed by
        /// mistake).
        ///
        /// A file that this API does not own - an external URL, a
        /// portfolio-bundled path, or anything resolving outside the upload
        /// root - is never touched.
        /// </summary>
        public static void TryDeleteStoredImage(string? url, string storagePath)
        {
            if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(storagePath))
                return;

            var fileName = Path.GetFileName(url);
            if (string.IsNullOrWhiteSpace(fileName))
                return;

            // Path traversal protection: a file name must not carry separators.
            if (fileName.Contains("..", StringComparison.Ordinal) ||
                fileName.Contains(Path.DirectorySeparatorChar) ||
                fileName.Contains(Path.AltDirectorySeparatorChar))
                return;

            var storageRoot = Path.GetFullPath(storagePath);
            var exactPath = Path.GetFullPath(Path.Combine(storageRoot, fileName));
            if (!exactPath.StartsWith(storageRoot, StringComparison.OrdinalIgnoreCase))
                return;

            // 1. The exact file named by the URL.
            try
            {
                if (File.Exists(exactPath))
                    File.Delete(exactPath);
            }
            catch { /* best effort */ }

            // 2. Its sibling from the same upload.
            var (baseSlug, counter) = ParseResponsiveName(Path.GetFileNameWithoutExtension(fileName));

            // A name is only treated as a slug when it looks like one, so the
            // GUID/hash names left behind by the pre-slug era can never match
            // another asset.
            if (baseSlug.Length < 2 || !Regex.IsMatch(baseSlug, @"^[a-z0-9-]+$"))
                return;

            var suffix = counter <= 1 ? string.Empty : $"-{counter}";
            foreach (var candidate in new[]
                     {
                         $"{baseSlug}.webp",
                         $"{baseSlug}-640{suffix}.webp",
                         $"{baseSlug}-1280{suffix}.webp"
                     })
            {
                var candidatePath = Path.GetFullPath(Path.Combine(storageRoot, candidate));
                if (!candidatePath.StartsWith(storageRoot, StringComparison.OrdinalIgnoreCase))
                    continue;

                try { if (File.Exists(candidatePath)) File.Delete(candidatePath); }
                catch { /* best effort */ }
            }
        }

        /// <summary>
        /// Splits <c>{slug}-640.webp</c> / <c>{slug}-1280-2.webp</c> into the
        /// project slug and the upload counter, so the sibling written with it
        /// can be found exactly.
        /// </summary>
        private static (string BaseSlug, int Counter) ParseResponsiveName(string nameWithoutExtension)
        {
            var numbered = Regex.Match(nameWithoutExtension, @"^(.*)-(640|1280)-(\d{1,6})$");
            if (numbered.Success && int.TryParse(numbered.Groups[3].Value, out var numberedCounter) && numberedCounter >= 2)
                return (numbered.Groups[1].Value, numberedCounter);

            var plain = Regex.Match(nameWithoutExtension, @"^(.*)-(640|1280)$");
            if (plain.Success)
                return (plain.Groups[1].Value, 1);

            return (nameWithoutExtension, 1);
        }
    }
}
