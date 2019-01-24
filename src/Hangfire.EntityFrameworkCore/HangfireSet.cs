﻿using System;
using System.ComponentModel.DataAnnotations;

namespace Hangfire.EntityFrameworkCore
{
    internal class HangfireSet
    {
        [MaxLength(100)]
        public string Key { get; set; }

        [MaxLength(100)]
        public string Value { get; set; }

        public decimal Score { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? ExpireAt { get; set; }
    }
}
