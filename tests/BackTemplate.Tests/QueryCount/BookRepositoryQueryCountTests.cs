using BackTemplate.Api.Data.Entities;
using BackTemplate.Api.Diagnostics;
using BackTemplate.Api.Repositories;
using BackTemplate.Tests.TestFixtures;
using Xunit;

namespace BackTemplate.Tests.QueryCount;

/// <summary>
/// Guarda contra N+1: GetAllWithAuthorsAsync deve continuar rodando 1 query,
/// não importa quantos livros existam. Se alguém remover o .Include() do
/// BookRepository, esse teste fica vermelho no CI antes de virar problema em produção.
/// </summary>
[Collection(nameof(PostgresCollection))]
public class BookRepositoryQueryCountTests(PostgresFixture fixture)
{
    [Fact]
    public async Task GetAllWithAuthorsAsync_runs_exactly_one_query_regardless_of_row_count()
    {
        var interceptor = new QueryCountInterceptor();

        await using (var seedContext = fixture.CreateContext())
        {
            var author1 = new Author { Name = "Robert C. Martin" };
            var author2 = new Author { Name = "Eric Evans" };
            seedContext.Authors.AddRange(author1, author2);
            await seedContext.SaveChangesAsync();

            seedContext.Books.AddRange(
                new Book { Title = "Clean Code", AuthorId = author1.Id },
                new Book { Title = "The Clean Coder", AuthorId = author1.Id },
                new Book { Title = "Clean Architecture", AuthorId = author1.Id },
                new Book { Title = "Domain-Driven Design", AuthorId = author2.Id });
            await seedContext.SaveChangesAsync();
        }

        await using var context = fixture.CreateContext(interceptor);
        var repository = new BookRepository(context);

        interceptor.Reset();
        var books = await repository.GetAllWithAuthorsAsync();

        Assert.Equal(4, books.Count);
        Assert.All(books, b => Assert.False(string.IsNullOrEmpty(b.Author.Name)));
        Assert.Equal(1, interceptor.CommandCount);
    }
}
