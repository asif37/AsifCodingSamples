using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using elearning.web.Code.Models;
using elearning.web.Controllers.Abstract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace elearning.web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TopicController : AppControllerBase<TopicController>
    {
        [HttpGet]
        [Route("lesson/{lessonId}")]
        public async Task<ActionResult> GetLessonTopics(int lessonId)
        {
            var topics = await DbContext.Topics.Where(o => o.LessonId == lessonId).ToListAsync();
            if (topics.Count == 0)
            {
                return NotFound();
            }
            return Ok(topics);
        }

        [HttpPost]
        [Route("{lessonId:int}/{topicType}/{topicName}")]
        public async Task<ActionResult> CreateNewTopic(int lessonId, TopicType topicType, string topicName)
        {
            var topic = (topicType == TopicType.Quiz ? (Topic)new Quiz() : (Topic)new Blob());
            topic.LessonId = lessonId;
            topic.Type = topicType;
            topic.Title = topicName;

            DbContext.Topics.Add(topic);
            await DbContext.SaveChangesAsync();
            return Ok(topic);
        }

        [HttpGet]
        public async Task<ActionResult> GetTopicById(int topicId)
        {
            var topic = await DbContext.Topics.FindAsync(topicId);
            if (topic == null)
            {
                return NotFound();
            }
            return Ok(topic);
        }

        [HttpPut]
        public async Task<ActionResult> SaveTopic(Topic topic)
        {
            DbContext.Entry(topic).State = EntityState.Modified;
            await DbContext.SaveChangesAsync();
            return NoContent();
        }

        [HttpGet]
        [Route("quiz/questions/{topicId:int}")]
        public async Task<ActionResult> GetQuizQuestions(int topicId)
        {
            var questions = await DbContext.Questions.Where(o => o.QuizId == topicId).ToListAsync();
            if (questions.Count == 0)
            {
                return NotFound();
            }
            return Ok(questions);
        }

        [HttpPost]
        [Route("quiz/question")]
        public async Task<ActionResult> AddQuizQuestion(Question question)
        {
            DbContext.Questions.Add(question);
            await DbContext.SaveChangesAsync();
            return Ok(question);
        }

        [HttpDelete]
        [Route("quiz/question/{questionId:int}")]
        public async Task<ActionResult> DeleteQuizQuestion(int questionId)
        {
            var question = await DbContext.Questions.FindAsync(questionId);
            if (question == null)
            {
                return NotFound();
            }
            DbContext.Questions.Remove(question);
            await DbContext.SaveChangesAsync();
            return Ok();
        }
    }
}