namespace Azure.Api.Data.DTOs.Preinscription
{
    public class PreinscriptionStatusDto
    {
        public int? Id { get; set; }
        public string Label { get; set; }

        public bool IsNull => Id == null && string.IsNullOrEmpty(Label);
    }
}
