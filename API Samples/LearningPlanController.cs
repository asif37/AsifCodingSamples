using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using elearning.web.Code.Models;
using elearning.web.Code.ViewModels;
using elearning.web.Controllers.Abstract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace elearning.web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LearningPlanController : AppControllerBase<LearningPlanController>
    {
        [HttpGet]
        [Route("all")]
        public async Task<IList<LearningPlan>> GetMyLearningPlans()
        {
            return await DbContext.LearningPlans.Where(o => o.CreatedById == AdminUserID).ToListAsync();
        }

        [HttpPost]
        public async Task<ActionResult> CreateNewLearningPlan([FromBody] LearningPlan learningPlan)
        {
            learningPlan.CreatedById = AdminUserID;
            DbContext.LearningPlans.Add(learningPlan);
            await DbContext.SaveChangesAsync();
            return Ok(learningPlan);
        }

        [HttpGet]
        public async Task<ActionResult> GetLearningPlanById(int id)
        {
            var learningPlan = await DbContext.LearningPlans.FindAsync(id);
            if (learningPlan == null)
            {
                return NotFound();
            }
            else
            {
                return Ok(learningPlan);
            }
        }

        [HttpPut]
        [Route("{id}/{name}/{description}")]
        public async Task<ActionResult> SaveLearningPlan(int id, string name, string description)
        {
            var learningPlan = await DbContext.LearningPlans.FirstOrDefaultAsync(o => o.CreatedById == AdminUserID && o.Id == id);

            if (learningPlan == null)
            {
                return NotFound();
            }

            learningPlan.Name = name;
            learningPlan.Description = description;
            await DbContext.SaveChangesAsync();
            return Ok(learningPlan);
        }

        [HttpDelete]
        public async Task<ActionResult> DeleteLearningPlan(int learningPlanId)
        {
            var learningPlan = await DbContext.LearningPlans.FirstOrDefaultAsync(o => o.Id == learningPlanId && o.CreatedById == AdminUserID);
            if (learningPlan == null)
            {
                return NotFound();
            }
            DbContext.LearningPlans.Remove(learningPlan);
            await DbContext.SaveChangesAsync();
            return NoContent();
        }

        [HttpGet]
        [Route("getlearningplanemployees/{learningPlanId}")]
        public async Task<ActionResult> GetLearningPlanEmployees(int learningPlanId)
        {
            var employees = await DbContext.EmployeeLearningPlans
                .Include(o => o.Employee)
                .Where(o => o.LearningPlanId == learningPlanId && o.LearningPlan.CreatedById == AdminUserID)
                .Select(o => o.Employee)
                .ToListAsync();

            if (employees.Count == 0)
            {
                return NotFound();
            }
            return Ok(employees.Select(o => new EmployeeVM { Id = o.Id, Name = o.Name, Description = o.Description }));
        }

        [HttpPost]
        [Route("assigntoemployees")]
        public async Task<ActionResult> AssignLearningPlanToEmployees(int learningPlanId, int[] employeeIds)
        {
            // NOTE: First unassign all employees from this learning plan and then assign all employees with emplueeIds parameter.
            // All this needs to be in a transaction
            var found = await DbContext.LearningPlans.Include(o => o.Employees).FirstOrDefaultAsync(o => o.Id == learningPlanId);
            if (found != null)
            {
                if (found.Employees.Count > 0)
                {
                    DbContext.EmployeeLearningPlans.RemoveRange(found.Employees);
                }
                var employeesToAssign = DbContext.Employees.Where(o => employeeIds.Contains(o.Id));
                foreach (var employee in employeesToAssign)
                {
                    found.Employees.Add(new EmployeeLearningPlans { Employee = employee });
                }
                await DbContext.SaveChangesAsync();
                return NoContent();
            }
            return NotFound();
        }
    }
}