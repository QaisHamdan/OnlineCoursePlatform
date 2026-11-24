namespace OnlineCoursePlatform.Models
{
    public class PaymentViewModel
    {
        public int CourseId { get; set; }
        public string CourseTitle { get; set; }
        public decimal Amount { get; set; }
        public string CardHolderName { get; set; }
        public string CardNumber { get; set; }
        public string ExpiryDate { get; set; }
        public string Cvv { get; set; }
    }
}