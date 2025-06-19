using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Practise_Ych_06._25
{
    public class ContractViewModel
    {
        public int contract_id { get; set; }
        public string client_name { get; set; }
        public string product_name { get; set; }
        public string category { get; set; }
        public decimal monthly_payment { get; set; }
        public int months_remaining { get; set; }
    }
}
