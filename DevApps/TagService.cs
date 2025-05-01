using System.Text.RegularExpressions;

namespace DevApps
{
    internal static class TagService
    {
        internal static string[] LangagesTags = { "cs", "cpp", "c", "rust" };
        internal static string[] FormatTags = { "grafcet", "csv", "pdf", "json", "yml", "erd", "dbml", "md", "png", "bmp", "jpg", "gif" };
        internal static string[] TypeTags = { "text", "raw", "image", "document", "layout", "form", "canvas" };
        internal static string[] UsageTags = { "codegen", "codemerge", "script" };

        internal static Regex TagFormat = new Regex("^#[A-z0-9]+$");
    }
}
