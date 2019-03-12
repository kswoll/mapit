using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using static MapIt.Utils.ExpressionTrees;

namespace MapIt.Tests
{
    [TestFixture]
    public class EfCoreBugTests
    {
        [Test]
        public async Task EfCoreBug14987NullTarget()
        {
            var dbConnection = new SqliteConnection("DataSource=:memory:");
            dbConnection.Open();
            var dbOptions = new DbContextOptionsBuilder<TestDb>()
                .UseSqlite(dbConnection)
                .Options;
            var db = new TestDb(dbOptions);
            db.Database.EnsureCreated();

            var dbTarget = new DbTarget
            {
            };
            var dbContainer = new DbContainer
            {
                Targets = new List<DbContainerTarget>()
            };
            dbContainer.Targets.Add(new DbContainerTarget
            {
                Target = dbTarget
            });
            db.Containers.Add(dbContainer);
            await db.SaveChangesAsync();

            var container = await db.Containers
                .Select(x => new Container
                {
                    Targets = x.Targets.AsQueryable().Select(MapContainerTarget).ToList()
                })
                .SingleAsync();

        }

        [Test]
        public async Task EfCoreBug14987NonNullTarget()
        {
            var dbConnection = new SqliteConnection("DataSource=:memory:");
            dbConnection.Open();
            var dbOptions = new DbContextOptionsBuilder<TestDb>()
                .UseSqlite(dbConnection)
                .Options;
            var db = new TestDb(dbOptions);
            db.Database.EnsureCreated();

            var dbTarget = new DbTarget
            {
                TypeA = new DbTargetTypeA()
            };
            var dbContainer = new DbContainer
            {
                Targets = new List<DbContainerTarget>()
            };
            dbContainer.Targets.Add(new DbContainerTarget
            {
                Target = dbTarget
            });
            db.Containers.Add(dbContainer);
            await db.SaveChangesAsync();

            var container = await db.Containers
                .Select(x => new Container
                {
                    Targets = x.Targets.AsQueryable().Select(MapContainerTarget).ToList()
                })
                .SingleAsync();

        }

        public static Expression<Func<DbTargetTypeA, TypeA>> MapTargetTypeA { get; } = typeA => new TypeA
        {
            Id = typeA.Id
        };

        public static Expression<Func<DbTarget, Target>> MapTarget { get; } = Compose((DbTarget target) => (Target)Include(target.TypeA, MapTargetTypeA));

        public static Expression<Func<DbContainerTarget, ContainerTarget>> MapContainerTarget { get; } = Compose((DbContainerTarget containerTarget) => new ContainerTarget
        {
            Target = Include(containerTarget.Target, MapTarget)
        });
    }

    public class TestDb : DbContext
    {
        public DbSet<DbContainer> Containers { get; set; }

        public TestDb(DbContextOptions options) : base(options)
        {
        }
    }

    public class DbContainer
    {
        public int Id { get; set; }
        public List<DbContainerTarget> Targets { get; set; }
    }

    public class DbContainerTarget
    {
        public int Id { get; set; }
        public int TargetId { get; set; }

        public DbTarget Target { get; set; }
    }

    public class DbTarget
    {
        public int Id { get; set; }
        public int? TypeAId { get; set; }

        public DbTargetTypeA TypeA { get; set; }
    }

    public class DbTargetTypeA
    {
        public int Id { get; set; }

        public DbTarget Target { get; set; }
    }

    public class Target
    {
    }

    public class TypeA : Target
    {
        public int Id { get; set; }
    }

    public class ContainerTarget
    {
        public Target Target { get; set; }
    }

    public class Container
    {
        public List<ContainerTarget> Targets { get; set; }
    }

}