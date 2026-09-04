using BackTemplate.Api.Data.Entities;
using BackTemplate.Api.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BackTemplate.Api.Controllers;

[ApiController]
[Route("api/books")]
[Authorize]
public class BooksController(IBookRepository books) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<Book>>> GetAll(CancellationToken cancellationToken)
    {
        var result = await books.GetAllWithAuthorsAsync(cancellationToken);
        return Ok(result);
    }
}
