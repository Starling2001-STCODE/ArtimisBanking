using System;
using System.Threading;
using System.Threading.Tasks;
using ArtemisBanking.Application.Common;
using ArtemisBanking.Application.Users;
using ArtemisBanking.Application.Users.Dtos;
using ArtemisBanking.Core.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArtemisBanking.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        // GET: api/users?pageIndex=1&pageSize=10&role=Admin
        [HttpGet]
        public async Task<IActionResult> GetUsers(
            [FromQuery] int pageIndex = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] UserRole? role = null,
            CancellationToken cancellationToken = default)
        {
            var result = await _userService.GetUsersAsync(pageIndex, pageSize, role, cancellationToken);

            // PagedResult<UserDto> no tiene Error, devolvemos 200 directo
            return Ok(result);
        }

        // GET: api/users/{id}
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            var result = await _userService.GetByIdAsync(id, cancellationToken);

            if (!result.Succeeded)
            {
                return NotFound(result.Error);
            }

            // 👇 aquí antes era result.Value
            return Ok(result.Data);
        }

        // POST: api/users
        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] CreateUserDto dto,
            CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _userService.CreateAsync(dto, cancellationToken);

            if (!result.Succeeded)
            {
                return BadRequest(result.Error);
            }

            return CreatedAtAction(
                nameof(GetById),
                new { id = result.Data.Id },
                result.Data
            );
        }

        // PUT: api/users/{id}
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(
            Guid id,
            [FromBody] UpdateUserDto dto,
            CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _userService.UpdateAsync(id, dto, cancellationToken);

            if (!result.Succeeded)
            {
                if (string.Equals(result.Error, "Usuario no encontrado.", StringComparison.OrdinalIgnoreCase))
                {
                    return NotFound(result.Error);
                }

                return BadRequest(result.Error);
            }

            return NoContent();
        }

        // PATCH: api/users/{id}/toggle-active
        [HttpPatch("{id:guid}/toggle-active")]
        public async Task<IActionResult> ToggleActive(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            var result = await _userService.ToggleActiveAsync(id, cancellationToken);

            if (!result.Succeeded)
            {
                if (string.Equals(result.Error, "Usuario no encontrado.", StringComparison.OrdinalIgnoreCase))
                {
                    return NotFound(result.Error);
                }

                return BadRequest(result.Error);
            }

            return NoContent();
        }
    }
}
