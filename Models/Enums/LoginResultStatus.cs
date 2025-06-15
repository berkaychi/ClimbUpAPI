namespace ClimbUpAPI.Models.Enums
{
    public enum LoginResultStatus
    {
        Success,
        InvalidCredentials,
        EmailNotConfirmed,
        UserNotFound,
        Lockout
    }
}
