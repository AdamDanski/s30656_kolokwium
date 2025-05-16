namespace s30656_kolokwium.Models.DTOs;

public class AppointmentGetDTO
{
    public DateTime Date { get; set; }
    public PatientGetDTO Patient { get; set; }
    public DoctorGetDTO Doctor { get; set; }
    public List<AppointmentServicesDTO> AppointmentServices { get; set; }
}