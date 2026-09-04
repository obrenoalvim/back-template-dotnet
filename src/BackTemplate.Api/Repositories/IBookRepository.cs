using BackTemplate.Api.Data.Entities;

namespace BackTemplate.Api.Repositories;

public interface IBookRepository
{
    /// <summary>
    /// Traz todos os livros com o autor já carregado, numa query só.
    /// Existe um teste (BookRepositoryQueryCountTests) que garante isso continue
    /// sendo 1 query: se o .Include() for removido ou trocado por acesso lazy
    /// dentro de um loop, o teste quebra no CI antes de virar N+1 em produção.
    /// </summary>
    Task<List<Book>> GetAllWithAuthorsAsync(CancellationToken cancellationToken = default);

    Task<Book> AddAsync(Book book, CancellationToken cancellationToken = default);
}
