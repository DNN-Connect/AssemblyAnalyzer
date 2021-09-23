namespace Connect.AssemblyAnalyzer
{
   public static class Extensions
    {
        public static string ToFullSourcePath(this string input, string projectCodePathIdentifier, string codePath)
        {
            var relPath = input.Substring(input.IndexOf(projectCodePathIdentifier) + projectCodePathIdentifier.Length);
            return codePath + relPath;
        }

        public static string EnsureEndsWith(this string input, string endWith)
        {
            if (!input.EndsWith(endWith)) return input + endWith;
            return input;
        }
    }
}
