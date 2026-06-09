using System.Collections.Generic;

namespace SchoolMvc.Models
{
    public class MyCapsulesViewModel
    {
        public AppUser? User { get; set; }
        public List<EncryptionHistory> SentCapsules { get; set; } = new List<EncryptionHistory>();
        public List<EncryptionHistory> IncomingCapsules { get; set; } = new List<EncryptionHistory>();
    }
}
