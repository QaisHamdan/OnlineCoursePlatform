using System;
using System.Collections.Generic;

namespace OnlineCoursePlatform.Models;

public partial class Review
{
    public int Id { get; set; }

    public int CourseId { get; set; }

    public string StudentId { get; set; } = null!;

    public int Rating { get; set; }

    public string? Comment { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Course Course { get; set; } = null!;

    public virtual ApplicationUser Student { get; set; } = null!;
}
