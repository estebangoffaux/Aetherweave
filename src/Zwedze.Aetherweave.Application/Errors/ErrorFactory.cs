using System.Collections.Immutable;
using System.Text;
using JetBrains.Annotations;
using Zwedze.Aetherweave.SharedKernel;

namespace Zwedze.Aetherweave.Application.Errors;

public static class ErrorFactory
{
    [UsedImplicitly]
    public static Error Create(ErrorCode code, string message)
    {
        return new Error
        {
            Codes = [code],
            Message = message
        };
    }

    [UsedImplicitly]
    public static Error Create(Exception ex)
    {
        return ex switch
        {
            BusinessException bzEx => Create(bzEx),
            AggregateException aggEx => Create(aggEx),
            _ => new Error
            {
                Codes = [ErrorCode.UnmanagedErrorCode],
                Message = "An internal server error occurred. Please contact support."
            }
        };
    }

    private static Error Create(AggregateException ex)
    {
        List<ErrorCode> errorCodes = new();
        StringBuilder messageBuilder = new();

        var businessExceptions = ex
            .InnerExceptions
            .OfType<BusinessException>()
            .ToImmutableArray();
        if (businessExceptions.Length != 0)
        {
            var (codes, message) = ExtractCodesAndMessage(businessExceptions);
            errorCodes.AddRange(codes);
            messageBuilder.Append(message);
        }

        var otherExceptions = ex.InnerExceptions.Except(businessExceptions).ToArray();

        if (otherExceptions.Length > 0)
        {
            errorCodes.Add(ErrorCode.UnmanagedErrorCode);
        }

        return new Error
        {
            Codes = [..errorCodes],
            Message = messageBuilder.ToString()
        };
    }

    private static Error Create(BusinessException ex)
    {
        return new Error
        {
            Codes = [ex.ErrorCode],
            Message = ex.Message
        };
    }

    private static (IReadOnlyCollection<ErrorCode> ErrorCodes, string Message) ExtractCodesAndMessage(IReadOnlyCollection<BusinessException> exceptions)
    {
        var errorCodes = exceptions.Select(x => x.ErrorCode).ToImmutableArray();
        var message = exceptions.Select(x => x.Message).Aggregate((x, y) => x + Environment.NewLine + y);

        return (errorCodes, message);
    }
}
