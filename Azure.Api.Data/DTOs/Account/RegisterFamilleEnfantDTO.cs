using System;
using System.Collections.Generic;
using System.Text;

namespace Azure.Api.Data.DTOs
{
    public class RegisterFamilleEnfantDTO
    {
        public string nom_enfant { get; set; }
        public string prenom_enfant { get; set; }
        public DateTime date_naissance_enfant { get; set; }
    }
}
