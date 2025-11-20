using System;
using System.Security.Claims;
using System.Threading.Tasks;
using ArtemisBanking.Application.CreditCards.Dtos;
using ArtemisBanking.Application.CreditCards.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArtemisBanking.WebApi.Controllers
{
    [ApiController]
    [Route("api/credit-cards")]
    [Authorize(Roles = "Admin,Cashier")]
    public class CreditCardsController : ControllerBase
    {
        private readonly ICreditCardService _creditCardService;

        public CreditCardsController(ICreditCardService creditCardService)
        {
            _creditCardService = creditCardService;
        }

        [HttpGet]
        public async Task<IActionResult> GetActiveCards(
            [FromQuery] int pageIndex = 1,
            [FromQuery] int pageSize = 20)
        {
            var result = await _creditCardService.GetActiveCardsAsync(pageIndex, pageSize);
            return Ok(result);
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchCards(
            [FromQuery] string nationalId,
            [FromQuery] int pageIndex = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? status = null)
        {
            var result = await _creditCardService.SearchCardsByNationalIdAsync(
                nationalId,
                pageIndex,
                pageSize,
                status);

            return Ok(result);
        }

        [HttpGet("assignable-clients")]
        public async Task<IActionResult> GetAssignableClients()
        {
            var result = await _creditCardService.GetAssignableClientsAsync();
            if (!result.Succeeded)
                return BadRequest(result.Error);

            return Ok(result.Data);
        }

        [HttpPost]
        public async Task<IActionResult> AssignCard([FromBody] AssignCreditCardRequestDto dto)
        {
            var adminUserId = GetCurrentUserId();
            if (adminUserId == Guid.Empty)
                return Unauthorized("No se pudo determinar el usuario actual.");

            var result = await _creditCardService.AssignCardAsync(dto, adminUserId);
            if (!result.Succeeded)
                return BadRequest(result.Error);

            return Ok(result.Data);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetCardDetails(Guid id)
        {
            var result = await _creditCardService.GetCardDetailsAsync(id);
            if (!result.Succeeded)
                return NotFound(result.Error);

            return Ok(result.Data);
        }

        [HttpPut("{id:guid}/limit")]
        public async Task<IActionResult> UpdateLimit(
            Guid id,
            [FromBody] UpdateCreditCardLimitDto dto)
        {
            var result = await _creditCardService.UpdateLimitAsync(id, dto.NewLimit);
            if (!result.Succeeded)
                return BadRequest(result.Error);

            return NoContent();
        }

        [HttpPost("{id:guid}/cancel")]
        public async Task<IActionResult> CancelCard(Guid id)
        {
            var result = await _creditCardService.CancelCardAsync(id);
            if (!result.Succeeded)
                return BadRequest(result.Error);

            return NoContent();
        }

        private Guid GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)
                        ?? User.FindFirst("sub");

            if (claim == null)
                return Guid.Empty;

            return Guid.TryParse(claim.Value, out var id)
                ? id
                : Guid.Empty;
        }
    }
}
