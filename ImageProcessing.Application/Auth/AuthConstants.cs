namespace ImageProcessing.Application.Auth;

public static class Roles
{
    public const string Admin = "Admin";
    public const string User = "User";
}

public static class Policies
{
    public const string CanRead = "CanRead";
    public const string CanCreate = "CanCreate";
    public const string CanDelete= "CanDelete";


    public const string CanReadUsers = "CanReadUsers";
    public const string CanCreateUser = "CanCreateUser";
    public const string CanDeleteUser = "CanDeleteUser";
}
