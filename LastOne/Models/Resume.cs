using LastOne.Models;
using LastOne.Models.enums;
using System;
using System.Collections.Generic;

public partial class Resume
{
    public string FullName { get; set; }
    public Uri Email { get; set; }
    public string ProfessionalTitle { get; set; }
    public string Location { get; set; }
    public DateTime Date { get; set; }
    public Uri VideoURL { get; set; }
    public ResumeCategory ResumeCategory { get; set; }
    public List<SkillType> Skills { get; set; }
    public SocialMedia SocialMedia { get; set; }
    public string ResumeContent { get; set; }
    public byte[] PhotoFile { get; set; }


    public List<Education> Educations { get; set; }
    public List<Experience> Experiences { get; set; }
    public byte[] ResumeFile { get; set; }
}


