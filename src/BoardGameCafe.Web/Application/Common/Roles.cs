namespace BoardGameCafe.Web.Application.Common;

public static class Roles
{
    public const string Customer = "Customer";
    public const string Employee = "Employee";
    public const string Manager = "Manager";

    public const string Staff = Employee + "," + Manager;

    public static readonly string[] All = [Customer, Employee, Manager];
}
