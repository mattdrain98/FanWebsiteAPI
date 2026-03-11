using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fan_Website.Models.Follow
{
    public class Follow
    {
        public int Id { get; set; }
        public required ApplicationUser Follower { get; set; }
        public required ApplicationUser Following { get; set; }
    }
}
