namespace TvmVr2.Api.Responses
{
    /// <summary>
    /// Result of request validation.
    /// </summary>
    public sealed class ValidationResult
    {
        /// <summary>
        /// Whether validation passed.
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Validation error message.
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>
        /// Creates a valid result.
        /// </summary>
        public static ValidationResult Valid()
        {
            return new ValidationResult
            {
                IsValid = true
            };
        }

        /// <summary>
        /// Creates an invalid result.
        /// </summary>
        public static ValidationResult Invalid(string errorMessage)
        {
            return new ValidationResult
            {
                IsValid = false,
                ErrorMessage = errorMessage ?? string.Empty
            };
        }
    }
}
