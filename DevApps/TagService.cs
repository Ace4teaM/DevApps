using System.Text.RegularExpressions;

namespace DevApps
{
    internal static class TagService
    {
        internal static readonly string[] LangagesTags = ["cs", "cpp", "c", "rust"];
        internal static readonly string[] FormatTags = ["grafcet", "csv", "pdf", "json", "yml", "erd", "dbml", "md", "png", "bmp", "jpg", "gif"];
        internal static readonly string[] TypeTags = ["text", "raw", "image", "document", "layout", "form", "canvas"];
        internal static readonly string[] UsageTags = ["codegen", "codemerge", "script"];

        internal static readonly Regex TagFormat = new Regex("^#[A-z0-9]+$");
    }
}
