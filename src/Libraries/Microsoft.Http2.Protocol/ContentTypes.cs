namespace SharedProtocol
{
    using System.IO;

    /// <summary>
    /// Content types helper class
    /// </summary>
    public static class ContentTypes
    {
        /// <summary>
        /// Plain text definition
        /// </summary>
        public const string TextPlain = "text/plain";

        /// <summary>
        /// CSS text definition
        /// </summary>
        public const string TextCss = "text/css";

        /// <summary>
        /// HTML text definition
        /// </summary>
        public const string TextHtml = "text/html";

        /// <summary>
        /// Script text definition
        /// </summary>
        public const string TextScript = "text/script";

        /// <summary>
        /// Returns file type from file extension.
        /// </summary>
        /// <param name="name">File extension name.</param>
        /// <returns>File type.</returns>
        public static string GetTypeFromFileName(string name)
        {
            switch (Path.GetExtension(name))
            {
                case ".html":
                case ".htm":
                    return TextHtml;
                case ".js":
                    return TextScript;
                case ".css":
                    return TextCss;
                default:
                    return TextPlain;
            }
        }
    }
}
