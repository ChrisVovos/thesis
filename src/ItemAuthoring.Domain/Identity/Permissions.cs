namespace ItemAuthoring.Domain.Identity;

/// <summary>
/// The permissions that gate every use case in the application.
/// </summary>
/// <remarks>
/// Authorization is expressed in terms of permissions rather than roles. A role is a bundle of
/// permissions that an administrator can change at runtime, whereas a use case names the single
/// capability it needs and never has to be redeployed when the bundling changes.
/// </remarks>
public static class Permissions
{
    /// <summary>Read items and item versions.</summary>
    public const string ItemsRead = "items.read";

    /// <summary>Create new draft items.</summary>
    public const string ItemsCreate = "items.create";

    /// <summary>Edit draft items.</summary>
    public const string ItemsUpdate = "items.update";

    /// <summary>Logically delete items.</summary>
    public const string ItemsDelete = "items.delete";

    /// <summary>Submit a draft item for review.</summary>
    public const string ItemsSubmit = "items.submit";

    /// <summary>Approve or return an item that is under review.</summary>
    public const string ItemsReview = "items.review";

    /// <summary>Publish an approved item, or retire a published one.</summary>
    public const string ItemsPublish = "items.publish";

    /// <summary>Read exams.</summary>
    public const string ExamsRead = "exams.read";

    /// <summary>Create exams.</summary>
    public const string ExamsCreate = "exams.create";

    /// <summary>Change the composition of a draft exam.</summary>
    public const string ExamsUpdate = "exams.update";

    /// <summary>Delete exams.</summary>
    public const string ExamsDelete = "exams.delete";

    /// <summary>Publish or archive an exam.</summary>
    public const string ExamsPublish = "exams.publish";

    /// <summary>Manage the category and tag taxonomy.</summary>
    public const string TaxonomyManage = "taxonomy.manage";

    /// <summary>Read the user directory.</summary>
    public const string UsersRead = "users.read";

    /// <summary>Create, update, activate and deactivate users.</summary>
    public const string UsersManage = "users.manage";

    /// <summary>Create roles and change their permission sets.</summary>
    public const string RolesManage = "roles.manage";

    /// <summary>Gets every permission known to the application.</summary>
    public static IReadOnlyList<string> All { get; } =
    [
        ItemsRead, ItemsCreate, ItemsUpdate, ItemsDelete, ItemsSubmit, ItemsReview, ItemsPublish,
        ExamsRead, ExamsCreate, ExamsUpdate, ExamsDelete, ExamsPublish,
        TaxonomyManage, UsersRead, UsersManage, RolesManage,
    ];

    /// <summary>Gets the default permission bundle for each role shipped with the platform.</summary>
    public static IReadOnlyDictionary<string, IReadOnlyList<string>> DefaultsByRole { get; } =
        new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
        {
            [RoleNames.Administrator] = All,
            [RoleNames.Author] =
                [ItemsRead, ItemsCreate, ItemsUpdate, ItemsDelete, ItemsSubmit, ExamsRead, TaxonomyManage],
            [RoleNames.Reviewer] = [ItemsRead, ItemsReview, ItemsPublish, ExamsRead],
            [RoleNames.Instructor] =
                [ItemsRead, ExamsRead, ExamsCreate, ExamsUpdate, ExamsDelete, ExamsPublish],
        };
}
