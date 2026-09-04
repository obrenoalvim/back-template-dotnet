using BackTemplate.Api.Data;
using BackTemplate.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace BackTemplate.Api.Repositories;

public class BookRepository(AppDbContext db) : IBookRepository
{
    public async Task<List<Book>> GetAllWithAuthorsAsync(CancellationToken cancellationToken = default)
    {
        return await db.Books
            .AsNoTracking()
            .Include(b => b.Author)
            .OrderBy(b => b.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<Book> AddAsync(Book book, CancellationToken cancellationToken = default)
    {
        db.Books.Add(book);
        await db.SaveChangesAsync(cancellationToken);
        return book;
    }
}
