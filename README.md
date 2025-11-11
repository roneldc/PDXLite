# PDXLite - PDF Text Extractor with AI

A modern ASP.NET Core web application that extracts text from PDF files and uses Google Gemini AI to intelligently structure the data into JSON format.

## Features

- **User Authentication** - JWT-based registration and login system
- **Email Confirmation** - Secure email verification with beautiful templates
- **BCrypt Password Hashing** - Industry-standard password security
- **Strong Password Requirements** - Enforced complexity rules
- **PDF Text Extraction** - Extract text from any PDF document
- **AI-Powered Analysis** - Uses Google Gemini to intelligently structure extracted text
- **SQLite Database** - Store user accounts and extraction history
- **Modern UI** - Beautiful, responsive interface for testing
- **RESTful API** - Full API access for programmatic usage
- **History Tracking** - View and manage past extractions
- **Comprehensive Logging** - Structured logging with Serilog
- **Standardized Error Handling** - Consistent error responses with codes
- **Three-Tier Rate Limiting** - Smart rate limits for different user types
- **Health Checks** - Monitor application and database health

## Prerequisites

- .NET 8.0 SDK or later
- Google Gemini API key (free at https://makersuite.google.com/app/apikey)
- Visual Studio 2022, VS Code, or any C# IDE

## Project Structure

```
PDXLite/
├── Controllers/
│   ├── AuthController.cs       # Authentication endpoints
│   ├── PdfController.cs        # PDF extraction endpoints
│   └── HomeController.cs       # UI controller
├── Data/
│   └── AppDbContext.cs         # Entity Framework context
├── Middleware/
│   ├── ExceptionMiddleware.cs  # Global exception handling
│   └── RequestLoggingMiddleware.cs  # HTTP request/response logging
├── Models/
│   ├── User.cs                 # Data models and DTOs
│   └── ErrorResponse.cs        # Standardized error models
├── Services/
│   ├── AuthService.cs          # Authentication logic
│   ├── PdfService.cs           # PDF text extraction
│   ├── GeminiService.cs        # Gemini AI integration
│   └── RateLimitService.cs     # Rate limiting logic
├── Views/
│   ├── Home/
│   │   └── Index.cshtml        # Main UI
│   └── _ViewImports.cshtml
├── Logs/                       # Application logs (auto-created)
├── Program.cs                  # Application entry point
├── appsettings.json           # Configuration
└── PDXLite.csproj             # Project file
```

## Setup Instructions

### 1. Clone the Repository

```bash
git clone https://github.com/roneldc/PDXLite.git
cd PDXLite
```

### 2. Install Dependencies

```bash
dotnet restore
```

### 3. Configure Settings

Open `appsettings.json` and configure:

**1. Gemini API Key**

```json
"Gemini": {
  "ApiKey": "your-actual-api-key-here"
}
```

**2. Email Settings (Optional for Development)**

```json
"Email": {
  "Enabled": false,  // Set to true for production
  "FromEmail": "noreply@pdxlite.com",
  "AppUrl": "https://localhost:5001",
  "SmtpHost": "smtp.gmail.com",
  "SmtpPort": 587,
  "SmtpUsername": "your-email@gmail.com",
  "SmtpPassword": "your-app-password",
  "UseSsl": true
}
```

For Gmail:

1. Enable 2FA: https://myaccount.google.com/security
2. Generate App Password: https://myaccount.google.com/apppasswords
3. Use the 16-character app password

**3. JWT Secret (Change in Production)**

```json
"Jwt": {
  "Key": "YourSuperSecretKeyHere"
}
```

### 4. Build and Run

```bash
dotnet build
dotnet run
```

The application will start at:

- **UI**: https://localhost:5001 (or http://localhost:5000)
- **API**: https://localhost:5001/swagger
- **Health Check**: https://localhost:5001/health

### 5. Monitor Logs

Logs are automatically written to:

- **Console**: Real-time output
- **Files**: `Logs/pdxlite-YYYY-MM-DD.log`

Watch logs in real-time:

```bash
tail -f Logs/pdxlite-*.log
```

## Usage

### Using the Web Interface

1. **Navigate** to https://localhost:5001
2. **Register** a new account (strong password required: 8+ chars, uppercase, lowercase, number, special char)
3. **Check your email** for confirmation link (or check logs if email is disabled)
4. **Confirm your email** by clicking the link
5. **Login** to access your API key
6. **Upload** a PDF file using the Extract PDF tab
7. **View** the structured JSON output
8. **Check History** to see past extractions

### Password Requirements

Passwords must contain:

- Minimum 8 characters
- At least one uppercase letter (A-Z)
- At least one lowercase letter (a-z)
- At least one number (0-9)
- At least one special character (@$!%\*?&)

**Valid Examples:**

- `MyP@ssw0rd`
- `Secure123!`
- `C0mplex&Pass`

### Using the API

#### 1. Register a User

```bash
curl -X POST https://localhost:5001/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "user@example.com",
    "password": "password123",
    "fullName": "John Doe"
  }'
```

Response:

```json
{
  "token": "eyJhbGciOiJIUzI1NiIs...",
  "email": "user@example.com",
  "fullName": "John Doe"
}
```

#### 2. Login

```bash
curl -X POST https://localhost:5001/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "user@example.com",
    "password": "password123"
  }'
```

#### 3. Extract PDF

```bash
curl -X POST https://localhost:5001/api/pdf/extract \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -F "file=@/path/to/document.pdf"
```

Response:

```json
{
  "success": true,
  "message": "PDF processed successfully",
  "data": {
    "documentType": "Invoice",
    "sections": [...],
    "entities": {...}
  },
  "metadata": {
    "fileName": "document.pdf",
    "fileSize": 245760,
    "pageCount": 3,
    "extractedAt": "2024-11-09T10:30:00Z"
  }
}
```

#### 4. Get Extraction History

```bash
curl -X GET https://localhost:5001/api/pdf/history?page=1&pageSize=10 \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

#### 5. Get Specific Extraction

```bash
curl -X GET https://localhost:5001/api/pdf/history/1 \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

## API Endpoints

### Authentication

- `POST /api/auth/register` - Register new user
- `POST /api/auth/login` - Login user

### PDF Operations (Requires Authentication)

- `POST /api/pdf/extract` - Upload and extract PDF
- `GET /api/pdf/history` - Get extraction history (paginated)
- `GET /api/pdf/history/{id}` - Get specific extraction details

## API Response Headers

All responses include rate limit information:

```http
X-RateLimit-Limit: 100
X-RateLimit-Remaining: 95
X-RateLimit-Reset: 2024-11-11T11:30:00Z
```

## Error Handling

All errors follow a standardized format:

```json
{
  "success": false,
  "error": {
    "code": "ERROR_CODE",
    "message": "Human-readable error message",
    "details": {}
  },
  "timestamp": "2024-11-11T10:30:15Z",
  "path": "/api/pdf/extract"
}
```

Common error codes:

- `RATE_LIMIT_EXCEEDED` - Too many requests
- `FILE_TOO_LARGE` - File exceeds 10MB
- `INVALID_FILE_TYPE` - Not a PDF file
- `INVALID_API_KEY` - Wrong API key
- `UNAUTHORIZED` - Authentication required

## Logging

Logs are written to:

- Console (structured format)
- `Logs/pdxlite-YYYY-MM-DD.log` (rolling daily logs)

Search logs:

```bash
# Find all errors
grep "ERR" Logs/pdxlite-*.log

# Search by request ID
grep "RequestId: abc123" Logs/pdxlite-*.log

# Watch live
tail -f Logs/pdxlite-*.log
```

The application uses SQLite with automatic database creation. The database file `pdxlite.db` will be created in the project root on first run.

### Tables:

- **Users** - User accounts
- **ExtractionHistories** - PDF extraction records

## Configuration

### JWT Settings (`appsettings.json`)

```json
"Jwt": {
  "Key": "YourSuperSecretKeyForPDXLiteApplication2024!",
  "ExpiryInDays": 30
}
```

**Important**: Change the JWT key in production!

### Gemini Settings

```json
"Gemini": {
  "ApiKey": "your-api-key",
  "Model": "gemini-1.5-flash",
  "TimeoutSeconds": 30,
  "MaxRetries": 3
}
```

Available models:

- `gemini-1.5-flash` (recommended - fast and efficient)
- `gemini-1.5-pro` (more capable but slower)

### Rate Limiting Settings

```json
"RateLimiting": {
  "Anonymous": {
    "PermitLimit": 5,
    "WindowMinutes": 60
  },
  "Authenticated": {
    "PermitLimit": 100,
    "WindowMinutes": 60
  },
  "ApiKey": {
    "PermitLimit": 1000,
    "WindowHours": 24
  }
}
```

### File Upload Settings

```json
"FileUpload": {
  "MaxFileSizeBytes": 10485760,
  "AllowedExtensions": [".pdf"]
}
```

## Security Notes

⚠️ **For Production Use:**

1. **Change JWT Secret**: Use a strong, unique key
2. **Enable HTTPS**: Configure proper SSL certificates
3. **Secure API Keys**: Use environment variables or Azure Key Vault
4. **Add Rate Limiting**: Prevent API abuse
5. **Implement CORS**: Restrict allowed origins
6. **Add Input Validation**: Validate file sizes and types
7. **Use Stronger Password Hashing**: Consider bcrypt or Argon2

## Troubleshooting

### Issue: "Gemini API key is not configured"

**Solution**: Make sure you've added your API key in `appsettings.json`

### Issue: Database errors

**Solution**: Delete `pdxlite.db` and restart the application

### Issue: JWT authentication fails

**Solution**: Make sure the JWT key matches in `appsettings.json` and the token is not expired

### Issue: PDF extraction fails

**Solution**: Ensure the PDF is not password-protected and contains extractable text (not scanned images)

### Issue: Email confirmation link not working

**Symptoms**: Click link, nothing happens or page just loads normally

**Debug Steps**:

1. Open browser console (F12) and check for JavaScript errors
2. Verify the URL format is correct: `https://localhost:5001/?token=...`
3. Check if the token is in the URL after clicking
4. Try in a different browser or incognito mode

**Test with Postman**:

```bash
# Extract token from email link and test directly
POST http://localhost:5000/api/auth/confirm-email
Content-Type: application/json

{
  "token": "your-token-from-email"
}
```

**Workaround**: If link doesn't work, manually confirm:

1. Copy the token from the email URL (after `?token=`)
2. Open browser console (F12)
3. Run:

```javascript
fetch("/api/auth/confirm-email", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify({ token: "YOUR_TOKEN_HERE" }),
})
  .then((r) => r.json())
  .then(console.log);
```

### Issue: "I confirmed but still see unverified"

**Solution**: Logout and login again. The confirmation updates the database but not your current session.

### Issue: Can't see API key

**Solution**:

1. Check the badge next to your email (should be green "✓ Verified")
2. If yellow "⚠ Unverified", confirm your email first
3. After confirmation, logout and login again

### Issue: Large files rejected

**Solution**: Increase `FileUpload:MaxFileSizeBytes` in `appsettings.json` (default 10MB)

### Debugging Tips

1. **Check logs**: `Logs/pdxlite-*.log` contains detailed information
2. **Search by RequestId**: Every request has a unique ID for tracing
3. **Use Swagger**: Test API endpoints at `/swagger`
4. **Health check**: Visit `/health` to verify database connectivity

## Extending the Application

### Add Custom PDF Processing

Edit `Services/PdfService.cs` to add custom extraction logic

### Customize JSON Structure

Modify the prompt in `Services/GeminiService.cs` to change how Gemini structures the data

### Add More AI Features

- Document classification
- Entity extraction
- Sentiment analysis
- Translation

## License

MIT License - Feel free to use and modify for your projects!

## Support

For issues and questions:

- Check the Swagger documentation at `/swagger`
- Review the code comments in each file
- Test with the provided UI first before using the API

## Credits

Built with:

- ASP.NET Core 8.0
- Entity Framework Core
- iText7 for PDF processing
- Google Gemini AI
- JWT Authentication

---

**Happy Extracting! 📄✨**