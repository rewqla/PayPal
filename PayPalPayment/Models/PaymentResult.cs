namespace PayPalPayment.Models
{
    public class PaymentResultViewModel
    {
        public string RecipientName { get; set; }
        public string RecipientPhone { get; set; }
        public string RecipientEmail { get; set; }
        public string Country { get; set; }
        public string State { get; set; }
        public string City { get; set; }
        public string Address { get; set; }
        public string PostalCode { get; set; }
        public decimal Total { get; set; }
        public string OrderDate { get; set; }
        public string PaymentTransactionId { get; set; }
    }
}
