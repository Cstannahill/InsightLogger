namespace InsightLogger.Application.Abstractions.Privacy;

public interface IPrivacyPolicyProvider
{
    PrivacyPolicy GetCurrentPolicy();
}
