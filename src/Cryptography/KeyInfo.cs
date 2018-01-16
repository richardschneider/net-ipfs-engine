using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ipfs.Engine.Cryptography
{
    class KeyInfo : IKey
    {
        [Column("Id", Order = 1)]
        public string _Id {
            get { return Id.ToBase58(); }
            set { Id = new MultiHash(value); }
        }

        [NotMapped]
        public MultiHash Id { get; set; }

        [Key]
        [Column(Order = 0)]
        public string Name { get; set; }
    }
}
