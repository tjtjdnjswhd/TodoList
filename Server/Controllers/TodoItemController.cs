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

            Guid id = GetUserIdOrEmpty(User);
            if (id == Guid.Empty)
            {
                return BadRequest(new Response()
                {
                    IsSuccess = false,
                    Message = USER_ID_PARSE_FAIL
                });
            }

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
            Guid id = GetUserIdOrEmpty(User);
            if (id == Guid.Empty)
            {
                return BadRequest(new Response()
                {
                    IsSuccess = false,
                    Message = USER_ID_PARSE_FAIL
                });
            }

            TodoItem? item = await _todoItemService.GetByIdOrNullAsync(itemId);

            if (item == null)
            {
                return NotFound(new Response()
                {
                    IsSuccess = false,
                    Message = ITEM_NOT_FOUND
                });
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
        public async Task<IActionResult> PostAsync([FromForm] string name)
        {
            Guid id = GetUserIdOrEmpty(User);
            if (id == Guid.Empty)
            {
                return BadRequest(new Response()
                {
                    IsSuccess = false,
                    Message = USER_ID_PARSE_FAIL
                });
            }

            int itemId = await _todoItemService.AddAsync(id, name);
            return CreatedAtAction("GetSingle", "TodoItem", routeValues: new { itemId }, value: null);
        }

        [HttpPatch]
        [Authorize]
        public async Task<IActionResult> PatchAsync([FromForm] int itemId, [FromForm] string newName)
        {

            Guid id = GetUserIdOrEmpty(User);
            if (id == Guid.Empty)
            {
                return BadRequest(new Response()
                {
                    IsSuccess = false,
                    Message = USER_ID_PARSE_FAIL
                });
            }

            TodoItem? item = await _todoItemService.GetByIdOrNullAsync(itemId);

            if (item == null)
            {
                return NotFound(new Response()
                {
                    IsSuccess = false,
                    Message = ITEM_NOT_FOUND
                });
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
        public async Task<IActionResult> DeleteAsync([FromForm] int itemId)
        {
            Guid id = GetUserIdOrEmpty(User);
            if (id == Guid.Empty)
            {
                return BadRequest(new Response()
                {
                    IsSuccess = false,
                    Message = USER_ID_PARSE_FAIL
                });
            }

            TodoItem? item = await _todoItemService.GetByIdOrNullAsync(itemId);

            if (item == null)
            {
                return NotFound(new Response()
                {
                    IsSuccess = false,
                    Message = ITEM_NOT_FOUND
                });
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

            Guid id = GetUserIdOrEmpty(User);
            if (id == Guid.Empty)
            {
                return BadRequest(new Response()
                {
                    IsSuccess = false,
                    Message = USER_ID_PARSE_FAIL
                });
            }

            TodoItem? item = await _todoItemService.GetByIdOrNullAsync(itemId);

            if (item == null)
            {
                return NotFound(new Response()
                {
                    IsSuccess = false,
                    Message = ITEM_NOT_FOUND
                });
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
