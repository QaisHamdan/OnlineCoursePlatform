using System;
using System.Collections.Generic;

namespace OnlineCoursePlatform.Models;

public partial class Lesson
{
    public int Id { get; set; }

    public int CourseId { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public string? VideoUrl { get; set; }

    public int DisplayOrder { get; set; }

    public int? DurationInMinutes { get; set; }

    public bool IsFreePreview { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Course Course { get; set; } = null!;
}
