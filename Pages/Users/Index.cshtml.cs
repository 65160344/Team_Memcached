using System;
using System.Collections.Generic;
using System.Diagnostics; // เพื่อใช้งาน Stopwatch สำหรับจับเวลาการทำงาน
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Memcach.Data;
using Memcach.Model;
using Microsoft.Extensions.Caching.Memory; // เพื่อใช้งาน Memory Cache

namespace Memcach.Pages.Users
{
    public class IndexModel : PageModel
    {
        private readonly MemcachContext _context; // ตัวแปรสำหรับเชื่อมต่อกับฐานข้อมูล
        private readonly IMemoryCache _memoryCache; // ตัวแปรสำหรับจัดการ Memory Cache

        // Constructor รับ dependency injection ของ DbContext และ IMemoryCache
        public IndexModel(MemcachContext context, IMemoryCache memoryCache)
        {
            _context = context;
            _memoryCache = memoryCache;
        }

        // Property สำหรับเก็บข้อมูล User ที่จะแสดงผล
        public IList<User> User { get; set; } = default!;
        
        // Property สำหรับเก็บเวลาที่ใช้ในการดึงข้อมูล
        public TimeSpan ElapsedTime { get; set; }

        // Method สำหรับ HTTP GET request
        // fetchFromCache: flag สำหรับบอกว่าต้องการดึงข้อมูลจาก cache หรือไม่
        // fetchFromDb: flag สำหรับบอกว่าต้องการดึงข้อมูลจาก database โดยตรงหรือไม่
        public async Task OnGetAsync(bool fetchFromCache = false, bool fetchFromDb = false)
        {
            // สร้าง Stopwatch object สำหรับจับเวลาการทำงาน
            var stopwatch = new Stopwatch();
            stopwatch.Start(); // เริ่มจับเวลา

            if (fetchFromCache)
            {
                // พยายามดึงข้อมูลจาก cache ก่อน
                // ใช้ key "users" ในการเก็บและดึงข้อมูล
                User = _memoryCache.Get<List<User>>("users");

                if (User == null)
                {
                    // ถ้าไม่มีข้อมูลใน cache (cache miss)
                    // ดึงข้อมูลจากฐานข้อมูล
                    User = await _context.User.ToListAsync();
                    
                    // เก็บข้อมูลลง cache โดยกำหนดอายุ 7 วัน
                    _memoryCache.Set("users", User, TimeSpan.FromDays(7));
                }
            }
            else if (fetchFromDb)
            {
                // ดึงข้อมูลจากฐานข้อมูลโดยตรง
                User = await _context.User.ToListAsync();
                
                // อัพเดทข้อมูลใน cache
                _memoryCache.Set("users", User, TimeSpan.FromDays(7));
            }
            else
            {
                // กรณีเริ่มต้น (default) จะดึงข้อมูลจากฐานข้อมูล
                User = await _context.User.ToListAsync();
                
                // และเก็บข้อมูลลง cache
                _memoryCache.Set("users", User, TimeSpan.FromDays(7));
            }

            stopwatch.Stop(); // หยุดจับเวลา
            ElapsedTime = stopwatch.Elapsed; // เก็บเวลาที่ใช้ไปทั้งหมด
        }
    }
}