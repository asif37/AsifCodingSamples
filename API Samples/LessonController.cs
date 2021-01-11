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
    public class LessonController : AppControllerBase<LessonController>
    {
        [HttpGet]
        [Route("course/{courseId}")]
        public async Task<ActionResult> GetCourseLessons(int courseId)
        {
            var lessons = await DbContext.Lessons.Where(o => o.CourseId == courseId).ToListAsync();
            if (lessons.Count == 0)
            {
                return NotFound();
            }
            return Ok(lessons);
        }

        [HttpPost]
        [Route("{courseId}/{lessonName}")]
        public async Task<ActionResult> CreateNewLesson(int courseId, string lessonName)
        {
            var lesson = new Lesson
            {
                Title = lessonName,
                CourseId = courseId
            };
            DbContext.Lessons.Add(lesson);
            await DbContext.SaveChangesAsync();
            return Ok(lesson);
        }

        [HttpGet]
        public async Task<ActionResult> GetLessonById(int lessonId)
        {
            var lesson = await DbContext.Lessons.FindAsync(lessonId);
            if (lesson == null)
            {
                return NotFound();
            }
            return Ok(lesson);
        }

        [HttpPut]
        public async Task<ActionResult> SaveLesson(Lesson lesson)
        {
            DbContext.Entry(lesson).State = EntityState.Modified;
            await DbContext.SaveChangesAsync();
            return NoContent();
        }
    }
}