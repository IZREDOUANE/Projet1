using System;

namespace Azure.Api.Data.Models
{
    public class EmailDocument
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public int EmailID { get; set; }
        public string DocumentGUID { get; set; }

        public virtual Email EmailNavFk { get; set; }
        public virtual Document DocumentNavFk { get; set; }
    }
}
