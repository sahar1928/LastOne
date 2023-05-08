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
    public class EmployerController : ApiController
    {

        [HttpPost]
        public IHttpActionResult CreateMatch(Employer employer)
        {
            try
            {
                
                var employees = GetAllEmployees();

                
                var scoredEmployees = ScoreEmployers(employees, employer);

               
                var matches = NrmpMatch(scoredEmployees, employer.Id);

                
                foreach (var match in matches)
                {
                    CreateMatchEntry(employer.Id, match.EmployerId);
                }

                return Ok();
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        private List<Employee> GetAllEmployees()
        {
            string sqlDataSource = WebConfigurationManager.ConnectionStrings["DBConnectionString"].ConnectionString;

            using (var connection = new SqlConnection(sqlDataSource))
            {
                var employees = new List<Employee>();

                var query = "SELECT employer_id, name, job_title, location, availability FROM Employer";
                using (var command = new SqlCommand(query, connection))
                {
                    connection.Open();
                    var reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        var employee = new Employee
                        {
                            Id = reader.GetInt32(0),
                            Name = reader.GetString(1),
                            JobTitle = reader.GetString(2),
                            Location = reader.GetString(3),
                            Availability = reader.GetBoolean(4)
                        };
                        employees.Add(employee);
                    }
                    reader.Close();
                }

                return employees;
            }
        }

        private List<Employer> ScoreEmployers(List<Employer> employees, Employer employer)
        {
            var scoredEmployers = new List<Employer>();

            foreach (var employee in employees)
            {
                var score = 0;

                
                var employerLocations = GetEmployerLocations(employee.Id);
                foreach (var location in employerLocations)
                {
                    if (employer.Locations.Contains(location))
                    {
                        score++;
                    }
                }

                
                var employeeSkills = GetEmployerSkills(employee.Id);
                foreach (var skill in employeeSkills)
                {
                    if (employer.RequiredSkills.Contains(skill))
                    {
                        score++;
                    }
                }

                
                if (employer.Availability == employer.Availability)
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


        public async Task<int> CreateEmployer(Employer employer)
        {
            int newemployerId = 0;
            string sqlDataSource = WebConfigurationManager.ConnectionStrings["DBConnectionString"].ConnectionString;
            using (SqlConnection connection = new SqlConnection(sqlDataSource))
            {
                await connection.OpenAsync();

                
                string sqlInsertemployer = "INSERT INTO Employer (name, job_title, location, availability) VALUES (@Name, @JobTitle, @Location, @Availability); SELECT SCOPE_IDENTITY();";
                using (SqlCommand command = new SqlCommand(sqlInsertemployer, connection))
                {
                    command.Parameters.AddWithValue("@Name", employer.Name);
                    command.Parameters.AddWithValue("@JobTitle", employer.JobTitle);
                    command.Parameters.AddWithValue("@Location", employer.Location);
                    command.Parameters.AddWithValue("@Availability", employer.Availability);
                    newemployerId = Convert.ToInt32(await command.ExecuteScalarAsync());
                }

                foreach (var skill in employer.RequiredSkills)
                {
                    string insertSkillQuery = "INSERT INTO EmployerSkills (employer_id, skill_id) " +
                                               "VALUES (@EmployerId, @SkillId)";
                    SqlCommand insertSkillCommand = new SqlCommand(insertSkillQuery, connection);
                    insertSkillCommand.Parameters.AddWithValue("EmployerId", employer.Id);
                    insertSkillCommand.Parameters.AddWithValue("SkillId", skill.Id);
                    insertSkillCommand.ExecuteNonQuery();
                }

                foreach (var location in employer.Locations)
                {
                    string insertSkillQuery = "INSERT INTO EmployerLocations (employer_id, skill_id) " +
                                               "VALUES (@EmployerId, @SkillId)";
                    SqlCommand insertSkillCommand = new SqlCommand(insertSkillQuery, connection);
                    insertSkillCommand.Parameters.AddWithValue("EmployerId", employer.Id);
                    insertSkillCommand.Parameters.AddWithValue("SkillId", location.Id);
                    insertSkillCommand.ExecuteNonQuery();
                }
            }


            return newemployerId;
        }

        public static List<Match> NrmpMatch(List<Employee> scoredEmployees, int employerId)
        {
            
            scoredEmployees.Sort((e1, e2) => e2.Score.CompareTo(e1.Score));

            var matches = new List<Match>();
            var usedIndices = new HashSet<int>();

            foreach (var employee in scoredEmployees)
            {
                for (int i = 0; i < scoredEmployees.Count; i++)
                {
                    
                    if (!usedIndices.Contains(i))
                    {
                        var match = new Match
                        {
                            EmployerId = employerId,
                            EmployeeId = scoredEmployees[i].Id
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