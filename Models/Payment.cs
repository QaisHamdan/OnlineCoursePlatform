using System;
using System.Collections.Generic;

namespace OnlineCoursePlatform.Models;

public partial class Payment
{
    public int Id { get; set; }

    public string StudentId { get; set; } = null!;

    public int CourseId { get; set; }

    public string TransactionId { get; set; } = null!;

    public decimal Amount { get; set; }

    public string PaymentProvider { get; set; } = null!;

    public string PaymentStatus { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual Course Course { get; set; } = null!;

    public virtual ApplicationUser Student { get; set; } = null!;
}
