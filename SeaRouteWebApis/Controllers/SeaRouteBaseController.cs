using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SeaRouteWebApis.Interfaces;

namespace SeaRouteWebApis.Controllers
{
    [ApiController]
    public class SeaRouteBaseController<T>(ILoggerFactory loggerFactory, IRepository<T> repository) : ControllerBase where T : class
    {
        private readonly ILogger<SeaRouteBaseController<T>> _logger = loggerFactory.CreateLogger<SeaRouteBaseController<T>>();
        private readonly IRepository<T> _repository = repository;

        [HttpPost]
        public IActionResult Insert([FromBody] T entity)
        {
            _repository.Insert(entity);
            return Ok();
        }

        [HttpPut]
        public IActionResult Update([FromBody] T entity)
        {
            _repository.Update(entity);
            return Ok();
        }

        [HttpGet("{id}")]
        public IActionResult Get(int id)
        {
            var entity = _repository.Get(id);
            if (entity == null)
            {
                return NotFound();
            }
            return Ok(entity);
        }
        [HttpGet]
        public IActionResult GetAll()
        {
            var entity = _repository.GetAll();
            if (entity == null)
            {
                return NotFound();
            }
            return Ok(entity);
        }

    }
}
