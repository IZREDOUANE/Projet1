using System;
using System.Collections.Generic;
using System.Text;

namespace Azure.Api.Data.DTOs
{
    public class RegisterFamilleDTO
    {
        public string nom_p1 { get; set; }
        public string prenom_p1 { get; set; }
        public string adresse_p1 { get; set; }
        public string ville_p1 { get; set; }
        public string code_postale_p1 { get; set; }
        public string tel_fixe_p1 { get; set; }
        public string portable_p1 { get; set; }
        public string email_p1 { get; set; }
        public string num_caf_msa { get; set; }
        public int nbre_enfant_charge { get; set; }
        public int nbre_enfant_handicape { get; set; }
        public int nbre_jours_conges { get; set; }
        public string situation_p1 { get; set; }
        public string autorite_p1 { get; set; }
        public string nom_p2 { get; set; }
        public string prenom_p2 { get; set; }
        public string adresse_p2 { get; set; }
        public string ville_p2 { get; set; }
        public string code_postale_p2 { get; set; }
        public string tel_fixe_p2 { get; set; }
        public string portable_p2 { get; set; }
        public string email_p2 { get; set; }
        public string num_caf_msa_p2 { get; set; }
        public int nbre_enfant_charge_p2 { get; set; }
        public int nbre_enfant_handicape_p2 { get; set; }
        public int nbre_jours_conges_p2 { get; set; }
        public string situation_p2 { get; set; }
        public string autorite_p2 { get; set; }
        public string password { get; set; }
        public string code { get; set; }
        public List<RegisterFamilleEnfantDTO> Enfants { get; set; }

    }
}
