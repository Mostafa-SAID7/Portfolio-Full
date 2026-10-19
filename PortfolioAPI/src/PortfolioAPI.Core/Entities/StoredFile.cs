namespace PortfolioAPI.Core.Entities
{
    /// <summary>
    /// An uploaded file (project cover images) kept in the DATABASE.
    ///
    /// Uploads used to live only in one web server's <c>App_Data/uploads</c>
    /// folder while the rows referencing them lived in the shared SQL Server.
    /// The dev API and the production API therefore saw the same rows but not
    /// the same bytes: a cover uploaded through one deployment 404'd on the
    /// other, and every redeploy that replaced the app folder lost the files.
    /// Storing the bytes in the database makes an upload visible to every
    /// instance at once and independent of the file system.
    /// </summary>
    public class StoredFile
    {
        /// <summary>File name including extension, e.g. "d38d4ea5-….png". Primary key.</summary>
        public string FileName { get; set; } = string.Empty;

        /// <summary>Detected content type (image/png, image/jpeg, …).</summary>
        public string ContentType { get; set; } = "application/octet-stream";

        /// <summary>The raw bytes.</summary>
        public byte[] Content { get; set; } = Array.Empty<byte>();

        /// <summary>Size in bytes (denormalised so callers need not read the blob).</summary>
        public long Size { get; set; }

        /// <summary>When the file was stored.</summary>
        public DateTime CreatedAt { get; set; }
    }
}
