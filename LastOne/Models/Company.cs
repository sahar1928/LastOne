using System;

namespace LastOne.Models
{
    public class Company
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string Email { get; set; }
        public string Name { get; set; }
        public Uri Website { get; set; }
        public Uri VideoUrl { get; set; }
        public SocialMedia SocialMedia { get; set; }
        public string Description { get; set; }
        public Uri Logo { get; set; }
    }
}