namespace TvmVr2.Api.Responses
{
    public sealed class ValidationResult
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;

        public static ValidationResult Valid()
        {
            return new ValidationResult
            {
                IsValid = true
            };
        }

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
