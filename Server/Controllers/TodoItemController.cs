using AutoMapper;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

using TodoList.Shared.Data.Dtos;
using TodoList.Shared.Data.Models;
using TodoList.Shared.Models;
using TodoList.Shared.Svcs.Interfaces;

namespace TodoList.Server.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class TodoItemController : ControllerBase
    {
        private static readonly string USER_ID_PARSE_FAIL = "Wrong id";
        private static readonly string ITEM_NOT_FOUND = "Item not found";

        private static Guid GetUserIdOrEmpty(ClaimsPrincipal user) => Guid.TryParse(user.FindFirstValue(JwtRegisteredClaimNames.Jti), out Guid id) ? id : Guid.Empty;

        private readonly ITodoItemService _todoItemService;
        private readonly ILogger<TodoItemController> _logger;
        private readonly IMapper _mapper;

        public TodoItemController(ITodoItemService todoItemService, ILogger<TodoItemController> logger, IMapper mapper)
        {
            _todoItemService = todoItemService;
            _logger = logger;
            _mapper = mapper;
        }

        [HttpGet]
        [Authorize]
        public IActionResult Get()
        {
            Response<IEnumerable<TodoItemDto>> response = new();

            Guid id = GetUserIdOrEmpty(User);
            if (id == Guid.Empty)
            {
                response.IsSuccess = false;
                response.Message = USER_ID_PARSE_FAIL;
                return BadRequest(response);
            }

            IEnumerable<TodoItem> items = _todoItemService.GetByUserId(id);

            IEnumerable<TodoItemDto> itemDtos = _mapper.Map<IEnumerable<TodoItem>, IEnumerable<TodoItemDto>>(items);
            response.Data = itemDtos;
            response.IsSuccess = true;

            return Ok(response);
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetSingleAsync(int itemId)
        {
            Response<TodoItemDto> response = new();

            Guid id = GetUserIdOrEmpty(User);
            if (id == Guid.Empty)
            {
                response.IsSuccess = false;
                response.Message = USER_ID_PARSE_FAIL;
                return BadRequest(response);
            }

            TodoItem? item = await _todoItemService.GetByIdOrNullAsync(itemId);

            if (item == null)
            {
                response.IsSuccess = false;
                response.Message = ITEM_NOT_FOUND;
                return NotFound(response);
            }

            if (item.UserId != id)
            {
                return Forbid(JwtBearerDefaults.AuthenticationScheme);
            }

            TodoItemDto itemDto = _mapper.Map<TodoItem, TodoItemDto>(item);
            response.Data = itemDto;
            response.IsSuccess = true;

            return Ok(response);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> PostAsync([FromForm] string name)
        {
            Response<object> response = new();

            Guid id = GetUserIdOrEmpty(User);
            if (id == Guid.Empty)
            {
                response.IsSuccess = false;
                response.Message = USER_ID_PARSE_FAIL;
                return BadRequest(response);
            }

            int itemId = await _todoItemService.AddAsync(id, name);
            return CreatedAtAction("GetSingle", "TodoItem", routeValues: new { itemId }, value: null);
        }

        [HttpPatch]
        [Authorize]
        public async Task<IActionResult> PatchAsync([FromForm] int itemId, [FromForm] string newName)
        {
            Response<object> response = new();

            Guid id = GetUserIdOrEmpty(User);
            if (id == Guid.Empty)
            {
                response.IsSuccess = false;
                response.Message = USER_ID_PARSE_FAIL;
                return BadRequest(response);
            }

            TodoItem? item = await _todoItemService.GetByIdOrNullAsync(itemId);

            if (item == null)
            {
                response.IsSuccess = false;
                response.Message = ITEM_NOT_FOUND;
                return NotFound(response);
            }

            if (item.UserId != id)
            {
                return Forbid(JwtBearerDefaults.AuthenticationScheme);
            }

            await _todoItemService.EditNameAsync(itemId, newName);
            response.Data = new { itemId, name = newName };

            return Ok(response);
        }

        [HttpDelete]
        [Authorize]
        public async Task<IActionResult> DeleteAsync([FromForm] int itemId)
        {
            Response<object> response = new();

            Guid id = GetUserIdOrEmpty(User);
            if (id == Guid.Empty)
            {
                response.IsSuccess = false;
                response.Message = USER_ID_PARSE_FAIL;
                return BadRequest(response);
            }

            TodoItem? item = await _todoItemService.GetByIdOrNullAsync(itemId);

            if (item == null)
            {
                response.IsSuccess = false;
                response.Message = ITEM_NOT_FOUND;
                return NotFound(response);
            }

            if (item.UserId != id)
            {
                return Forbid(JwtBearerDefaults.AuthenticationScheme);
            }

            await _todoItemService.DeleteAsync(itemId);

            response.Data = new { itemId };
            return Ok(response);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> ToggleCompleteAsync([FromForm] int itemId)
        {
            Response<object> response = new();

            Guid id = GetUserIdOrEmpty(User);
            if (id == Guid.Empty)
            {
                response.IsSuccess = false;
                response.Message = USER_ID_PARSE_FAIL;
                return BadRequest(response);
            }

            TodoItem? item = await _todoItemService.GetByIdOrNullAsync(itemId);

            if (item == null)
            {
                response.IsSuccess = false;
                response.Message = ITEM_NOT_FOUND;
                return NotFound(response);
            }

            if (item.UserId != id)
            {
                return Forbid(JwtBearerDefaults.AuthenticationScheme);
            }

            if (item.IsComplete)
            {
                await _todoItemService.UncompleteAsync(itemId);
            }
            else
            {
                await _todoItemService.CompleteAsync(itemId);
            }

            response.Data = new { itemId, isComplete = !item.IsComplete };

            return Ok(response);
        }
    }
}
