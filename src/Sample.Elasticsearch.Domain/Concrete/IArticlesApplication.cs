using System;
using System.Collections.Generic;
using Sample.Elasticsearch.Domain.Indices;
using Sample.Elasticsearch.Domain.Model;

namespace Sample.Elasticsearch.Domain.Concrete
{
    public interface IArticlesApplication
    {
        void PostArticlesSample();
        ICollection<IndexArticles> GetAll();
        ICollection<IndexArticles> GetByTitle(string title);
        ICollection<IndexArticles> GetByContent(string content);
        ICollection<IndexArticles> GetArticlesCondition(string title, string content, DateTime? publishDate);
        ICollection<IndexArticles> GetArticlesAllCondition(string term);
        ArticleAggregationModel GetArticlesAggregation();
    }
}
