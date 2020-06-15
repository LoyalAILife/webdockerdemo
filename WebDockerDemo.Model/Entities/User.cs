﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace WebDockerDemo.Model.Entities
{
    public class User
    {
        [Key]
        public Guid ID { get; set; }

        [Required]
        [Column(TypeName = "VARCHAR(16)")]
        public string UserName { get; set; }

        [Required]
        [Column(TypeName = "VARCHAR(16)")]
        public string Password { get; set; }
    }
}