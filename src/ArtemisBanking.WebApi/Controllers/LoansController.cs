using System.Security.Claims;
using ArtemisBanking.Application.Common;
using ArtemisBanking.Application.Loans;
using ArtemisBanking.Application.Loans.Dtos;
using ArtemisBanking.Core.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArtemisBanking.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin,Cashier")]
    public class LoansController : ControllerBase
    {
        private readonly ILoanService _loanService;

        public LoansController(ILoanService loanService)
        {
            _loanService = loanService;
        }

        [HttpGet]
        public async Task<IActionResult> GetActiveLoans(
            [FromQuery] int pageIndex = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken cancellationToken = default)
        {
            var result = await _loanService.GetActiveLoansAsync(pageIndex, pageSize, cancellationToken);
            return Ok(result);
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchByNationalId(
            [FromQuery] string nationalId,
            [FromQuery] LoanStatus? status = null,
            [FromQuery] int pageIndex = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(nationalId))
            {
                return BadRequest("Debe especificar la cédula para buscar.");
            }

            var result = await _loanService.SearchLoansByNationalIdAsync(
                nationalId,
                status,
                pageIndex,
                pageSize,
                cancellationToken);

            return Ok(result);
        }

        [HttpGet("assignable-clients")]
        public async Task<IActionResult> GetAssignableClients(
            [FromQuery] string? nationalId = null,
            CancellationToken cancellationToken = default)
        {
            var result = await _loanService.GetAssignableClientsAsync(nationalId, cancellationToken);

            if (!result.Succeeded)
            {
                return BadRequest(result.Error);
            }

            return Ok(result.Data);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetLoanDetails(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            var result = await _loanService.GetLoanDetailsAsync(id, cancellationToken);

            if (!result.Succeeded)
            {
                return NotFound(result.Error);
            }

            return Ok(result.Data);
        }

        [HttpPost]
        public async Task<IActionResult> AssignLoan(
            [FromBody] AssignLoanRequestDto dto,
            CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var adminUserId = GetCurrentUserId();
            if (adminUserId == null)
            {
                return Unauthorized("No se pudo determinar el usuario actual.");
            }

            var result = await _loanService.AssignLoanAsync(dto, adminUserId.Value, cancellationToken);

            if (!result.Succeeded)
            {
                return BadRequest(result.Error);
            }

            return CreatedAtAction(
                nameof(GetLoanDetails),
                new { id = result.Data.Id },
                result.Data);
        }

        [HttpPut("{id:guid}/rate")]
        public async Task<IActionResult> UpdateAnnualRate(
            Guid id,
            [FromBody] UpdateLoanRateDto dto,
            CancellationToken cancellationToken = default)
        {
            if (dto.NewAnnualRate <= 0)
            {
                return BadRequest("La nueva tasa anual debe ser mayor que cero.");
            }

            var result = await _loanService.UpdateAnnualRateAsync(id, dto.NewAnnualRate, cancellationToken);

            if (!result.Succeeded)
            {
                return BadRequest(result.Error);
            }

            return Ok(result.Data);
        }

        [HttpPost("run-overdue-job")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RunOverdueJob(CancellationToken cancellationToken = default)
        {
            var result = await _loanService.MarkOverdueInstallmentsAsync(cancellationToken);

            if (!result.Succeeded)
            {
                return BadRequest(result.Error);
            }

            return NoContent();
        }

        private Guid? GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
            if (claim == null)
            {
                return null;
            }

            return Guid.TryParse(claim.Value, out var id) ? id : null;
        }
    }
}
