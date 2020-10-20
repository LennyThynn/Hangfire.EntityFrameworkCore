using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Hangfire.EntityFrameworkCore
{
    internal class HangfireState
    {
        public long Id { get; set; }

        public long JobId { get; set; }

        [Required]
        [MaxLength(20)]
        public string Name { get; set; }

        [MaxLength(100)]
        public string Reason { get; set; }

        public DateTime CreatedAt { get; set; }

        [Required]
        public string Data { get; set; }

        [ForeignKey("JobId")]
        public virtual HangfireJob Job { get; set; }
    }
}
