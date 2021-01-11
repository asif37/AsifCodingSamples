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
    public class EmployeeController : AppControllerBase<EmployeeController>
    {
        [HttpGet]
        [Route("course/{magicLink}")]
        public async Task<ActionResult> GetCourseForEmployee(string magicLink)
        {
            var parts = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(magicLink)).Split(":");
            if (parts.Length == 3)
            {
                var uuid = parts[0];
                var employeeId = Convert.ToInt32(parts[1]);
                var courseId = Convert.ToInt32(parts[2]);

                // If an employee is found having the same session code
                // and session time is not greater than 20 min then he is allowed to view
                var employee = DbContext.Employees.FirstOrDefault(o =>
                    o.Id == employeeId &&
                    o.ExternalSessionCode == uuid);
                if (employee != null &&
                    Math.Abs(employee.ExternalSessionCodeCreationDate.Subtract(DateTime.Now).TotalMinutes) <= 20)
                {
                    var course = await DbContext.Courses
                        .Include(o => o.LearningPlan).ThenInclude(o => o.Employees)
                        .Include(o => o.Lessons)
                        .FirstOrDefaultAsync(o => o.Id == courseId);

                    if (course == null)
                    {
                        return BadRequest();
                    }
                    var employeeLearningPlan = course.LearningPlan.Employees.FirstOrDefault(o => o.EmployeeId == employeeId);
                    if (employeeLearningPlan == null)
                    {
                        return Unauthorized();
                    }

                    var courseVM = GetCourseVmFromCourseEntity(course);

                    employee.ExternalSessionCodeCreationDate = DateTime.Now;
                    await DbContext.SaveChangesAsync();
                    return Ok(courseVM);
                }
                else
                    return Unauthorized();
            }
            return BadRequest();
        }

        private Code.ViewModels.CoursePlayer.Course GetCourseVmFromCourseEntity(Course course)
        {
            var courseVM = new Code.ViewModels.CoursePlayer.Course
            {
                Id = course.Id,
                Title = course.Title,
                Description = course.Description,
                Detail = course.Details,
                Instruction = course.Instructions
            };

            var courseLessonIds = course.Lessons.Select(o => o.Id).ToArray();
            var blobs = DbContext.Topics.Where(o => courseLessonIds.Contains(o.LessonId)).OfType<Blob>();
            var quizes = DbContext.Topics.Where(o => courseLessonIds.Contains(o.LessonId)).OfType<Quiz>().Include(o => o.Questions);

            foreach (var lesson in course.Lessons)
            {
                var lessonVM = new Code.ViewModels.CoursePlayer.Lesson
                {
                    Id = lesson.Id,
                    Title = lesson.Title,
                    Description = lesson.Description,
                    Details = lesson.Details,
                    TimeToCompleteInMin = lesson.TimeToComplete,
                    Instructions = lesson.Instructions,
                    CreatedOn = lesson.CreatedOn
                };

                foreach (var topic in blobs.Where(o => o.LessonId == lesson.Id))
                {
                    var topicVM = new Code.ViewModels.CoursePlayer.Topic
                    { 
                        Id = topic.Id,
                        Title = topic.Title,
                        Type = topic.Type.ToString(),
                        BlobLink = topic.ContentBlobLink
                    };
                    lessonVM.Topics.Add(topicVM);
                }

                foreach (var topic in quizes.Where(o => o.LessonId == lesson.Id))
                {
                    var topicVM = new Code.ViewModels.CoursePlayer.Topic
                    {
                        Id = topic.Id,
                        Title = topic.Title,
                        Type = topic.Type.ToString(),
                        Questions = topic.Questions.Select(o => new Code.ViewModels.CoursePlayer.Question { Id = o.Id, Text = o.Text }).ToList()
                    };
                    lessonVM.Topics.Add(topicVM);
                }
                courseVM.Lessons.Add(lessonVM);
            }

            return courseVM;
        }

        // TODO: This web method needs to be secured such that it can only be called by the external system
        [HttpPost]
        [Route("getemployeecoursemagiclink/{employeeId}/{courseId}")]
        public async Task<ActionResult> UpdateEmployeeExternalSession(int employeeId, int courseId)
        {
            var employee = await DbContext.Employees.FindAsync(employeeId);
            if (employee != null)
            {
                var uuid = Guid.NewGuid().ToString();
                employee.ExternalSessionCode = uuid;
                employee.ExternalSessionCodeCreationDate = DateTime.Now;
                await DbContext.SaveChangesAsync();

                var magic_link = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{uuid}:{employeeId}:{courseId}"));
                return Ok($"{magic_link}");
            }
            return NotFound();
        }

        [HttpPost]
        [Route("postquizquestionanswer/{employeeId}/{questionId}/{answerText}")]
        public async Task<ActionResult> PostQuizQuestionAnswer(int employeeId, int questionId, string answerText)
        {
            var employee = DbContext.Employees.Find(employeeId);
            var question = DbContext.Questions.Find(questionId);
            if (employee != null && question != null)
            {
                var answer = DbContext.Answers.FirstOrDefault(o => o.QuestionId == questionId && o.EmployeeId == employeeId);
                if (answer != null)
                {
                    answer.Text = answerText;
                }
                else
                {
                    DbContext.Answers.Add(new Answer
                    {
                        EmployeeId = employeeId,
                        QuestionId = questionId,
                        Text = answerText
                    });
                }
                await DbContext.SaveChangesAsync();
                return Ok();
            }
            return BadRequest();
        }
    }
}