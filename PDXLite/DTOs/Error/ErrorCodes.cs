namespace PDXLite.DTOs.Error
{
    public class ErrorCodes
    {
        // Authentication & Authorization
        public const string UNAUTHORIZED = "UNAUTHORIZED";
        public const string INVALID_CREDENTIALS = "INVALID_CREDENTIALS";
        public const string INVALID_TOKEN = "INVALID_TOKEN";
        public const string INVALID_API_KEY = "INVALID_API_KEY";
        public const string ACCOUNT_INACTIVE = "ACCOUNT_INACTIVE";

        // Validation
        public const string VALIDATION_ERROR = "VALIDATION_ERROR";
        public const string INVALID_INPUT = "INVALID_INPUT";
        public const string FILE_TOO_LARGE = "FILE_TOO_LARGE";
        public const string INVALID_FILE_TYPE = "INVALID_FILE_TYPE";
        public const string FILE_REQUIRED = "FILE_REQUIRED";

        // Business Logic
        public const string USER_ALREADY_EXISTS = "USER_ALREADY_EXISTS";
        public const string RESOURCE_NOT_FOUND = "RESOURCE_NOT_FOUND";
        public const string PDF_EXTRACTION_FAILED = "PDF_EXTRACTION_FAILED";
        public const string AI_PROCESSING_FAILED = "AI_PROCESSING_FAILED";
        public const string EMPTY_PDF = "EMPTY_PDF";

        // Rate Limiting
        public const string RATE_LIMIT_EXCEEDED = "RATE_LIMIT_EXCEEDED";

        // System
        public const string INTERNAL_ERROR = "INTERNAL_ERROR";
        public const string SERVICE_UNAVAILABLE = "SERVICE_UNAVAILABLE";
        public const string CONFIGURATION_ERROR = "CONFIGURATION_ERROR";
    }
}
