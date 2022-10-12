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
    [ApiController]
    [Produces("application/json")]
    [Route("api/[controller]/[action]")]
    public class TodoItemController : ControllerBase
    {
        private static readonly Response ITEM_NOT_FOUND_RESPONSE = new(EErrorCode.TodoItemNotFound);

        private readonly ITodoItemService _todoItemService;
        private readonly ILogger<TodoItemController> _logger;
        private readonly IMapper _mapper;

        public TodoItemController(ITodoItemService todoItemService, ILogger<TodoItemController> logger, IMapper mapper)
        {
            _todoItemService = todoItemService;
            _logger = logger;
            _mapper = mapper;
        }

        /// <summary>
        /// 유저가 작성한 아이템 리스트 또는 단일 아이템을 반환합니다.
        /// </summary>
        /// <param name="itemId"></param>
        /// <returns></returns>
        [HttpGet]
        [Authorize]
        [ProducesResponseType(typeof(Response<TodoItemDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Response<IEnumerable<TodoItemDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(Response), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetAsync(int? itemId)
        {
            Guid id = GetUserId(User);
            if (itemId == null)
            {
                IEnumerable<TodoItem> items = _todoItemService.GetByUserId(id);
                IEnumerable<TodoItemDto> itemDtos = _mapper.Map<IEnumerable<TodoItem>, IEnumerable<TodoItemDto>>(items);
                return Ok(new Response<IEnumerable<TodoItemDto>>(EErrorCode.NoError, itemDtos));
            }
            else
            {
                TodoItem? item = await _todoItemService.GetByIdOrNullAsync(itemId.Value);
                if (item == null)
                {
                    return NotFound(ITEM_NOT_FOUND_RESPONSE);
                }

                if (item.UserId != id)
                {
                    return Forbid(JwtBearerDefaults.AuthenticationScheme);
                }

                TodoItemDto itemDto = _mapper.Map<TodoItem, TodoItemDto>(item);
                return Ok(new Response<TodoItemDto>(EErrorCode.NoError, itemDto));
            }
        }

        /// <summary>
        /// 새 아이템을 생성합니다.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        [HttpPost]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status201Created)]
        public IActionResult Post([FromForm] string name)
        {
            Guid id = GetUserId(User);

            int itemId = _todoItemService.AddAsync(id, name).Result;
            return CreatedAtAction("Get", "TodoItem", routeValues: new { itemId }, value: null);
        }

        /// <summary>
        /// 해당하는 아이템을 삭제합니다.
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        [HttpPatch]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(Response), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> PatchAsync([FromBody] TodoItemUpdateInfo info)
        {
            if (info.NewName is null)
            {
                ModelState.AddModelError(nameof(TodoItemUpdateInfo.NewName), new ArgumentNullException(nameof(TodoItemUpdateInfo.NewName)), MetadataProvider.GetMetadataForType(typeof(TodoItemUpdateInfo)));
                return UnprocessableEntity(ModelState);
            }

            Guid id = GetUserId(User);

            TodoItem? item = await _todoItemService.GetByIdOrNullAsync(info.ItemId);

            if (item == null)
            {
                return NotFound(ITEM_NOT_FOUND_RESPONSE);
            }

            if (item.UserId != id)
            {
                return Forbid(JwtBearerDefaults.AuthenticationScheme);
            }

            await _todoItemService.EditNameAsync(info.ItemId, info.NewName);
            return Ok();
        }

        /// <summary>
        /// 해당하는 아이템을 삭제합니다.
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        [HttpDelete]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(Response), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteAsync([FromBody] TodoItemUpdateInfo info)
        {
            Guid id = GetUserId(User);

            TodoItem? item = await _todoItemService.GetByIdOrNullAsync(info.ItemId);

            if (item == null)
            {
                return NotFound(ITEM_NOT_FOUND_RESPONSE);
            }

            if (item.UserId != id)
            {
                return Forbid(JwtBearerDefaults.AuthenticationScheme);
            }

            await _todoItemService.DeleteAsync(info.ItemId);
            return Ok();
        }

        /// <summary>
        /// 해당하는 아이템의 완료 여부를 토글합니다.
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        [HttpPost]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(Response), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ToggleCompleteAsync([FromBody] TodoItemUpdateInfo info)
        {
            Guid id = GetUserId(User);
            TodoItem? item = await _todoItemService.GetByIdOrNullAsync(info.ItemId);

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
                await _todoItemService.UncompleteAsync(info.ItemId);
            }
            else
            {
                await _todoItemService.CompleteAsync(info.ItemId);
            }
            return Ok();
        }

        private static Guid GetUserId(ClaimsPrincipal user) => Guid.Parse(user.FindFirstValue(JwtRegisteredClaimNames.Jti));
    }
}
