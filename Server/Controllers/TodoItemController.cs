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
        private static readonly Response ITEM_NOT_FOUND_RESPONSE = new()
        {
            IsSuccess = false,
            ErrorCode = EErrorCode.TodoItemNotFound
        };

        private static Guid GetUserId(ClaimsPrincipal user) => Guid.Parse(user.FindFirstValue(JwtRegisteredClaimNames.Jti));

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
            Guid id = GetUserId(User);

            IEnumerable<TodoItem> items = _todoItemService.GetByUserId(id);

            IEnumerable<TodoItemDto> itemDtos = _mapper.Map<IEnumerable<TodoItem>, IEnumerable<TodoItemDto>>(items);
            Response<IEnumerable<TodoItemDto>> response = new()
            {
                Data = itemDtos,
                IsSuccess = true
            };

            return Ok(response);
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetSingleAsync(int itemId)
        {
            Guid id = GetUserId(User);

            TodoItem? item = await _todoItemService.GetByIdOrNullAsync(itemId);

            if (item == null)
            {
                return NotFound(ITEM_NOT_FOUND_RESPONSE);
            }

            if (item.UserId != id)
            {
                return Forbid(JwtBearerDefaults.AuthenticationScheme);
            }

            TodoItemDto itemDto = _mapper.Map<TodoItem, TodoItemDto>(item);
            Response<TodoItemDto> response = new()
            {
                Data = itemDto,
                IsSuccess = true
            };

            return Ok(response);
        }

        [HttpPost]
        [Authorize]
        public IActionResult Post([FromForm] string name)
        {
            Guid id = GetUserId(User);

            int itemId = _todoItemService.AddAsync(id, name).Result;
            return CreatedAtAction("GetSingle", "TodoItem", routeValues: new { itemId }, value: null);
        }

        [HttpPatch]
        [Authorize]
        public async Task<IActionResult> PatchAsync([FromForm] int itemId, [FromForm] string newName)
        {

            Guid id = GetUserId(User);

            TodoItem? item = await _todoItemService.GetByIdOrNullAsync(itemId);

            if (item == null)
            {
                return NotFound(ITEM_NOT_FOUND_RESPONSE);
            }

            if (item.UserId != id)
            {
                return Forbid(JwtBearerDefaults.AuthenticationScheme);
            }

            await _todoItemService.EditNameAsync(itemId, newName);

            Response<object> response = new()
            {
                Data = new { itemId, name = newName },
                IsSuccess = true
            };

            return Ok(response);
        }

        [HttpDelete]
        [Authorize]
        public async Task<IActionResult> DeleteAsync(int itemId)
        {
            Guid id = GetUserId(User);

            TodoItem? item = await _todoItemService.GetByIdOrNullAsync(itemId);

            if (item == null)
            {
                return NotFound(ITEM_NOT_FOUND_RESPONSE);
            }

            if (item.UserId != id)
            {
                return Forbid(JwtBearerDefaults.AuthenticationScheme);
            }

            await _todoItemService.DeleteAsync(itemId);

            Response<object> response = new()
            {
                Data = new { itemId },
                IsSuccess = true
            };

            return Ok(response);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> ToggleCompleteAsync([FromForm] int itemId)
        {
            Guid id = GetUserId(User);

            TodoItem? item = await _todoItemService.GetByIdOrNullAsync(itemId);

            if (item == null)
            {
                return NotFound(ITEM_NOT_FOUND_RESPONSE);
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

            Response<object> response = new()
            {
                Data = new { itemId, isComplete = !item.IsComplete },
                IsSuccess = true
            };

            return Ok(response);
        }
    }
}
