using LastOne.Models.enums;
using System;
using System.Collections.Generic;

namespace LastOne.Models
{
    public class Job
    {
        public int Id { get; set; }
        public string JobTitle { get; set; }
        public Uri EmailUrl { get; set; }
        public string Location { get; set; }
        public JobType JobType { get; set; }
        public JobCategory JobCategory { get; set; }
        public decimal ExpectedSalary { get; set; }
        public string PreviousExperience { get; set; }
        public JobDescription JobDescription { get; set; }
        public List<string> JobTags { get; set; }
        public Range Salary { get; set; }
        public Company Company { get; set; }
    }



}