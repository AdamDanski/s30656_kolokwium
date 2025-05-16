using System.ComponentModel.DataAnnotations;

namespace s30656_kolokwium.Models.DTOs;

public class AppointmentServicesDTO
{
    [MaxLength(50)]
    public string ServiceName { get; set; }
    public double ServiceFee { get; set; }
}