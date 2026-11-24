using System;
using System.Collections.Generic;

namespace OnlineCoursePlatform.Models;

public partial class Enrollment
{
    public int Id { get; set; }

    public string StudentId { get; set; } = null!;

    public int CourseId { get; set; }

    public DateTime EnrolledAt { get; set; }

    public decimal ProgressPercentage { get; set; }

    public string Status { get; set; } = null!;

    public virtual Course Course { get; set; } = null!;

    public virtual ApplicationUser Student { get; set; } = null!;
}
