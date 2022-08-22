﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocStipendDesktop.Models
{
    public class Student
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? StudentGroup { get; set; }
        public string? Status { get; set; }
    }
}
