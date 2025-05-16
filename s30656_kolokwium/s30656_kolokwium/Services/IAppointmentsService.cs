using s30656_kolokwium.Models.DTOs;

namespace s30656_kolokwium.Services;

public interface IAppointmentsService
{
    public Task<AppointmentGetDTO> GetAppointmentByIdAsync(int id);
    public Task<AppointmentGetDTO> CreateAppointmentAsync(AppointmentCreateDTO data);
}