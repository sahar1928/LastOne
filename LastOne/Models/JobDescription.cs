using System.Collections.Generic;

namespace LastOne.Models
{
    public class JobDescription
    {
        public string JobSummary { get; set; }
        public List<Responsibility> Responsibilities { get; set; }
        public List<Qualification> Qualifications { get; set; }
        public List<SkillAndExperience> SkillsAndExperience { get; set; }
    }

    public class Responsibility
    {
        public string Description { get; set; }

    }

    public class Qualification
    {
        public string Description { get; set; }
    }

    public class SkillAndExperience
    {
        public string Description { get; set; }

    }


}