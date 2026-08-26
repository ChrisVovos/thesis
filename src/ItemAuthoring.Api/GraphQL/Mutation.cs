using HotChocolate;
using ItemAuthoring.Application.Abstractions.Messaging;
using ItemAuthoring.Application.Exams.Commands;
using ItemAuthoring.Application.Identity.Commands;
using ItemAuthoring.Application.Identity.Dtos;
using ItemAuthoring.Application.Items.Commands;

namespace ItemAuthoring.Api.GraphQL;

/// <summary>
/// The root mutation type.
/// </summary>
/// <remarks>
/// Every field forwards the same command object the corresponding REST endpoint forwards, so the two
/// surfaces execute byte-for-byte the same handler, validators, authorization rules and SQL. The only
/// code that differs between them is the twenty lines of translation in this file and in the
/// controllers.
/// </remarks>
public sealed class Mutation
{
    /// <summary>Creates a draft item.</summary>
    /// <param name="input">The item to create.</param>
    /// <param name="sender">The request dispatcher.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns>The identity of the new item.</returns>
    public async Task<Guid> CreateItem(
        CreateItemCommand input,
        [Service] ISender sender,
        CancellationToken cancellationToken)
        => (await sender.SendAsync(input, cancellationToken)).UnwrapOrThrow();

    /// <summary>Replaces the content of a draft item.</summary>
    /// <param name="input">The new content.</param>
    /// <param name="sender">The request dispatcher.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns><see langword="true"/> on success.</returns>
    public async Task<bool> UpdateItem(
        UpdateItemCommand input,
        [Service] ISender sender,
        CancellationToken cancellationToken)
        => (await sender.SendAsync(input, cancellationToken)).UnwrapOrThrow();

    /// <summary>Logically removes an item from the bank.</summary>
    /// <param name="itemId">The identity of the item.</param>
    /// <param name="sender">The request dispatcher.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns><see langword="true"/> on success.</returns>
    public async Task<bool> DeleteItem(
        Guid itemId,
        [Service] ISender sender,
        CancellationToken cancellationToken)
        => (await sender.SendAsync(new DeleteItemCommand(itemId), cancellationToken)).UnwrapOrThrow();

    /// <summary>Submits a draft item for review.</summary>
    /// <param name="itemId">The identity of the item.</param>
    /// <param name="sender">The request dispatcher.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns><see langword="true"/> on success.</returns>
    public async Task<bool> SubmitItemForReview(
        Guid itemId,
        [Service] ISender sender,
        CancellationToken cancellationToken)
        => (await sender.SendAsync(new SubmitItemForReviewCommand(itemId), cancellationToken))
            .UnwrapOrThrow();

    /// <summary>Approves an item that is under review.</summary>
    /// <param name="itemId">The identity of the item.</param>
    /// <param name="sender">The request dispatcher.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns><see langword="true"/> on success.</returns>
    public async Task<bool> ApproveItem(
        Guid itemId,
        [Service] ISender sender,
        CancellationToken cancellationToken)
        => (await sender.SendAsync(new ApproveItemCommand(itemId), cancellationToken)).UnwrapOrThrow();

    /// <summary>Returns an item to its author.</summary>
    /// <param name="itemId">The identity of the item.</param>
    /// <param name="sender">The request dispatcher.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns><see langword="true"/> on success.</returns>
    public async Task<bool> ReturnItemToDraft(
        Guid itemId,
        [Service] ISender sender,
        CancellationToken cancellationToken)
        => (await sender.SendAsync(new ReturnItemToDraftCommand(itemId), cancellationToken))
            .UnwrapOrThrow();

    /// <summary>Publishes an approved item as a new immutable version.</summary>
    /// <param name="itemId">The identity of the item.</param>
    /// <param name="sender">The request dispatcher.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns><see langword="true"/> on success.</returns>
    public async Task<bool> PublishItem(
        Guid itemId,
        [Service] ISender sender,
        CancellationToken cancellationToken)
        => (await sender.SendAsync(new PublishItemCommand(itemId), cancellationToken)).UnwrapOrThrow();

    /// <summary>Withdraws a published item from further use.</summary>
    /// <param name="itemId">The identity of the item.</param>
    /// <param name="sender">The request dispatcher.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns><see langword="true"/> on success.</returns>
    public async Task<bool> RetireItem(
        Guid itemId,
        [Service] ISender sender,
        CancellationToken cancellationToken)
        => (await sender.SendAsync(new RetireItemCommand(itemId), cancellationToken)).UnwrapOrThrow();

    /// <summary>Creates a category.</summary>
    /// <param name="input">The category to create.</param>
    /// <param name="sender">The request dispatcher.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns>The identity of the new category.</returns>
    public async Task<Guid> CreateCategory(
        CreateCategoryCommand input,
        [Service] ISender sender,
        CancellationToken cancellationToken)
        => (await sender.SendAsync(input, cancellationToken)).UnwrapOrThrow();

    /// <summary>Replaces the details of a category.</summary>
    /// <param name="input">The new details.</param>
    /// <param name="sender">The request dispatcher.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns><see langword="true"/> on success.</returns>
    public async Task<bool> UpdateCategory(
        UpdateCategoryCommand input,
        [Service] ISender sender,
        CancellationToken cancellationToken)
        => (await sender.SendAsync(input, cancellationToken)).UnwrapOrThrow();

    /// <summary>Deletes a category that holds no items.</summary>
    /// <param name="categoryId">The identity of the category.</param>
    /// <param name="sender">The request dispatcher.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns><see langword="true"/> on success.</returns>
    public async Task<bool> DeleteCategory(
        Guid categoryId,
        [Service] ISender sender,
        CancellationToken cancellationToken)
        => (await sender.SendAsync(new DeleteCategoryCommand(categoryId), cancellationToken))
            .UnwrapOrThrow();

    /// <summary>Creates a tag, or returns the existing one when the label is already in use.</summary>
    /// <param name="name">The tag label.</param>
    /// <param name="sender">The request dispatcher.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns>The identity of the tag.</returns>
    public async Task<Guid> CreateTag(
        string name,
        [Service] ISender sender,
        CancellationToken cancellationToken)
        => (await sender.SendAsync(new CreateTagCommand(name), cancellationToken)).UnwrapOrThrow();

    /// <summary>Deletes a tag and detaches it from every item.</summary>
    /// <param name="tagId">The identity of the tag.</param>
    /// <param name="sender">The request dispatcher.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns><see langword="true"/> on success.</returns>
    public async Task<bool> DeleteTag(
        Guid tagId,
        [Service] ISender sender,
        CancellationToken cancellationToken)
        => (await sender.SendAsync(new DeleteTagCommand(tagId), cancellationToken)).UnwrapOrThrow();

    /// <summary>Exchanges credentials for a token pair.</summary>
    /// <param name="input">The credentials.</param>
    /// <param name="sender">The request dispatcher.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns>The issued tokens and the profile of the signed-in user.</returns>
    public async Task<AuthenticationResultDto> Login(
        LoginCommand input,
        [Service] ISender sender,
        CancellationToken cancellationToken)
        => (await sender.SendAsync(input, cancellationToken)).UnwrapOrThrow();

    /// <summary>Exchanges a refresh token for a new token pair.</summary>
    /// <param name="refreshToken">The refresh token held by the client.</param>
    /// <param name="sender">The request dispatcher.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns>The issued tokens and the profile of the signed-in user.</returns>
    public async Task<AuthenticationResultDto> RefreshToken(
        string refreshToken,
        [Service] ISender sender,
        CancellationToken cancellationToken)
        => (await sender.SendAsync(new RefreshTokenCommand(refreshToken), cancellationToken))
            .UnwrapOrThrow();

    /// <summary>Revokes a refresh token, ending the session it belongs to.</summary>
    /// <param name="refreshToken">The refresh token held by the client.</param>
    /// <param name="sender">The request dispatcher.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns><see langword="true"/> on success.</returns>
    public async Task<bool> Logout(
        string refreshToken,
        [Service] ISender sender,
        CancellationToken cancellationToken)
        => (await sender.SendAsync(new LogoutCommand(refreshToken), cancellationToken)).UnwrapOrThrow();
}
