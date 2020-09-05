using System;
using System.Collections.Generic;
using System.Linq;
using Nest;
using Sample.Elasticsearch.Domain.Concrete;
using Sample.Elasticsearch.Domain.Indices;
using Sample.Elasticsearch.Domain.Model;

namespace Sample.Elasticsearch.Domain.Application
{
    public class ArticlesApplication : IArticlesApplication
    {
        private readonly IElasticClient _elasticClient;

        public ArticlesApplication(IElasticClient elasticClient)
        {
            _elasticClient = elasticClient;
        }

        public void PostArticlesSample()
        {
            if (!_elasticClient.Indices.Exists(IndexArticles.ArticleIndex).Exists)
                _elasticClient.Indices.Create(IndexArticles.ArticleIndex);

            _elasticClient.IndexMany<IndexArticles>(IndexArticles.GetSampleData(), IndexArticles.ArticleIndex);

            #region
            var descriptor = new BulkDescriptor();

            descriptor.UpdateMany<IndexArticles>(IndexArticles.GetSampleData(), (b, u) => b
                .Index(IndexArticles.ArticleIndex)
                .Doc(u)
                .DocAsUpsert());

            var insert = _elasticClient.Bulk(descriptor);

            if (!insert.IsValid)
                throw new Exception(insert.OriginalException.ToString());
            #endregion
        }

        public ICollection<IndexArticles> GetAll()
        {
            var result = _elasticClient.Search<IndexArticles>(s => s
                .Index(IndexArticles.ArticleIndex)
                .Sort(q => q.Descending(p => p.PublishDate)))?.Documents;

            #region
            var result2 = _elasticClient.Search<IndexArticles>(s => s
                .Index(IndexArticles.ArticleIndex)
                .MatchAll()).Documents.ToList();

            var result3 = _elasticClient.Search<IndexArticles>(s => s
                .Index(IndexArticles.ArticleIndex)
                .From(0)
                .Size(5)
                .MatchAll()).Documents.ToList();

            //scroll
            var result4 = _elasticClient.Search<IndexArticles>(s => s
                .Index(IndexArticles.ArticleIndex)
                .From(0)
                .Size(5)
                .Scroll("1m")
                .MatchAll());

            List<IndexArticles> results = new List<IndexArticles>();

            if (result4.Documents.Any())
                results.AddRange(result4.Documents);

            string scrollid = result4.ScrollId;
            bool isScrollSetHasData = true;
            while (isScrollSetHasData)
            {
                ISearchResponse<IndexArticles> loopingResponse = _elasticClient.Scroll<IndexArticles>("1m", scrollid);
                if (loopingResponse.IsValid)
                {
                    results.AddRange(loopingResponse.Documents);
                    scrollid = loopingResponse.ScrollId;
                }
                isScrollSetHasData = loopingResponse.Documents.Any();
            }

            _elasticClient.ClearScroll(new ClearScrollRequest(scrollid));
            #endregion

            return results;
        }

        public ICollection<IndexArticles> GetByTitle(string title)
        {
            //usado em lowcase
            var query = new QueryContainerDescriptor<IndexArticles>().Term(t => t.Field(f => f.Title).Value(title));

            var result = _elasticClient.Search<IndexArticles>(s => s
                .Index(IndexArticles.ArticleIndex)
                .Query(s => query)
                .Size(5)
                .Sort(q => q.Descending(p => p.PublishDate)))?.Documents;

            #region
            var result2 = _elasticClient.Search<IndexArticles>(s => s
                .Index(IndexArticles.ArticleIndex)
                .Query(s => s.Wildcard(w => w.Field(f => f.Title).Value(title + "*")))
                .Size(5)
                .Sort(q => q.Descending(p => p.PublishDate)))?.Documents;

            var result3 = _elasticClient.Search<IndexArticles>(s => s
                .Index(IndexArticles.ArticleIndex)
                .Query(s => s.Match(m => m.Field(f => f.Title).Query(title))) 
                //.Query(s => s.Match(m => m.Field(f => f.Title).Query(title).Operator(Operator.And))
                .Size(5)
                .Sort(q => q.Descending(p => p.PublishDate)))?.Documents;

            var result4 = _elasticClient.Search<IndexArticles>(s => s
                .Index(IndexArticles.ArticleIndex)
                .Query(s => s.MatchPhrase(m => m.Field(f => f.Title).Query(title))) 
                 //.Query(s => s.MatchPhrase(m => m.Field(f => f.Title).Query(title).Slop(1))) 
                .Size(5)
                .Sort(q => q.Descending(p => p.PublishDate)))?.Documents;
            #endregion

            return result3?.ToList();
        }

        public ICollection<IndexArticles> GetByContent(string content)
        {
            //term是代表完全匹配，也就是精确查询，搜索前不会再对搜索词进行分词拆解。
            //match进行搜索的时候，会先进行分词拆分，拆完后，再来匹配
            //match_phrase 称为短语搜索，要求所有的分词必须同时出现在文档中，同时位置必须紧邻一致。
            //var query = new QueryContainerDescriptor<IndexArticles>().Match(t => t.Field(f => f.Content).Query(content));
            var query = new QueryContainerDescriptor<IndexArticles>().MatchPhrase(t => t.Field(f => f.Content).Query(content));
            //var query = new QueryContainerDescriptor<IndexArticles>().Term(t => t.Content, content); 
            var result = _elasticClient.Search<IndexArticles>(s => s
                    .Index(IndexArticles.ArticleIndex)
                    .Query(s => query)
                    .Size(10)
                    .Sort(q => q.Descending(p => p.PublishDate)))?.Documents;

            return result?.ToList();
        }

        public ICollection<IndexArticles> GetArticlesCondition(string title, string content, DateTime? publishDate)
        {
            //use Fuzzy para autocomplete

            QueryContainer query = new QueryContainerDescriptor<IndexArticles>();

            if (!string.IsNullOrEmpty(title))
            {
                query = query && new QueryContainerDescriptor<IndexArticles>().Match(qs => qs.Field(fs => fs.Title).Query(title));
            }
            if (!string.IsNullOrEmpty(content))
            {
                query = query && new QueryContainerDescriptor<IndexArticles>().Match(qs => qs.Field(fs => fs.Content).Query(content));
            }
            if (publishDate.HasValue)
            {
                query = query && new QueryContainerDescriptor<IndexArticles>()
                .Bool(b => b.Filter(f => f.DateRange(dt => dt
                                           .Field(field => field.PublishDate)
                                           .GreaterThanOrEquals(publishDate)
                                           .LessThanOrEquals(publishDate)
                                           .TimeZone("+00:00"))));
            }

            var result = _elasticClient.Search<IndexArticles>(s => s
                    .Index(IndexArticles.ArticleIndex)
                    .Query(s => query)
                    .Size(10)
                    .Sort(q => q.Descending(p => p.PublishDate)))?.Documents;

            return result?.ToList();
        }

        public ICollection<IndexArticles> GetArticlesAllCondition(string term)
        {
            QueryContainer query = new QueryContainerDescriptor<IndexArticles>().Bool(b => b.Must(m => m.Exists(e => e.Field(f => f.Content))));

            query = query && new QueryContainerDescriptor<IndexArticles>().MatchPhrase(w => w.Field(f => f.Title).Query(term))
                   || new QueryContainerDescriptor<IndexArticles>().MatchPhrase(w => w.Field(f => f.Content).Query(term))
                   || new QueryContainerDescriptor<IndexArticles>().MatchPhrase(w => w.Field(f => f.Author).Query(term));

            var result = _elasticClient.Search<IndexArticles>(s => s
                .Index(IndexArticles.ArticleIndex)
                .Query(s => query)
                .Size(10)
                .Sort(q => q.Descending(p => p.PublishDate)))?.Documents;

            return result?.ToList();
        }

        public ArticleAggregationModel GetArticlesAggregation()
        {
            QueryContainer query = new QueryContainerDescriptor<IndexArticles>().Bool(b => b.Must(m => m.Exists(e => e.Field(f => f.Content))));

            var result = _elasticClient.Search<IndexArticles>(s => s
                .Index(IndexArticles.ArticleIndex)
                .Query(s => query)
                .Aggregations(a => a.Sum("TotalViews", sa => sa.Field(p => p.TotalViews))
                            .Average("AverageViews", sa => sa.Field(p => p.TotalViews))
                        ));

            var totalViews = ObterBucketAggregationDouble(result.Aggregations, "TotalViews");
            var avViews = ObterBucketAggregationDouble(result.Aggregations, "AverageViews");

            return new ArticleAggregationModel {TotalViews = totalViews, AverageViews = avViews };
        }

        public static double ObterBucketAggregationDouble(AggregateDictionary agg, string bucket)
        {
            if (agg.BucketScript(bucket).Value.HasValue)
                return agg.BucketScript(bucket).Value.Value;
            return 0;
        }
    }
}
