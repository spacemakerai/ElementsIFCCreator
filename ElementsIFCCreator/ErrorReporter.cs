using FormaAPI;

namespace ElementsIFCCreator
{
    public class ErrorReporter : IErrorReporter
    {
        public Action<string> LogErrorMethod { get; }
        public Action<string> LogMethod { get; }

        public ErrorReporter(Action<string> LogErrorMethod, Action<string> LogMethod)
        {
            this.LogErrorMethod = LogErrorMethod;
            this.LogMethod = LogMethod;
        }

        public string CaptureErrorMessage(string message, Severity level = Severity.Error)
        {
            string errorMessage = FormatErrorMessageWithSeverityLevel(message, level);
            this.LogErrorMethod(errorMessage);
            return errorMessage;
        }

        public string CaptureException(Exception exception)
        {
            this.LogErrorMethod(exception.Message);
            throw exception;
        }

        public void RecordBreadcrumb(string bc)
        {
            this.LogMethod(bc);
        }

        public void ReportErrorToUser(Exception exception, string userMessage)
        {
            this.LogErrorMethod(exception.Message);
            throw new Exception(userMessage);
        }

        public void ReportErrorToUser(APIResponse response, string userMessage, Severity level = Severity.Error)
        {
            this.LogErrorMethod(response.Content);
            string errorMessage = FormatErrorMessageWithSeverityLevel(userMessage, level);
            this.LogErrorMethod(errorMessage);
        }

        public void ReportErrorToUser(string internalMessage, string userMessage, Severity level = Severity.Error)
        {
            this.LogErrorMethod(FormatErrorMessageWithSeverityLevel(internalMessage, level));
            throw new Exception(userMessage);
        }

        public void ShowErrorToUser(string message)
        {
            throw new Exception(message);
        }

        private string FormatErrorMessageWithSeverityLevel(string message, Severity level)
        {
            return $"Level: {level.ToString()}\nError Message: {message}";
        }
    }
}
