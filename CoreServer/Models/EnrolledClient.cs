namespace CoreServer.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    public partial class EnrolledClient
    {
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int Id { get; set; }

        [Required]
        [StringLength(40)]
        public string ClientGuid { get; set; }

        [Required]
        [StringLength(50)]
        public string Fingerprint { get; set; }

        [Required]
        [StringLength(40)]
        public string User { get; set; }

        public int? LockoutStatus { get; set; }

        [StringLength(40)]
        public string LockoutRef { get; set; }
    }
}
