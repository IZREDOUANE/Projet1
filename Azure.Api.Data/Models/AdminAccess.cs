namespace Azure.Api.Data.Models
{
    public class AdminAccess
    {
        public int AdminID { get; set; }
        public int PlatformID { get; set; }
        public bool IsSuperAdmin { get; set; }

        public virtual Admin AdminFkNav { get; set; }
        public virtual Platform PlatformFkNav { get; set; }
    }
}
