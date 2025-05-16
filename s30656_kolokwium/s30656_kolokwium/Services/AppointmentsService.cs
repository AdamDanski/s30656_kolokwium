using System.Data;
using s30656_kolokwium.Exceptions;
using s30656_kolokwium.Models.DTOs;
using Microsoft.Data.SqlClient;

namespace s30656_kolokwium.Services;

public class AppointmentsService : IAppointmentsService
{
    private readonly IConfiguration _configuration;

    public AppointmentsService(IConfiguration configuration)
    {
        _configuration = configuration;
    }
    private async Task<SqlConnection> GetConnectionAsync()
    {
        var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync();
        }
        return connection;
    }
    public async Task<AppointmentGetDTO> GetAppointmentByIdAsync(int id)
    {
        await using var connection = await GetConnectionAsync();
        var sql = """
                  SELECT A.date, P.first_name, P.last_name, P.date_of_birth, D.doctor_id, D.PWZ, S.name, ASS.service_fee
                  FROM Appointment A
                  JOIN Patient P on P.patient_id=A.patient_id
                  JOIN Doctor D on D.doctor_id=A.doctor_id
                  JOIN Appointment_Service ASS ON ASS.appoitment_id=A.appoitment_id
                  JOIN Service S on S.service_id=ASS.service_id
                  WHERE A.appoitment_id = @id;
                  """;
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", id);
        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            throw new NotFoundException($"Appointment not found of {id}");
        }
        var appointment = new AppointmentGetDTO
        {
            Date = reader.GetDateTime(0),
            Patient = new PatientGetDTO
            {
                FirstName = reader.GetString(1),
                LastName = reader.GetString(2),
                DateOfBirth = reader.GetDateTime(3),
            },
            Doctor = new DoctorGetDTO
            {
                DoctorId = reader.GetInt32(4),
                Pwz = reader.GetString(5),
            },
            AppointmentServices = new List<AppointmentServicesDTO>()
        };
        appointment.AppointmentServices.Add(new AppointmentServicesDTO
        {
            ServiceName = reader.GetString(6),
            ServiceFee = Convert.ToDouble(reader.GetDecimal(7))
        });
        while (await reader.ReadAsync())
        {
            appointment.AppointmentServices.Add(new AppointmentServicesDTO
            {
                ServiceName = reader.GetString(6),
                ServiceFee = Convert.ToDouble(reader.GetDecimal(7))
            });
        }
        return appointment;
    }

    public async Task<AppointmentGetDTO> CreateAppointmentAsync(AppointmentCreateDTO data)
    {
        await using var connection = await GetConnectionAsync();
        var checkAppointmentSql = """
                                  SELECT 1 FROM Appointment A
                                  WHERE appoitment_id = @id
                                  """;
        await using (var chceckCommand = new SqlCommand(checkAppointmentSql, connection))
        {
            chceckCommand.Parameters.AddWithValue("@Id", data.AppointmentId);
            var exists = await chceckCommand.ExecuteScalarAsync();
            if (exists is not null)
            {
                throw new NotFoundException($"Appointment with {data.AppointmentId} exists");
            }
        }

        PatientGetDTO patientBody;
        var checkPatientSql = "SELECT first_name, last_name, date_of_birth FROM Patient WHERE patient_id = @id";
        await using (var checkPatientCommand = new SqlCommand(checkPatientSql, connection))
        {
            checkPatientCommand.Parameters.AddWithValue("@id", data.PatientId);
            await using var patientReader = await checkPatientCommand.ExecuteReaderAsync();
            if (!await patientReader.ReadAsync())
            {
                throw new NotFoundException($"Patient with {data.PatientId} not found");
            }

            patientBody = new PatientGetDTO
            {
                FirstName = patientReader.GetString(0),
                LastName = patientReader.GetString(1),
                DateOfBirth = patientReader.GetDateTime(2),
            };

        }

        int doctorId;
        var checkDoctorSql = "SELECT doctor_id, PWZ FROM Doctor WHERE PWZ = @pwz";
        DoctorGetDTO doctorBody;
        await using (var checkDoctorCommand = new SqlCommand(checkDoctorSql, connection))
        {
            checkDoctorCommand.Parameters.AddWithValue("@pwz", data.Pwz);
    
            await using var result = await checkDoctorCommand.ExecuteReaderAsync(); 
            if (!await result.ReadAsync())
            {
                throw new NotFoundException($"Doctor with PWZ {data.Pwz} not found");
            }

            doctorId = result.GetInt32(0);
            doctorBody = new DoctorGetDTO
            {
                DoctorId = doctorId,
                Pwz = data.Pwz
            };
        }
        var serviceIds = new List<int>();

        foreach (var service in data.Services)
        {
            var checkServiceSql = "SELECT service_id FROM Service WHERE name = @name";
            await using (var checkServiceCommand = new SqlCommand(checkServiceSql, connection))
            {
                checkServiceCommand.Parameters.AddWithValue("@name", service.ServiceName);
                var result = await checkServiceCommand.ExecuteScalarAsync();
                if (result is null)
                {
                    throw new NotFoundException($"Service with {service.ServiceName} not found");
                }
                serviceIds.Add(Convert.ToInt32(result));
            }
        }
        await using var transaction = await connection.BeginTransactionAsync();
        var insertAppointmentSql = """
                                   INSERT INTO Appointment (appoitment_id, date, patient_id, doctor_id)
                                   VALUES (@id, @date, @patientId, @doctorId)
                                   """;
        await using (var insertAppointmentCommand =
                     new SqlCommand(insertAppointmentSql, connection, (SqlTransaction)transaction))
        {
            insertAppointmentCommand.Parameters.AddWithValue("@id", data.AppointmentId);
            insertAppointmentCommand.Parameters.AddWithValue("@date", DateTime.Now);
            insertAppointmentCommand.Parameters.AddWithValue("@patientId", data.PatientId);
            insertAppointmentCommand.Parameters.AddWithValue("@doctorId", doctorId);
            
            await insertAppointmentCommand.ExecuteNonQueryAsync();
        }

        for (int i = 0; i < serviceIds.Count; i++)
        {
            var insertService = """
                                INSERT INTO Appointment_Service (appoitment_id, service_id, service_fee)
                                VALUES (@appointmentId, @serviceId, @fee)
                                """;

            await using (var insertServiceCommand =
                         new SqlCommand(insertService, connection, (SqlTransaction)transaction))
            {
                insertServiceCommand.Parameters.AddWithValue("@appointmentId", data.AppointmentId);
                insertServiceCommand.Parameters.AddWithValue("@serviceId", serviceIds[i]);
                insertServiceCommand.Parameters.AddWithValue("@fee", data.Services[i].ServiceFee);
                
                await insertServiceCommand.ExecuteNonQueryAsync();
            }
        }
        await transaction.CommitAsync();
        return new AppointmentGetDTO
        {
            Date = DateTime.Now,
            Patient = patientBody,
            Doctor = doctorBody,

            AppointmentServices = data.Services.Select(s => new AppointmentServicesDTO
            {
                ServiceName = s.ServiceName,
                ServiceFee = s.ServiceFee
            }).ToList()
        };
    }
}