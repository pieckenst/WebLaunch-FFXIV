namespace El_Garnan_Plugin_Loader.Models
{
    /// <summary>
    /// Credentials model for game authentication
    /// </summary>
    public class GameCredentials
    {
        /// <summary>
        /// User login name/email
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// User password
        /// </summary> 
        public string Password { get; set; }

        /// <summary>
        /// One-time password if enabled
        /// </summary>
        public string OTP { get; set; }

        /// <summary>
        /// Authentication token if applicable
        /// </summary>
        public string Token { get; set; }

        /// <summary>
        /// Whether credentials are valid
        /// </summary>
        public bool IsValid => !string.IsNullOrEmpty(Username) && !string.IsNullOrEmpty(Password);
    }
}
