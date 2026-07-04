using System;
using System.Collections.Generic;
using System.Text;
using TaskTracker.Enums;

namespace TaskTracker.Models
{
    public class AppTask
    {
        public int Id { get; set; }
        public string Description { get; set; } = string.Empty;
        public Status Status { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; }
    }
}
