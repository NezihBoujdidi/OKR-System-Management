namespace NXM.Tensai.Back.OKR.Application.Common.Exceptions;

public class ValidationException : Exception
{
    public IDictionary<string, string[]> Errors { get; }

    public ValidationException() : base("One or more validation failures have occurred.")
    {
        Errors = new Dictionary<string, string[]>();
    }

    public ValidationException(IDictionary<string, string[]> validationErrors) : base("One or more validation failures have occurred.")
    {
        Errors = validationErrors;
    }

    public ValidationException(string message) : base(message)
    {
        Errors = new Dictionary<string, string[]>
        {
            { "Error", new[] { message } }
        };
    }    public ValidationException(IEnumerable<(string PropertyName, string ErrorMessage)> failures) : this()
    {
        var failureGroups = failures
            .GroupBy(e => e.PropertyName, e => e.ErrorMessage)
            .ToDictionary(g => g.Key, g => g.ToArray());
        
        Errors = failureGroups;

    }
} 