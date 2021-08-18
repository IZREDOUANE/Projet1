using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Azure.Api.Data.Models
{
    [Table("MonitoringFlux")]
    public class MonitoringFlux
    {
        public int Id { get; set; }
        public string Nom { get; set; }
        public bool Sens { get; set; }
        public string Source { get; set; }
        public string Type { get; set; }
        public DateTime Date { get; set; } = DateTime.Now;
        public bool IsSuccess { get; set; } = true;
        public string Result { get; set; }
        public string Erreur { get; set; }
    }
}
