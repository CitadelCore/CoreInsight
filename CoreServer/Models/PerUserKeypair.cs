namespace CoreServer.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    public partial class PerUserKeypair
    {
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int Id { get; set; }

        [Required]
        [StringLength(40)]
        public string Fingerprint { get; set; }

        public DateTime Expiry { get; set; }

        [Required]
        [StringLength(40)]
        public string User { get; set; }
    }
}
