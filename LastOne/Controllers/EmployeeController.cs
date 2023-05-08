using LastOne.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Configuration;
using System.Web.Http;
using System.Web.Mvc;
using HttpPostAttribute = System.Web.Http.HttpPostAttribute;

namespace LastOne.Controllers
{
    public class EmployeeController : ApiController
    {

        [HttpPost]
        public IHttpActionResult CreateMatch(Employee employee)
        {
            try
            {
               
                var employers = GetAllEmployers();

               
                var scoredEmployers = ScoreEmployers(employers, employee);

               
                var matches = NrmpMatch(scoredEmployers,employee.Id);

               
                foreach (var match in matches)
                {
                    CreateMatchEntry(employee.Id, match.EmployerId);
                }

                return Ok();
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        private List<Employer> GetAllEmployers()
        {
            string sqlDataSource = WebConfigurationManager.ConnectionStrings["DBConnectionString"].ConnectionString;

            using (var connection = new SqlConnection(sqlDataSource))
            {
                var employers = new List<Employer>();

                var query = "SELECT employer_id, name, job_title, location, availability FROM Employer";
                using (var command = new SqlCommand(query, connection))
                {
                    connection.Open();
                    var reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        var employer = new Employer
                        {
                            Id = reader.GetInt32(0),
                            Name = reader.GetString(1),
                            JobTitle = reader.GetString(2),
                            Location = reader.GetString(3),
                            Availability = reader.GetBoolean(4)
                        };
                        employers.Add(employer);
                    }
                    reader.Close();
                }

                return employers;
            }
        }

        private List<Employer> ScoreEmployers(List<Employer> employers, Employee employee)
        {
            var scoredEmployers = new List<Employer>();

            foreach (var employer in employers)
            {
                var score = 0;

           
                var employerLocations = GetEmployerLocations(employer.Id);
                foreach (var location in employerLocations)
                {
                    if (employee.Locations.Contains(location))
                    {
                        score++;
                    }
                }

               
                var employerSkills = GetEmployerSkills(employer.Id);
                foreach (var skill in employerSkills)
                {
                    if (employee.Skills.Contains(skill))
                    {
                        score++;
                    }
                }


                if (employer.Availability == employee.Availability)
                {
                    score++;
                }
              


                var scoredEmployer = new Employer
                {
                    Id = employer.Id,
                    Score = score
                };
                scoredEmployers.Add(scoredEmployer);
            }

            return scoredEmployers;
        }

        private List<Skill> GetEmployerSkills(int employerId)
        {
            string sqlDataSource = WebConfigurationManager.ConnectionStrings["DBConnectionString"].ConnectionString;

            using (var connection = new SqlConnection(sqlDataSource))
            {
                var skills = new List<Skill>();

                var query = "SELECT s.name,s.skill_id FROM Skills s JOIN EmployerSkills es ON s.skill_id = es.skill_id WHERE es.employer_id = @EmployerId";
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@EmployerId", employerId);
                    connection.Open();
                    var reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        var newSkill = new Skill
                        {
                            Id = reader.GetInt32(0),
                            Name = reader.GetString(1),
                        };
 
                        skills.Add(newSkill);
                    }
                    reader.Close();
                }

                return skills;
            }
        }

        private List<Location> GetEmployerLocations(int employerId)
        {
            string sqlDataSource = WebConfigurationManager.ConnectionStrings["DBConnectionString"].ConnectionString;

            using (var connection = new SqlConnection(sqlDataSource))
            {
                var locations = new List<Location>();

                var query = "SELECT l.name FROM Locations l JOIN EmployerLocations el ON l.location_id = el.location_id WHERE el.employer_id = @EmployerId";
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@EmployerId", employerId);
                    connection.Open();
                    var reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        var newLocation = new Location
                        {
                            Id = reader.GetInt32(0),
                            Name = reader.GetString(1),
                        };
                        locations.Add(newLocation);
                    }
                    reader.Close();
                }

                return locations;
            }
        }


        public async Task<int> CreateEmployee(Employee employee)
        {
            int newEmployeeId = 0;
            string sqlDataSource = WebConfigurationManager.ConnectionStrings["DBConnectionString"].ConnectionString;
            using (SqlConnection connection = new SqlConnection(sqlDataSource))
            {
                await connection.OpenAsync();


                string sqlInsertEmployee = "INSERT INTO Employee (name, job_title, location, availability) VALUES (@Name, @JobTitle, @Location, @Availability); SELECT SCOPE_IDENTITY();";
                using (SqlCommand command = new SqlCommand(sqlInsertEmployee, connection))
                {
                    command.Parameters.AddWithValue("@Name", employee.Name);
                    command.Parameters.AddWithValue("@JobTitle", employee.JobTitle);
                    command.Parameters.AddWithValue("@Location", employee.Location);
                    command.Parameters.AddWithValue("@Availability", employee.Availability);
                    newEmployeeId = Convert.ToInt32(await command.ExecuteScalarAsync());
                }

                foreach (var skill in employee.Skills)
                {
                    string insertSkillQuery = "INSERT INTO EmployeeSkills (employee_id, skill_id) " +
                                               "VALUES (@EmployeeId, @SkillId)";
                    SqlCommand insertSkillCommand = new SqlCommand(insertSkillQuery, connection);
                    insertSkillCommand.Parameters.AddWithValue("EmployeeId", employee.Id);
                    insertSkillCommand.Parameters.AddWithValue("SkillId", skill.Id);
                    insertSkillCommand.ExecuteNonQuery();
                }

                foreach (var location in employee.Locations)
                {
                    string insertSkillQuery = "INSERT INTO EmployeeLocations (employee_id, location_id) " +
                                               "VALUES (@employee_id, @location_id)";
                    SqlCommand insertSkillCommand = new SqlCommand(insertSkillQuery, connection);
                    insertSkillCommand.Parameters.AddWithValue("employee_id", employee.Id);
                    insertSkillCommand.Parameters.AddWithValue("location_id", location.Id);
                    insertSkillCommand.ExecuteNonQuery();
                }
            }


            return newEmployeeId;
        }

        public static List<Match> NrmpMatch(List<Employer> scoredEmployers, int employeeId)
        {

            scoredEmployers.Sort((e1, e2) => e2.Score.CompareTo(e1.Score));

            var matches = new List<Match>();
            var usedIndices = new HashSet<int>();

            foreach (var employer in scoredEmployers)
            {
                for (int i = 0; i < scoredEmployers.Count; i++)
                {
                    if (!usedIndices.Contains(i))
                    {
                        var match = new Match
                        {
                            EmployeeId = employeeId,
                            EmployerId = scoredEmployers[i].Id
                        };

                        matches.Add(match);
                        usedIndices.Add(i);
                        break;
                    }
                }
            }

            return matches;
        }

        public void CreateMatchEntry(int employeeId, int employerId)
        {
            string sqlDataSource = WebConfigurationManager.ConnectionStrings["DBConnectionString"].ConnectionString;

            string query = "INSERT INTO Match (employee_id, employer_id, match_date, status) VALUES (@employeeId, @employerId, @matchDate, @status)";

            using (SqlConnection connection = new SqlConnection(sqlDataSource))
            {
                connection.Open();

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@employeeId", employeeId);
                    command.Parameters.AddWithValue("@employerId", employerId);
                    command.Parameters.AddWithValue("@matchDate", DBNull.Value);
                    command.Parameters.AddWithValue("@status", "Pending");

                    command.ExecuteNonQuery();
                }
            }
        }

    }
}