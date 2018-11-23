namespace CoreServer.Models
{
    using System;
    using System.Data.Entity;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Linq;

    public partial class CoreDB : DbContext
    {
        public CoreDB()
            : base("name=CoreDB")
        {
        }

        public virtual DbSet<C__MigrationHistory> C__MigrationHistory { get; set; }
        public virtual DbSet<Audit> Audits { get; set; }
        public virtual DbSet<EnrolledClient> EnrolledClients { get; set; }
        public virtual DbSet<PerUserKeypair> PerUserKeypairs { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Audit>()
                .Property(e => e.User)
                .IsFixedLength();

            modelBuilder.Entity<Audit>()
                .Property(e => e.Guid)
                .IsFixedLength();

            modelBuilder.Entity<Audit>()
                .Property(e => e.Time)
                .IsFixedLength();

            modelBuilder.Entity<Audit>()
                .Property(e => e.Application)
                .IsFixedLength();

            modelBuilder.Entity<Audit>()
                .Property(e => e.Reason)
                .IsFixedLength();

            modelBuilder.Entity<EnrolledClient>()
                .Property(e => e.ClientGuid)
                .IsFixedLength();

            modelBuilder.Entity<EnrolledClient>()
                .Property(e => e.Fingerprint)
                .IsFixedLength();

            modelBuilder.Entity<EnrolledClient>()
                .Property(e => e.User)
                .IsFixedLength();

            modelBuilder.Entity<EnrolledClient>()
                .Property(e => e.LockoutRef)
                .IsFixedLength();

            modelBuilder.Entity<PerUserKeypair>()
                .Property(e => e.Fingerprint)
                .IsFixedLength();

            modelBuilder.Entity<PerUserKeypair>()
                .Property(e => e.User)
                .IsFixedLength();
        }
    }
}
