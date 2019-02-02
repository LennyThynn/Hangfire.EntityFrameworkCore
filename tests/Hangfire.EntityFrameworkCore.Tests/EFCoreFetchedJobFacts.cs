﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Hangfire.Storage;
using Xunit;

namespace Hangfire.EntityFrameworkCore.Tests
{
    public class EFCoreFetchedJobFacts : EFCoreStorageTest
    {
        [Fact]
        public void Ctor_Throws_WhenStorageParameterIsNull()
        {
            EFCoreStorage storage = null;
            var item = new HangfireJobQueue();

            Assert.Throws<ArgumentNullException>(nameof(storage),
                () => new EFCoreFetchedJob(storage, item));
        }

        [Fact]
        public void Ctor_Throws_WhenItemParameterIsNull()
        {
            var storage = CreateStorageStub();
            HangfireJobQueue item = null;

            Assert.Throws<ArgumentNullException>(nameof(item),
                () => new EFCoreFetchedJob(storage, item));
        }

        [Fact]
        public void Ctor_CreatesInstance()
        {
            var item = new HangfireJobQueue
            {
                Id = 1,
                JobId = 2,
                Queue = "queue",
            };

            using (var instance = new EFCoreFetchedJob(Storage, item))
            {
                Assert.False(Assert.IsType<bool>(
                    instance.GetFieldValue("_disposed")));
                Assert.False(Assert.IsType<bool>(
                    instance.GetFieldValue("_completed")));
                Assert.Same(Storage, Assert.IsType<EFCoreStorage>(
                    instance.GetFieldValue("_storage")));
                Assert.Same(item,
                    Assert.IsType<HangfireJobQueue>(
                        instance.GetFieldValue("_item")));
                Assert.Equal(item.Id, instance.Id);
                Assert.Equal(item.JobId, instance.JobId);
                Assert.Equal(item.Queue, instance.Queue);
                Assert.Equal
                    (item.JobId.ToString(CultureInfo.InvariantCulture),
                    ((IFetchedJob)instance).JobId);
            }

        }

        [Fact]
        public void RemoveFromQueue_WhenItemAlreadyRemoved()
        {
            var item = new HangfireJobQueue
            {
                Id = 1,
                Queue = "queue",
                FetchedAt = DateTime.UtcNow,
            };
            using (var instance = new EFCoreFetchedJob(Storage, item))
            {
                instance.RemoveFromQueue();

                UseContext(context => Assert.Empty(context.JobQueues));
                Assert.True(Assert.IsType<bool>(
                    instance.GetFieldValue("_completed")));
                Assert.False(Assert.IsType<bool>(
                    instance.GetFieldValue("_disposed")));
            }
        }

        [Fact]
        public void RemoveFromQueue_WhenItemExists()
        {
            var job = new HangfireJob
            {
                InvocationData = new InvocationData(null, null, null, string.Empty),
                Queues = new List<HangfireJobQueue>
                {
                    new HangfireJobQueue
                    {
                        Queue = "queue",
                        FetchedAt = DateTime.UtcNow,
                    },
                },
            };
            UseContextSavingChanges(context => context.Add(job));
            using (var instance = new EFCoreFetchedJob(Storage, job.Queues.Single()))
            {
                instance.RemoveFromQueue();

                UseContext(context =>
                {
                    Assert.Single(context.Jobs);
                    Assert.Empty(context.JobQueues);
                });
                Assert.True(Assert.IsType<bool>(
                    instance.GetFieldValue("_completed")));
                Assert.False(Assert.IsType<bool>(
                    instance.GetFieldValue("_disposed")));
            }
        }

        [Fact]
        public void Requeue_CompletesSuccesfully_WhenItemAlreadyRemoved()
        {
            var item = new HangfireJobQueue
            {
                Id = 1,
                JobId = 1,
                Queue = "queue",
                FetchedAt = DateTime.UtcNow,
            };
            using (var instance = new EFCoreFetchedJob(Storage, item))
            {
                instance.Requeue();

                UseContext(context => Assert.Empty(context.JobQueues));
                Assert.True(Assert.IsType<bool>(
                    instance.GetFieldValue("_completed")));
                Assert.False(Assert.IsType<bool>(
                    instance.GetFieldValue("_disposed")));
            }
        }

        [Fact]
        public void Requeue_CompletesSuccesfully_WhenItemExists()
        {
            var job = new HangfireJob
            {
                InvocationData = new InvocationData(null, null, null, string.Empty),
                Queues = new List<HangfireJobQueue>
                {
                    new HangfireJobQueue
                    {
                        Queue = "queue",
                        FetchedAt = DateTime.UtcNow,
                    },
                },
            };
            UseContextSavingChanges(context => context.Add(job));
            using (var instance = new EFCoreFetchedJob(Storage, job.Queues.Single()))
            {
                instance.Requeue();

                UseContext(context =>
                {
                    var item = Assert.Single(context.JobQueues);
                    Assert.Null(item.FetchedAt);
                });
                Assert.True(Assert.IsType<bool>(
                    instance.GetFieldValue("_completed")));
                Assert.False(Assert.IsType<bool>(
                    instance.GetFieldValue("_disposed")));
            }
        }

        [Fact]
        public void Dispose_CompletesSuccesfully_WhenItemAlreadyRemoved()
        {
            var item = new HangfireJobQueue
            {
                Id = 1,
                JobId = 1,
                Queue = "queue",
                FetchedAt = DateTime.UtcNow,
            };
            using (var instance = new EFCoreFetchedJob(Storage, item))
            {
                instance.Dispose();

                UseContext(context => Assert.Empty(context.JobQueues));
                Assert.True(Assert.IsType<bool>(
                    instance.GetFieldValue("_completed")));
                Assert.True(Assert.IsType<bool>(
                    instance.GetFieldValue("_disposed")));

                instance.Dispose();
            }
        }

        [Fact]
        public void Dispose_CompletesSuccesfully_WhenItemExists()
        {
            var job = new HangfireJob
            {
                InvocationData = new InvocationData(null, null, null, string.Empty),
                Queues = new List<HangfireJobQueue>
                {
                    new HangfireJobQueue
                    {
                        Queue = "queue",
                        FetchedAt = DateTime.UtcNow,
                    },
                },
            };
            UseContextSavingChanges(context => context.Add(job));
            using (var instance = new EFCoreFetchedJob(Storage, job.Queues.Single()))
            {
                instance.Dispose();

                UseContext(context =>
                {
                    var item = Assert.Single(context.JobQueues);
                    Assert.Null(item.FetchedAt);
                });
                Assert.True(Assert.IsType<bool>(
                    instance.GetFieldValue("_completed")));
                Assert.True(Assert.IsType<bool>(
                    instance.GetFieldValue("_disposed")));

                instance.Dispose();
            }
        }
    }
}
