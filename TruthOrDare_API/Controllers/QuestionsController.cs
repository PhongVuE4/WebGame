﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using TruthOrDare_Contract.DTOs.Question;
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
        public async Task<IActionResult> GetQuestions(string? filters)
        {
            var question =  await _questionRepository.GetQuestions(filters);
            return Ok(new { message = "Get questions successfully.", data = question });
        }
        [HttpPost("add-a-question")]
        public async Task<ActionResult> CreateQuestion([FromBody] QuestionCreateDTO questionCreate)
        {
            await _questionRepository.CreateQuestion(questionCreate);
            return Ok(new { message = "Question created successfully." });

        }
        [HttpPost("add-many-question")]
        public async Task<IActionResult> InsertManyQuestions([FromBody] List<QuestionCreateDTO> questions)
        {
            await _questionRepository.InsertManyQuestions(questions);

            return Ok(new { message = "Questions created successfully." });
        }
        [HttpDelete("delete-question")]
        public async Task<IActionResult> DeleteQuestion(string questionId)
        {
            await _questionRepository.DeleteQuestion(questionId);

            return Ok(new { message = "Deleted question successfully." });
        }
    }
}
