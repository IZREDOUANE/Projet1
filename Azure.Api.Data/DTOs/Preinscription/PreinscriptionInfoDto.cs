using System;

namespace Azure.Api.Data.DTOs.Preinscription
{
    public class PreinscriptionInfoDto
    {
        public string Id { get; set; }
        public string RequestNumber { get; set; }
        public string Statut { get; set; }
        public string ChildFirstname { get; set; }
        public DateTime? DateRequest { get; set; }
        public string ContactEnfant { get; set; }
        public string Url { get; set; }
        public string CrecheName { get; set; }
    }
}
