﻿using Carter;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Newsletter.Api.Contracts;
using Newsletter.Api.Database;
using Newsletter.Api.Shared;

namespace Newsletter.Api.Features.Articles;

public static class GetArticle
{
    public class Query : IRequest<Result<ArticleResponse>>
    {
        public Guid Id { get; set; }
    }

    internal sealed class Handler : IRequestHandler<Query, Result<ArticleResponse>>
    {
        private readonly ApplicationDbContext _dbContext;

        public Handler(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<Result<ArticleResponse>> Handle(Query request, CancellationToken cancellationToken)
        {
            var articleResponse = await _dbContext
                .Articles
                .AsNoTracking()
                .Where(article => article.Id == request.Id)
                .Select(article => new ArticleResponse
                {
                    Id = article.Id,
                    Title = article.Title,
                    Content = article.Content,
                    Tags = article.Tags,
                    CreatedOnUtc = article.CreatedOnUtc,
                    PublishedOnUtc = article.PublishedOnUtc
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (articleResponse is null)
            {
                return Result.Failure<ArticleResponse>(new Error(
                    "GetArticle.Null",
                    "The article with the specified ID was not found"));
            }

            return articleResponse;
        }
    }
}

public class GetArticleEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("api/articles/{id}", async (Guid id, ISender sender) =>
        {
            var query = new GetArticle.Query { Id = id };

            var result = await sender.Send(query);

            if (result.IsFailure)
            {
                return Results.NotFound(result.Error);
            }

            return Results.Ok(result.Value);
        });
    }
}
