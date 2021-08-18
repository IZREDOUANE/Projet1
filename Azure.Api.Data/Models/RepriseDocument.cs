using System;
using System.Collections.Generic;
using System.Text;

namespace Azure.Api.Data.Models
{
    public class RepriseDocument
    {
        public int ID { get; set; }
        public string OwnerSfId { get; set; }
        public string DocSfID { get; set; }
        public string Name { get; set; }
        public string FileType { get; set; }
        public string DocumentStatus { get; set; }
        public string DocumentType { get; set; }
        public bool? IsActive { get; set; }
        public DateTime? Date { get; set; }
        public byte[] BinaryDocument { get; set; }
        public string CrecheSfId { get; set; }
        public string Doc_Owner_SFID { get; set; }
    }
}
