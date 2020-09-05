using System;
using System.Collections.Generic;
using System.Text;

namespace Sample.Elasticsearch.Domain.Model
{
    public class ArticleAggregationModel
    {
        public double TotalViews { get; set; }
        public double AverageViews { get; set; }
    }
}
