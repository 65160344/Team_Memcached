using System;
using System.Collections.Generic;
using System.Diagnostics; // นำเข้า Stopwatch
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Memcach.Data;
using Memcach.Model;
using Microsoft.Extensions.Caching.Memory; // นำเข้า IMemoryCache

namespace Memcach.Pages.Users
{
    public class IndexModel : PageModel
    {
        private readonly MemcachContext _context;
        private readonly IMemoryCache _memoryCache; // เพิ่ม IMemoryCache

        public IndexModel(MemcachContext context, IMemoryCache memoryCache)
        {
            _context = context;
            _memoryCache = memoryCache;
        }

        public IList<User> User { get; set; } = default!;
        public TimeSpan ElapsedTime { get; set; } // ตัวแปรเก็บเวลา

        public async Task OnGetAsync(bool fetchFromCache = false, bool fetchFromDb = false)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            if (fetchFromCache)
            {
                // ดึงข้อมูลจาก cache
                User = _memoryCache.Get<List<User>>("users");

                if (User == null)
                {
                    // หากไม่มีข้อมูลใน cache
                    User = await _context.User.ToListAsync();
                    _memoryCache.Set("users", User, TimeSpan.FromDays(7));
                }
            }
            else if (fetchFromDb)
            {
                // ดึงข้อมูลจากฐานข้อมูล
                User = await _context.User.ToListAsync();
                _memoryCache.Set("users", User, TimeSpan.FromDays(7));
            }
            else
            {
                // เริ่มต้นด้วยการดึงข้อมูลจากฐานข้อมูล
                User = await _context.User.ToListAsync();
                _memoryCache.Set("users", User, TimeSpan.FromDays(7));
            }

            stopwatch.Stop();
            ElapsedTime = stopwatch.Elapsed; // เก็บเวลาที่ใช้ในการดำเนินการ
        }
    }
}
