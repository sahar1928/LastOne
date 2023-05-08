﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LastOne.Models
{
    public class Employee
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string JobTitle { get; set; }
        public string Location { get; set; }
        public bool Availability { get; set; }
        public string Status { get; set; }
        public string Score { get; set; }
        public List<Location> Locations { get; set; }
        public List<Skill> Skills { get; set; }
    }
}