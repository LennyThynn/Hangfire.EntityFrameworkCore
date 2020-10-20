using System;
using Hangfire.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Hangfire.EntityFrameworkCore
{

    internal class HangfireContext : DbContext
    {
        internal string Schema { get; }
        private string Prefix { get; }

        public HangfireContext([NotNull] DbContextOptions options, [NotNull] string schema = "", string prefix = "")
            : base(options)
        {
            if (schema is null)
                throw new ArgumentNullException(nameof(schema));
            if (!string.IsNullOrEmpty(prefix))
                prefix += "_";
            Schema = schema;
            Prefix = prefix;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
            optionsBuilder.ReplaceService<IModelCacheKeyFactory, HangfireModelCacheKeyFactory>();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            if (!string.IsNullOrEmpty(Schema))
                modelBuilder.HasDefaultSchema(Schema);

            modelBuilder.Entity<HangfireCounter>(entity =>
            {
                entity.HasKey(x => x.Id).HasName(Prefix + "HF_COUNTER_PK");
                entity.HasIndex(nameof(HangfireCounter.Key), nameof(HangfireCounter.Value)).HasName(Prefix + "HF_COUNTER_KEY_VALUE_I");
                entity.HasIndex(nameof(HangfireCounter.ExpireAt)).HasName(Prefix + "HF_COUNTER_EXPIREAT_I");
                entity.ToTable(Prefix + "HF_COUNTER");
            });

            modelBuilder.Entity<HangfireHash>(entity =>
            {
                entity.HasKey(x => new { x.Key, x.Field }).HasName(Prefix + "HF_HASH_PK");
                entity.HasIndex(nameof(HangfireHash.ExpireAt)).HasName(Prefix + "HF_HASH_EXPIREAT_I"); ;
                entity.ToTable(Prefix + "HF_HASH");
            });

            modelBuilder.Entity<HangfireJob>(entity =>
            {
                entity.HasKey(x => x.Id).HasName(Prefix + "HF_JOB_PK");
                entity.HasIndex(nameof(HangfireJob.StateName)).HasName(Prefix + "HF_JOB_STATENAME_I");
                entity.HasIndex(nameof(HangfireJob.ExpireAt)).HasName(Prefix + "HF_JOB_EXPIREAT_I");
                entity.HasIndex(nameof(HangfireJob.StateId)).HasName(Prefix + "HF_JOB_STATEID_I");
                entity.HasMany(x => x.States)
                    .WithOne(x => x.Job)
                    .HasForeignKey(x => x.JobId)
                    .HasConstraintName(Prefix + "HF_STATE_JOB_FK");
                entity.HasMany(x => x.QueuedJobs)
                    .WithOne(x => x.Job)
                    .HasForeignKey(x => x.JobId)
                    .HasConstraintName(Prefix + "HF_JOB_QUEUE_JOB_FK");
                entity.HasMany(x => x.Parameters)
                    .WithOne(x => x.Job)
                    .HasForeignKey(x => x.JobId)
                    .HasConstraintName(Prefix + "HF_JOB_PARAMETER_JOB_FK");
                entity.ToTable(Prefix + "HF_JOB");
            });

            modelBuilder.Entity<HangfireJobParameter>(entity =>
            {
                entity.HasKey(x => new { x.JobId, x.Name }).HasName(Prefix + "HF_JOB_PARAMETER_PK");
                entity.ToTable(Prefix + "HF_JOB_PARAMETER");
            });

            modelBuilder.Entity<HangfireList>(entity =>
            {
                entity.HasKey(x => new { x.Key, x.Position }).HasName(Prefix + "HF_LIST_PK");
                entity.HasIndex(nameof(HangfireList.ExpireAt)).HasName(Prefix + "HF_LIST_EXPIREAT_I"); ;
                entity.ToTable(Prefix + "HF_LIST");
            });

            modelBuilder.Entity<HangfireLock>(entity =>
            {
                entity.HasKey(x => x.Id).HasName(Prefix + "HF_DISTRIBUTED_LOCK_PK");
                entity.ToTable(Prefix + "HF_DISTRIBUTED_LOCK");

            });

            modelBuilder.Entity<HangfireQueuedJob>(entity =>
            {
                entity.HasKey(x => x.Id).HasName(Prefix + "HF_JOB_QUEUE_PK");
                entity.HasIndex(nameof(HangfireQueuedJob.Queue), nameof(HangfireQueuedJob.FetchedAt)).HasName(Prefix + "HF_JOB_QUEUE_QUEUE_FETCHED_I");
                entity.HasIndex(nameof(HangfireQueuedJob.JobId)).HasName(Prefix + "HF_JOB_QUEUE_JOBID_I");
                entity.ToTable(Prefix + "HF_JOB_QUEUE");
            });

            modelBuilder.Entity<HangfireSet>(entity =>
            {
                entity.HasKey(x => new { x.Key, x.Value }).HasName(Prefix + "HF_SET_PK");
                entity.HasIndex(nameof(HangfireSet.Key), nameof(HangfireSet.Score)).HasName(Prefix + "HF_SET_KEY_SCORE_I");
                entity.HasIndex(nameof(HangfireSet.ExpireAt)).HasName(Prefix + "HF_SET_EXPIREAT_I");
                entity.ToTable(Prefix + "HF_SET");
            });

            modelBuilder.Entity<HangfireServer>(entity =>
            {
                entity.Property(x => x.Id).HasColumnName("ID");
                entity.Property(x => x.Heartbeat).HasColumnName("HEARTBEAT");
                entity.Property(x => x.WorkerCount).HasColumnName("WORKERCOUNT");
                entity.Property(x => x.StartedAt).HasColumnName("STARTEDAT");
                entity.Property(x => x.Queues).HasColumnName("QUEUES");

                entity.HasKey(x => x.Id).HasName(Prefix + "HF_SERVER_PK");
                entity.HasIndex(nameof(HangfireServer.Heartbeat)).HasName(Prefix + "HF_HEARTBEAT_I");
                entity.ToTable(Prefix + "HF_SERVER");
            });

            modelBuilder.Entity<HangfireState>(entity =>
            {
                entity.HasKey(x => x.Id).HasName(Prefix + "HF_STATE_PK");
                entity.HasIndex(nameof(HangfireState.JobId)).HasName(Prefix + "HF_STATE_JOBID_I");
                entity.HasMany<HangfireJob>().
                    WithOne(x => x.State).
                    HasForeignKey(x => x.StateId).HasConstraintName(Prefix + "HF_JOB_STATE_FK");
                entity.HasOne(x => x.Job)
                    .WithMany(x => x.States)
                    .HasForeignKey(x => x.JobId)
                    .HasConstraintName(Prefix + "HF_STATE_JOB_FK")
                    .OnDelete(DeleteBehavior.Cascade);
                entity.ToTable(Prefix + "HF_STATE");
            });
        }
    }
}
