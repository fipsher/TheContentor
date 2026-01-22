using MediatR;
using Microsoft.AspNetCore.Mvc;
using TheContentor.Application.Features.Criteria.Commands;
using TheContentor.Application.Features.Criteria.Models;
using TheContentor.Application.Features.Criteria.Queries;

namespace TheContentor.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CriteriaController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<CriteriaDto>>> GetList()
    {
        return await mediator.Send(new GetCriteriaListQuery());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CriteriaDto>> GetById(Guid id)
    {
        var result = await mediator.Send(new GetCriteriaByIdQuery(id));
        if (result == null)
        {
            return NotFound();
        }

        return result;
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> Create(CreateCriteriaCommand command)
    {
        var id = await mediator.Send(command);
        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult> Update(Guid id, UpdateCriteriaCommand command)
    {
        if (id != command.Id)
        {
            return BadRequest();
        }

        var result = await mediator.Send(command);
        if (!result)
        {
            return NotFound();
        }

        return NoContent();
    }
}
