using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using TruthOrDare_Contract.DTOs;
using TruthOrDare_Contract.IRepository;
using TruthOrDare_Contract.IServices;
using TruthOrDare_Infrastructure;

namespace TruthOrDare_API.Controllers
{
    [Route("api/questions")]
    [ApiController]
    public class QuestionsController : ControllerBase
    {
        private readonly MongoDbContext _dbContext;
        private readonly IQuestionRepository _questionRepository;
        public QuestionsController(MongoDbContext dbContext, IQuestionRepository questionRepository)
        {
            _dbContext = dbContext;
            _questionRepository = questionRepository;
        }
        [HttpGet("questions")]
        public async Task<IActionResult> GetQuestions()
        {
            var questions = await _dbContext.Questions
                .Find(_ => true)
                .Limit(10) // Giới hạn 10 câu hỏi để test
                .ToListAsync();
            return Ok(questions);
        }
        [HttpPost]
        public async Task<ActionResult> CreateQuestion([FromBody] QuestionCreateDTO questionCreate)
        {
            var result = await _questionRepository.CreateQuestion(questionCreate);
            if(result == "Success")
            {
                return Ok(result);
            }
            else if (result == "Question text and type are required")
            {
                return BadRequest(result);
            }
            else if (result == "Question text already exist")
            {
                return Conflict(result);
            }
            else
            {
                return StatusCode(500, result); // Failed: {error message}
            }
        }
    }
}
