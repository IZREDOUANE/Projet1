using System;
using System.Collections.Generic;
using System.Text;

namespace Azure.Api.Data.Models
{
    public class Email
    {
        public int ID { get; set; }
        public string Title { get; set; }
        public string Recipient { get; set; }
        public string RecipientCc { get; set; }
        public string Object { get; set; }
        public string Content { get; set; }
        public DateTime? SendingDate { get; set; }

        public ICollection<EmailDocument> EmailDocuments { get; set; }
    }
}
