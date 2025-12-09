namespace MemosToBarkOrTelegram.Models
{
    public class BarkOptions
    {
        public const string SectionName = "Bark";

        public bool Enabled { get; set; }
        public string ServerUrl { get; set; } = "https://api.day.app";
        
        /// <summary>
        /// User ID to device keys mapping
        /// Format: { "1": ["device-key-1"], "2": ["device-key-2", "device-key-3"] }
        /// Use "*" as key for global devices that receive all notifications
        /// </summary>
        public Dictionary<string, string[]>? DeviceKeys { get; set; }
        
        public string? Icon { get; set; }
        public string? Group { get; set; }
        public string? Sound { get; set; }

        public AuthOptions? Auth { get; set; }

        /// <summary>
        /// Get device keys for a specific user, excluding their own devices
        /// </summary>
        /// <param name="creatorUserId">The user ID who created the memo</param>
        /// <returns>List of device keys that should receive the notification</returns>
        public List<string> GetDeviceKeysForUser(string creatorUserId)
        {
            var result = new List<string>();

            if (DeviceKeys == null || DeviceKeys.Count == 0)
                return result;

            // Add global devices (for everyone)
            if (DeviceKeys.TryGetValue("*", out var globalDevices))
            {
                result.AddRange(globalDevices);
            }

            // Add devices for all users except the creator
            foreach (var kvp in DeviceKeys.Where(kvp => kvp.Key != "*" && kvp.Key != creatorUserId))
            {
                result.AddRange(kvp.Value);
            }

            return result;
        }
    }

    public class AuthOptions
    {
        // public const string SectionName = "Auth";
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}