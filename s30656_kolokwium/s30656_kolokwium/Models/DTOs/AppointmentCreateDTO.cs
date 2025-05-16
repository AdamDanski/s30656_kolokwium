using System.ComponentModel.DataAnnotations;

namespace s30656_kolokwium.Models.DTOs;

public class AppointmentCreateDTO
{
    [Range(0, Int32.MaxValue)]
    public required int AppointmentId { get; set; }
    [Range(0, Int32.MaxValue)]
    public required int PatientId { get; set; }
    [MaxLength(50)]
    public required string Pwz { get; set; }
    
    public List<AppointmentServicesDTO> Services { get; set; }
    
}