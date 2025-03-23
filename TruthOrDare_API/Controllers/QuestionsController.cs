using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
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
        private readonly IQuestionRepository _questionRepository;
        public QuestionsController(IQuestionRepository questionRepository)
        {
            _questionRepository = questionRepository;
        }
        [HttpGet("questions")]
        public async Task<IActionResult> GetQuestions(string? mode, string? type, string? difficulty, string? age_group)
        {
            var questions = await _questionRepository.GetQuestions(mode, type, difficulty, age_group);
            if (questions != null)
            {
                return NotFound(questions);
            }
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
