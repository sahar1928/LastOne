using LastOne.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using HttpGetAttribute = System.Web.Http.HttpGetAttribute;
using HttpPostAttribute = System.Web.Http.HttpPostAttribute;

namespace LastOne.Controllers
{
    public class UsersController : ApiController
    {
        
        [HttpGet]
        public IEnumerable<Employee> GetEmployees()
        {
            var employees = new List<Employee>();

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();

                var command = new SqlCommand("SELECT * FROM Employees", connection);

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        employees.Add(new Employee
                        {
                            Id = Convert.ToInt32(reader["employee_id"]),
                            Name = reader["name"].ToString(),
                            JobTitle = reader["job_title"].ToString(),
                            Skills = reader["skills"].ToString(),
                            Availability = reader["availability"].ToString(),
                            LocationId = Convert.ToInt32(reader["location_id"]),
                            PriorityLevel = Convert.ToInt32(reader["priority_level"]),
                            Status = reader["status"].ToString(),
                            Address = reader["address"].ToString(),
                            PhoneNumber = reader["phone_number"].ToString()
                        });
                    }
                }
            }

            return employees;
        }

        [HttpGet]
        public Employee GetEmployee(int id)
        {
            Employee employee = null;

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();

                var command = new SqlCommand("SELECT * FROM Employees WHERE employee_id=@id", connection);
                command.Parameters.AddWithValue("@id", id);

                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        employee = new Employee
                        {
                            Id = Convert.ToInt32(reader["employee_id"]),
                            Name = reader["name"].ToString(),
                            JobTitle = reader["job_title"].ToString(),
                            Skills = reader["skills"].ToString(),
                            Availability = reader["availability"].ToString(),
                            LocationId = Convert.ToInt32(reader["location_id"]),
                            PriorityLevel = Convert.ToInt32(reader["priority_level"]),
                            Status = reader["status"].ToString(),
                            Address = reader["address"].ToString(),
                            PhoneNumber = reader["phone_number"].ToString()
                        };
                    }
                }
            }

            if (employee == null)
            {
                throw new HttpResponseException(HttpStatusCode.NotFound);
            }

            return employee;
        }





        public void CalculateAndSaveScores(string inputFilePath, string connectionString)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();

                // Retrieve all employees and employers from the database
                var employees = new List<Employee>();
                using (var command = new SqlCommand("SELECT Id, Text FROM Employees", connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            employees.Add(new Employee
                            {
                                Id = (int)reader["Id"],
                                Text = (string)reader["Text"]
                            });
                        }
                    }
                }

                var employers = new List<Employer>();
                using (var command = new SqlCommand("SELECT Id, Text FROM Employers", connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            employers.Add(new Employer
                            {
                                Id = (int)reader["Id"],
                                Text = (string)reader["Text"]
                            });
                        }
                    }
                }

                // Create the table to store the scores if it doesn't exist
                using (var command = new SqlCommand("CREATE TABLE IF NOT EXISTS EmployeeEmployerScores (EmployeeId INT NOT NULL, EmployerId INT NOT NULL, Score FLOAT NOT NULL, PRIMARY KEY (EmployeeId, EmployerId))", connection))
                {
                    command.ExecuteNonQuery();
                }

                // Calculate the scores between each employee and employer
                var scores = ScoreCalculator.CalculateScores(employees, employers, inputFilePath);

                // Save the scores to the database
                using (var command = new SqlCommand("INSERT INTO EmployeeEmployerScores (EmployeeId, EmployerId, Score) VALUES (@EmployeeId, @EmployerId, @Score) ON DUPLICATE KEY UPDATE Score = VALUES(Score)", connection))
                {
                    foreach (var score in scores)
                    {
                        command.Parameters.Clear();
                        command.Parameters.AddWithValue("@EmployeeId", score.EmployeeId);
                        command.Parameters.AddWithValue("@EmployerId", score.EmployerId);
                        command.Parameters.AddWithValue("@Score", score.Score);
                        command.ExecuteNonQuery();
                    }
                }
            }
        }

        public static List<(int, int, double)> CalculateScore(string inputFilePath, int userId, bool isEmployee)
        {
            // Read input file and split into words
            string[] inputWords = File.ReadAllText(inputFilePath).Split(' ', '\t', '\r', '\n');

            // Create connection to SQL Server and open it
            using (SqlConnection connection = new SqlConnection("YourConnectionStringHere"))
            {
                connection.Open();

                // Create command for selecting either employees or employers based on user input
                string commandText = isEmployee ? "SELECT * FROM Employees" : "SELECT * FROM Employers";
                SqlCommand selectCommand = new SqlCommand(commandText, connection);

                // Execute command and read results into a list of objects
                List<object> objects = new List<object>();
                using (SqlDataReader reader = selectCommand.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        objects.Add(new { Id = reader.GetInt32(0), Text = reader.GetString(1) });
                    }
                }

                // Create command for inserting scores into the shared table
                SqlCommand insertCommand = new SqlCommand("INSERT INTO SharedTable (EmployeeId, EmployerId, Score) VALUES (@EmployeeId, @EmployerId, @Score)", connection);
                insertCommand.Parameters.Add("@EmployeeId", SqlDbType.Int);
                insertCommand.Parameters.Add("@EmployerId", SqlDbType.Int);
                insertCommand.Parameters.Add("@Score", SqlDbType.Float);

                // Calculate scores for each object and save to the shared table
                List<(int, int, double)> scores = new List<(int, int, double)>();
                foreach (dynamic obj in objects)
                {
                    int id = obj.Id;
                    string[] words = obj.Text.Split(' ', '\t', '\r', '\n');

                    double score = 0.0;
                    foreach (string inputWord in inputWords)
                    {
                        double maxSimilarity = 0.0;
                        foreach (string word in words)
                        {
                            double similarity = NRPM(inputWord, word);
                            if (similarity > maxSimilarity)
                            {
                                maxSimilarity = similarity;
                            }
                        }
                        score += maxSimilarity;
                    }

                    // Save score to the shared table
                    insertCommand.Parameters["@EmployeeId"].Value = isEmployee ? id : userId;
                    insertCommand.Parameters["@EmployerId"].Value = isEmployee ? userId : id;
                    insertCommand.Parameters["@Score"].Value = score;
                    insertCommand.ExecuteNonQuery();

                    // Add score to the list
                    scores.Add((isEmployee ? id : userId, isEmployee ? userId : id, score));
                }

                // Return list of scores
                return scores;
            }
        }

        private static double NRPM(string s, string t)
        {
            int n = s.Length;
            int m = t.Length;

            if (n == 0 || m == 0)
            {
                return 0;
            }

            double[,] dp = new double[n + 1, m + 1];

            for (int i = 0; i <= n; i++)
            {
                dp[i, 0] = 0;
            }

            for (int j = 0; j <= m; j++)
            {
                dp[0, j] = 0;
            }

            for (int i = 1; i <= n; i++)
            {
                for (int j = 1; j <= m; j++)
                {
                    if (s[i - 1] == t[j - 1])
                    {
                        dp[i, j] = dp[i - 1, j - 1] + 1;
                    }
                    else
                    {
                        dp[i, j] = Math.Max(dp[i - 1, j], dp[i, j - 1]);
                    }
                }
            }

            return dp[n, m] / Math.Max(n, m);
        }


    }
}