using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Memcach.Model;

namespace Memcach.Data
{
    public class MemcachContext : DbContext
    {
        public MemcachContext (DbContextOptions<MemcachContext> options)
            : base(options)
        {
        }

        public DbSet<Memcach.Model.User> User { get; set; } = default!;
    }
}
