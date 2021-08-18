using System;
using System.Collections.Generic;
using System.Text;

namespace Azure.Api.Data.DTOs.Contact
{
    public class ContactParentDTO
    {
        public string Id { get; set; }
        public string Nom { get; set; }
        public string Prenom { get; set; }
        public string Email { get; set; }
        public string Tel { get; set; }
        public string TelPortable { get; set; }
        public bool AutoriteParentale { get; set; }
    }
}
