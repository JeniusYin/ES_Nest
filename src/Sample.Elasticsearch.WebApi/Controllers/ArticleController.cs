using Microsoft.AspNetCore.Mvc;
using Sample.Elasticsearch.Domain.Concrete;
using System;

namespace Sample.Elasticsearch.WebApi.Controllers
{
    [Route("api/[controller]")]
    public class ArticleController : Controller
    {
        private readonly IArticlesApplication _actorsApplication;

        public ArticleController(IArticlesApplication actorsApplication)
        {
            _actorsApplication = actorsApplication;
        }

        [HttpPost("sample")]
        public IActionResult PostArticlesSample()
        {
            _actorsApplication.PostArticlesSample();

            return Ok(new { Result = "Data successfully registered with Elasticsearch" });
        }

        [HttpGet("")]
        public IActionResult GetAll()
        {
            var result = _actorsApplication.GetAll();

            return Json(result);
        }

        [HttpGet("title")]
        public IActionResult GetByTitle([FromQuery] string title)
        {
            var result = _actorsApplication.GetByTitle(title);

            return Json(result);
        }

        [HttpGet("content")]
        public IActionResult GetByContent([FromQuery] string content)
        {
            var result = _actorsApplication.GetByContent(content);

            return Json(result);
        }

        [HttpGet("Condition")]
        public IActionResult GetArticlesCondition([FromQuery] string title, [FromQuery] string content, [FromQuery] DateTime? publishDate)
        {
            var result = _actorsApplication.GetArticlesCondition(title, content, publishDate);

            return Json(result);
        }

        [HttpGet("term")]
        public IActionResult GetByAllCondictions([FromQuery] string term)
        {
            var result = _actorsApplication.GetArticlesAllCondition(term);

            return Json(result);
        }

        [HttpGet("aggregation")]
        public IActionResult GetArticlesAggregation()
        {
            var result = _actorsApplication.GetArticlesAggregation();

            return Json(result);
        }
    }
}
