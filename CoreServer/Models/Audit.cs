namespace CoreServer.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("Audit")]
    public partial class Audit
    {
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int Id { get; set; }

        [Required]
        [StringLength(30)]
        public string User { get; set; }

        [Required]
        [StringLength(50)]
        public string Guid { get; set; }

        [Column(TypeName = "timestamp")]
        [MaxLength(8)]
        [Timestamp]
        public byte[] Time { get; set; }

        [Required]
        [StringLength(25)]
        public string Application { get; set; }

        [Required]
        [StringLength(20)]
        public string Reason { get; set; }
    }
}
