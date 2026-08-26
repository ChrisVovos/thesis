using Asp.Versioning;
using ItemAuthoring.Api.Rest.Contracts;
using ItemAuthoring.Application.Abstractions.Messaging;
using ItemAuthoring.Application.Common;
using ItemAuthoring.Application.Items.Commands;
using ItemAuthoring.Application.Items.Dtos;
using ItemAuthoring.Application.Items.Queries;
using Microsoft.AspNetCore.Mvc;

namespace ItemAuthoring.Api.Rest.Controllers;

/// <summary>
/// The item bank.
/// </summary>
/// <param name="sender">The request dispatcher.</param>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/items")]
public sealed class ItemsController(ISender sender) : ApiControllerBase(sender)
{
    /// <summary>Searches, filters, sorts and pages the item bank.</summary>
    /// <param name="request">The query string parameters.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns>One page of item summaries together with paging metadata.</returns>
    [HttpGet(Name = nameof(SearchItems))]
    [ProducesResponseType(typeof(PagedResult<ItemSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchItems(
        [FromQuery] ItemSearchRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var result = await Sender.SendAsync(
            new SearchItemsQuery(request.ToCriteria()),
            cancellationToken);
        return Respond(result);
    }

    /// <summary>Reads a single item.</summary>
    /// <param name="id">The identity of the item.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns>The full projection of the item.</returns>
    [HttpGet("{id:guid}", Name = nameof(GetItem))]
    [ProducesResponseType(typeof(ItemDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status304NotModified)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetItem(Guid id, CancellationToken cancellationToken)
    {
        var result = await Sender.SendAsync(new GetItemByIdQuery(id), cancellationToken);
        if (result.IsFailure)
        {
            return Problem(result.Error);
        }

        var summary = result.Value.Summary;
        var versionToken =
            $"{summary.Id}:{summary.VersionNumber}:{summary.Status}:{summary.LastModifiedAtUtc:O}";
        return RespondWithEntityTag(result, versionToken, TimeSpan.FromSeconds(30));
    }

    /// <summary>Reads the published versions of an item, newest first.</summary>
    /// <param name="id">The identity of the item.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns>The published versions.</returns>
    [HttpGet("{id:guid}/versions", Name = nameof(GetItemVersions))]
    [ProducesResponseType(typeof(IReadOnlyList<ItemVersionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetItemVersions(Guid id, CancellationToken cancellationToken)
        => Respond(await Sender.SendAsync(new GetItemVersionsQuery(id), cancellationToken));

    /// <summary>Creates a draft item.</summary>
    /// <param name="command">The item to create.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns>The identity of the new item.</returns>
    [HttpPost(Name = nameof(CreateItem))]
    [ProducesResponseType(typeof(CreatedResourceResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateItem(
        [FromBody] CreateItemCommand command,
        CancellationToken cancellationToken)
    {
        var result = await Sender.SendAsync(command, cancellationToken);
        return RespondCreated(result, nameof(GetItem), new { id = result.IsSuccess ? result.Value : Guid.Empty });
    }

    /// <summary>Replaces the content of a draft item.</summary>
    /// <param name="id">The identity of the item.</param>
    /// <param name="command">The new content.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns>No content on success.</returns>
    [HttpPut("{id:guid}", Name = nameof(UpdateItem))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateItem(
        Guid id,
        [FromBody] UpdateItemCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        return Respond(await Sender.SendAsync(command with { ItemId = id }, cancellationToken));
    }

    /// <summary>Logically removes an item from the bank.</summary>
    /// <param name="id">The identity of the item.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns>No content on success.</returns>
    [HttpDelete("{id:guid}", Name = nameof(DeleteItem))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteItem(Guid id, CancellationToken cancellationToken)
        => Respond(await Sender.SendAsync(new DeleteItemCommand(id), cancellationToken));

    /// <summary>Submits a draft item for review.</summary>
    /// <param name="id">The identity of the item.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns>No content on success.</returns>
    [HttpPost("{id:guid}/submit", Name = nameof(SubmitItem))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SubmitItem(Guid id, CancellationToken cancellationToken)
        => Respond(await Sender.SendAsync(new SubmitItemForReviewCommand(id), cancellationToken));

    /// <summary>Approves an item that is under review.</summary>
    /// <param name="id">The identity of the item.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns>No content on success.</returns>
    [HttpPost("{id:guid}/approve", Name = nameof(ApproveItem))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ApproveItem(Guid id, CancellationToken cancellationToken)
        => Respond(await Sender.SendAsync(new ApproveItemCommand(id), cancellationToken));

    /// <summary>Returns an item to its author.</summary>
    /// <param name="id">The identity of the item.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns>No content on success.</returns>
    [HttpPost("{id:guid}/return-to-draft", Name = nameof(ReturnItemToDraft))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ReturnItemToDraft(Guid id, CancellationToken cancellationToken)
        => Respond(await Sender.SendAsync(new ReturnItemToDraftCommand(id), cancellationToken));

    /// <summary>Publishes an approved item as a new immutable version.</summary>
    /// <param name="id">The identity of the item.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns>No content on success.</returns>
    [HttpPost("{id:guid}/publish", Name = nameof(PublishItem))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> PublishItem(Guid id, CancellationToken cancellationToken)
        => Respond(await Sender.SendAsync(new PublishItemCommand(id), cancellationToken));

    /// <summary>Withdraws a published item from further use.</summary>
    /// <param name="id">The identity of the item.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns>No content on success.</returns>
    [HttpPost("{id:guid}/retire", Name = nameof(RetireItem))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RetireItem(Guid id, CancellationToken cancellationToken)
        => Respond(await Sender.SendAsync(new RetireItemCommand(id), cancellationToken));
}
