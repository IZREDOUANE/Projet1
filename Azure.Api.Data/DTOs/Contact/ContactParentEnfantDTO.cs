using System;
using System.Collections.Generic;
using System.Text;

namespace Azure.Api.Data.DTOs.Account
{
    public class ContactParentEnfantDTO
    {
        public string Id { get; set; }
        public string Nom { get; set; }
        public string Prenom { get; set; }
        public string Email { get; set; }
        public string ContactType { get; set; }
    }
}
