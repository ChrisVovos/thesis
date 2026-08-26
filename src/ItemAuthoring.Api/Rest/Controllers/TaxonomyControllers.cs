using Asp.Versioning;
using ItemAuthoring.Application.Abstractions.Messaging;
using ItemAuthoring.Application.Items.Commands;
using ItemAuthoring.Application.Items.Dtos;
using ItemAuthoring.Application.Items.Queries;
using Microsoft.AspNetCore.Mvc;

namespace ItemAuthoring.Api.Rest.Controllers;

/// <summary>
/// The category taxonomy of the item bank.
/// </summary>
/// <param name="sender">The request dispatcher.</param>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/categories")]
public sealed class CategoriesController(ISender sender) : ApiControllerBase(sender)
{
    /// <summary>Reads the complete category taxonomy.</summary>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns>Every category.</returns>
    [HttpGet(Name = nameof(ListCategories))]
    [ProducesResponseType(typeof(IReadOnlyList<CategoryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListCategories(CancellationToken cancellationToken)
        => Respond(await Sender.SendAsync(new ListCategoriesQuery(), cancellationToken));

    /// <summary>Creates a category.</summary>
    /// <param name="command">The category to create.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns>The identity of the new category.</returns>
    [HttpPost(Name = nameof(CreateCategory))]
    [ProducesResponseType(typeof(CreatedResourceResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateCategory(
        [FromBody] CreateCategoryCommand command,
        CancellationToken cancellationToken)
    {
        var result = await Sender.SendAsync(command, cancellationToken);
        return RespondCreated(result, nameof(ListCategories), new { });
    }

    /// <summary>Replaces the details of a category.</summary>
    /// <param name="id">The identity of the category.</param>
    /// <param name="command">The new details.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns>No content on success.</returns>
    [HttpPut("{id:guid}", Name = nameof(UpdateCategory))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateCategory(
        Guid id,
        [FromBody] UpdateCategoryCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        return Respond(await Sender.SendAsync(command with { CategoryId = id }, cancellationToken));
    }

    /// <summary>Deletes a category that holds no items.</summary>
    /// <param name="id">The identity of the category.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns>No content on success.</returns>
    [HttpDelete("{id:guid}", Name = nameof(DeleteCategory))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteCategory(Guid id, CancellationToken cancellationToken)
        => Respond(await Sender.SendAsync(new DeleteCategoryCommand(id), cancellationToken));
}

/// <summary>
/// The free-form tags attached to items.
/// </summary>
/// <param name="sender">The request dispatcher.</param>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/tags")]
public sealed class TagsController(ISender sender) : ApiControllerBase(sender)
{
    /// <summary>Reads every tag, ordered by label.</summary>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns>Every tag.</returns>
    [HttpGet(Name = nameof(ListTags))]
    [ProducesResponseType(typeof(IReadOnlyList<TagDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListTags(CancellationToken cancellationToken)
        => Respond(await Sender.SendAsync(new ListTagsQuery(), cancellationToken));

    /// <summary>Creates a tag, or returns the existing one when the label is already in use.</summary>
    /// <param name="command">The tag to create.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns>The identity of the tag.</returns>
    [HttpPost(Name = nameof(CreateTag))]
    [ProducesResponseType(typeof(CreatedResourceResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateTag(
        [FromBody] CreateTagCommand command,
        CancellationToken cancellationToken)
    {
        var result = await Sender.SendAsync(command, cancellationToken);
        return RespondCreated(result, nameof(ListTags), new { });
    }

    /// <summary>Deletes a tag and detaches it from every item.</summary>
    /// <param name="id">The identity of the tag.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns>No content on success.</returns>
    [HttpDelete("{id:guid}", Name = nameof(DeleteTag))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteTag(Guid id, CancellationToken cancellationToken)
        => Respond(await Sender.SendAsync(new DeleteTagCommand(id), cancellationToken));
}
