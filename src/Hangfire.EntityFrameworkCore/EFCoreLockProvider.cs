﻿using System;
using System.Linq;
using System.Threading;
using Hangfire.Annotations;
using Hangfire.Storage;
using Microsoft.EntityFrameworkCore;

namespace Hangfire.EntityFrameworkCore
{
    using GetLockFunc = Func<HangfireContext, string, HangfireLock>;

    internal class EFCoreLockProvider : IDistributedLockProvider
    {
        private static readonly TimeSpan s_maxSleepDuration = new TimeSpan(0, 0, 1);
        private readonly EFCoreStorage _storage;

        private static GetLockFunc GetLockFunc { get; } = EF.CompileQuery(
            (HangfireContext context, string id) =>
                context.Set<HangfireLock>().
                SingleOrDefault(x => x.Id == id));

        public EFCoreLockProvider(
            [NotNull] EFCoreStorage storage)
        {
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        }

        public void Acquire([NotNull] string resource, TimeSpan timeout)
        {
            if (resource == null)
                throw new ArgumentNullException(nameof(resource));
            if (resource.Length == 0)
                throw new ArgumentException(null, nameof(resource));
            if (timeout <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(timeout), timeout, null);

            var deadline = DateTime.UtcNow + timeout;

            while (true)
            {
                if (TryAcquireLock(resource))
                    return;

                switch (TryReacquireLock(resource))
                {
                    case true:
                        return;
                    case false:
                        continue;
                }

                var remaining = deadline - DateTime.UtcNow;

                if (remaining <= TimeSpan.Zero)
                    break;
                else if (remaining < s_maxSleepDuration)
                    Thread.Sleep(remaining);
                else
                    Thread.Sleep(s_maxSleepDuration);
            }

            throw new DistributedLockTimeoutException(resource);
        }

        public void Release([NotNull] string resource)
        {
            if (resource == null)
                throw new ArgumentNullException(nameof(resource));

            _storage.UseContext(context =>
            {
                context.Remove(new HangfireLock
                {
                    Id = resource,
                });
                try
                {
                    context.SaveChanges();
                }
                catch (DbUpdateConcurrencyException)
                {
                    // Someone else already has deleted this record. Database wins.
                }
            });
        }

        private bool TryAcquireLock(string resource)
        {
            return _storage.UseContext(context =>
            {
                context.Add(new HangfireLock
                {
                    Id = resource,
                    AcquiredAt = DateTime.UtcNow,
                });

                try
                {
                    context.SaveChanges();
                    return true; // Lock taken
                }
                catch (DbUpdateException)
                {
                    return false; // Lock already exists
                }
            });
        }

        private bool? TryReacquireLock(string resource)
        {
            return _storage.UseContext<bool?>(context =>
            {
                var @lock = GetLockFunc(context, resource);

                // If the lock has been removed we should try to insert again
                if (@lock == null)
                    return false;

                // If the lock has been expired, we should update its creation timestamp
                var now = DateTime.UtcNow;
                if (@lock.AcquiredAt + _storage.DistributedLockTimeout < now)
                {
                    @lock.AcquiredAt = now;

                    try
                    {
                        context.SaveChanges();
                        return true; // Lock taken
                    }
                    catch (DbUpdateConcurrencyException)
                    {
                        return false; // Already removed, we should try to insert again
                    }
                }

                return default;
            });
        }
    }
}
