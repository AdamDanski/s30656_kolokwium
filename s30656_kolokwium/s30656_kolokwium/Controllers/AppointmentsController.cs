using Microsoft.AspNetCore.Mvc;
using s30656_kolokwium.Services;
using s30656_kolokwium.Exceptions;
using s30656_kolokwium.Models.DTOs;

namespace Tutorial8.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AppointmentsController : ControllerBase
    {
        private readonly IAppointmentsService _appointmentsService;

        public AppointmentsController(IAppointmentsService appointmentsService)
        {
            _appointmentsService = appointmentsService;
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> GetAppointmentById([FromRoute]int id)
        {
            try
            {
                return Ok(await _appointmentsService.GetAppointmentByIdAsync(id));
            }
            catch (NotFoundException e)
            {
                return NotFound(e.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateAppointmentAsync([FromBody] AppointmentCreateDTO appointmentCreateDto)
        {
            try
            {
                var app = await _appointmentsService.CreateAppointmentAsync(appointmentCreateDto);
                return Created($"appointments/{appointmentCreateDto.AppointmentId}", app);
            }
            catch (NotFoundException e)
            {
                return NotFound(e.Message);
            }
        }
    }
}
