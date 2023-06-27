using LastOne.Models.enums;
using System;

namespace LastOne.Models
{
    public class Candidate
    {
        public string Id { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }

        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string Email { get; set; }
        public Gender Gender { get; set; }
        public CandidateType CandidateType { get; set; }
        public Resume Resume { get; set; }
    }

}