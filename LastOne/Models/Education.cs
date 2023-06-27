using System;

namespace LastOne.Models
{
    public class Education
    {
        public string InstitutionName { get; set; }
        public string Qualification { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Notes { get; set; }
    }
}