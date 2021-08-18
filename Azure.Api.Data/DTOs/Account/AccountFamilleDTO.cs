using Azure.Api.Data.DTOs.Contact;
using System;
using System.Collections.Generic;
using System.Text;

namespace Azure.Api.Data.DTOs.Account
{
    public class AccountFamilleDTO
    {       
        public string Nom { get; set; }
        public string Prenom { get; set; }
        public ShippingAddressDTO Adresse { get; set; }
        public string Login { get; set; }
        public string Password { get; set; }

        public string Tel { get; set; }

        public List<ContactParentEnfantDTO> Contacts { get; set; }

        public string NumeroAllocataire { get; set; }
        public int NbEnfantsACharge { get; set; }
        public bool EnfantHandicape { get; set; }
        public string Garde { get; set; }

        public IEnumerable<ContactParentDTO> Parents { get; set; }
        public IEnumerable<ContactEnfantDTO> Enfants { get; set; }

    }
}
