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
        private static readonly string USER_ID_PARSE_FAIL = "Wrong id";
        private static readonly string ITEM_NOT_FOUND = "Item not found";
        private static readonly string ITEM_NOT_AUTHORIZED = "Item not authorized";
        private static Guid GetUserIdOrEmpty(ClaimsPrincipal user) => Guid.TryParse(user.FindFirstValue(JwtRegisteredClaimNames.Jti), out Guid id) ? id : Guid.Empty;

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
            Guid id = GetUserIdOrEmpty(User);
            if (id == Guid.Empty)
            {
                return BadRequest(new { error_message = USER_ID_PARSE_FAIL });
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

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetSingleAsync(int itemId)
        {
            Guid id = GetUserIdOrEmpty(User);
            if (id == Guid.Empty)
            {
                return BadRequest(new { error_message = USER_ID_PARSE_FAIL });
            }

            TodoItem? item = await _todoItemService.GetByIdOrNullAsync(itemId);
            if (item == null)
            {
                return NotFound(new { error_message = ITEM_NOT_FOUND });
            }

            if (item.UserId != id)
            {
                return Unauthorized(new { error_message = ITEM_NOT_AUTHORIZED });
            }

            return Ok(new { item });
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> PostAsync([FromForm] string name)
        {
            Guid id = GetUserIdOrEmpty(User);
            if (id == Guid.Empty)
            {
                return BadRequest(new { error_message = USER_ID_PARSE_FAIL });
            }

            int itemId = await _todoItemService.AddAsync(name, id);
            return CreatedAtAction("GetSingle", "TodoItem", routeValues: new { itemId }, value: null);
        }

        [HttpPatch]
        [Authorize]
        public async Task<IActionResult> PatchAsync([FromForm] int itemId, [FromForm] string newName)
        {
            Guid id = GetUserIdOrEmpty(User);
            if (id == Guid.Empty)
            {
                return BadRequest(new { error_message = USER_ID_PARSE_FAIL });
            }

            TodoItem? item = await _todoItemService.GetByIdOrNullAsync(itemId);

            if (item == null)
            {
                return NotFound(new { error_message = ITEM_NOT_FOUND });
            }

            if (item.UserId != id)
            {
                return Unauthorized(new { error_message = ITEM_NOT_AUTHORIZED });
            }

            await _todoItemService.EditNameAsync(itemId, newName);

            return Ok(new { itemId, name = newName });
        }

        [HttpDelete]
        [Authorize]
        public async Task<IActionResult> DeleteAsync([FromForm] int itemId)
        {
            Guid id = GetUserIdOrEmpty(User);
            if (id == Guid.Empty)
            {
                return BadRequest(new { error_message = USER_ID_PARSE_FAIL });
            }

            TodoItem? item = await _todoItemService.GetByIdOrNullAsync(itemId);

            if (item == null)
            {
                return NotFound(new { error_message = ITEM_NOT_FOUND });
            }

            if (item.UserId != id)
            {
                return Unauthorized(new { error_message = ITEM_NOT_AUTHORIZED });
            }

            await _todoItemService.DeleteAsync(itemId);

            return Ok(new { itemId });
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> ToggleCompleteAsync([FromForm] int itemId)
        {
            Guid id = GetUserIdOrEmpty(User);
            if (id == Guid.Empty)
            {
                return BadRequest(new { error_message = USER_ID_PARSE_FAIL });
            }

            TodoItem? item = await _todoItemService.GetByIdOrNullAsync(itemId);
            if (item == null)
            {
                return NotFound(new { error_message = ITEM_NOT_FOUND });
            }

            if (item.UserId != id)
            {
                return Unauthorized(new { error_message = ITEM_NOT_AUTHORIZED });
            }

            if (item.IsComplete)
            {
                await _todoItemService.UncompleteAsync(itemId);
            }
            else
            {
                await _todoItemService.CompleteAsync(itemId);
            }

            return Ok(new { itemId, isComplete = !item.IsComplete });
        }
    }
}
