using System;
using System.Collections.Generic;
using System.Text;

namespace Azure.Api.Data.DTOs.SalesForce
{
    public class AccountContactsLpcrSalesForceDTO
    {
        public UserSalesForceDTO LPCR_ResponsableADV__r { get; set; }
        public UserSalesForceDTO LPCR_ResponsableServiceFamille__r { get; set; }
        public UserSalesForceDTO Owner { get; set; }
    }
}
