using System;
using System.IO;

namespace SteamPresenceUI.Services
{
    public class CookieValidationService
    {
        private readonly string _cookiePath;
        
        // Return codes
        public enum ValidationState
        {
            Valid,
            Missing,
            Old
        }

        public CookieValidationService(string basePath)
        {
            _cookiePath = Path.Combine(basePath, "cookies.txt");
        }

        /// <summary>
        /// Checks if cookies.txt exists and if it is older than 5 days
        /// </summary>
        public ValidationState ValidateCookies()
        {
            if (!File.Exists(_cookiePath))
                return ValidationState.Missing;

            var lastWrite = File.GetLastWriteTime(_cookiePath);
            var age = DateTime.Now - lastWrite;

            // Arbitrary agentic threshold: warn if > 5 days
            if (age.TotalDays > 5)
            {
                return ValidationState.Old;
            }

            return ValidationState.Valid;
        }

        public string GetCookieAge()
        {
            if (!File.Exists(_cookiePath)) return "N/A";
            
            var lastWrite = File.GetLastWriteTime(_cookiePath);
            var age = DateTime.Now - lastWrite;

            if (age.TotalDays >= 1)
                return $"{(int)age.TotalDays} days ago";
            if (age.TotalHours >= 1)
                return $"{(int)age.TotalHours} hours ago";
            return $"{(int)age.TotalMinutes} mins ago";
        }
    }
}
