using System;
using System.Collections.Generic;
using System.Text;

namespace Azure.Api.Data.DTOs.Contact
{
    public class ContactEnfantDTO
    {
        public string Id { get; set; }
        public string Nom { get; set; }
        public string Prenom { get; set; }
        public DateTime? DateDeNaissance { get; set; }
    }
}
