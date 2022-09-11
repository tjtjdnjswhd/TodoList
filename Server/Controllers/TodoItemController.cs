using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

using TodoList.Shared.Data.Models;
using TodoList.Shared.Svcs.Interfaces;

namespace TodoList.Server.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class TodoItemController : ControllerBase
    {
        private readonly ITodoItemService _todoItemService;
        private readonly ILogger<TodoItemController> _logger;

        public TodoItemController(ITodoItemService todoItemService, ILogger<TodoItemController> logger)
        {
            _todoItemService = todoItemService;
            _logger = logger;
        }

        [HttpGet]
        [Authorize]
        public IActionResult Get(int? skip, int? take)
        {
            if (!Guid.TryParse(User.FindFirstValue(JwtRegisteredClaimNames.Jti), out Guid id))
            {
                return BadRequest(new { error_message = "Wrong id" });
            }

            IAsyncEnumerable<TodoItem> items;

            if (skip == null)
            {
                if (take == null)
                {
                    items = _todoItemService.GetByUserId(id);
                }
                else
                {
                    items = _todoItemService.GetByUserId(id, take: take.Value);
                }
            }
            else
            {
                if (take == null)
                {
                    items = _todoItemService.GetByUserId(id, skip: skip.Value);
                }
                else
                {
                    items = _todoItemService.GetByUserId(id, skip: skip.Value, take: take.Value);
                }
            }

            return Ok(new { items });
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> PostAsync(string name)
        {
            if (Guid.TryParse(User.FindFirstValue(JwtRegisteredClaimNames.Jti), out Guid userId))
            {
                await _todoItemService.AddAsync(name, userId);
                return CreatedAtAction("Get", "TodoItem");
            }
            return BadRequest();
        }

        [HttpPatch]
        [Authorize]
        public async Task<IActionResult> PatchAsync(int itemId, string newName)
        {
            await _todoItemService.EditNameAsync(itemId, newName);
            return Ok();
        }

        [Authorize]
        [HttpDelete]
        public async Task<IActionResult> DeleteAsync(int itemId)
        {
            await _todoItemService.DeleteAsync(itemId);
            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> ToggleCompleteAsync(int itemId)
        {
            TodoItem? item = await _todoItemService.GetByIdOrNullAsync(itemId);
            if (item == null)
            {
                return NotFound(new { error_message = "Item not found" });
            }

            if (item.IsComplete)
            {
                await _todoItemService.UncompleteAsync(itemId);
            }
            else
            {
                await _todoItemService.CompleteAsync(itemId);
            }

            return Accepted();
        }
    }
}
