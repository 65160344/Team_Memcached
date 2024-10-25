using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Memcach.Data;
using Memcach.Model;
using Microsoft.Extensions.Caching.Memory;

namespace Memcach.Pages.Users
{
    // PageModel สำหรับแก้ไขข้อมูลผู้ใช้งาน พร้อมระบบ Cache
    public class EditModel : PageModel
    {
        // ตัวแปรสำหรับเชื่อมต่อฐานข้อมูลและระบบ Cache
        private readonly MemcachContext _context;
        private readonly IMemoryCache _memoryCache;

        public EditModel(MemcachContext context, IMemoryCache memoryCache)
        {
            _context = context;
            _memoryCache = memoryCache;
        }

        [BindProperty]
        public User User { get; set; } = default!;

        // Handler method สำหรับดึงข้อมูล User ที่จะแก้ไข
        public async Task<IActionResult> OnGetAsync(int? id)
        {
            // สร้าง stopwatch สำหรับจับเวลาการทำงาน
            var stopwatch = new Stopwatch();
            stopwatch.Start(); // เริ่มจับเวลา

            if (id == null)
            {
                stopwatch.Stop();
                Console.WriteLine($"[GET CHECK ID TIME] Time taken: {stopwatch.Elapsed.TotalMilliseconds} ms");
                return NotFound();
            }

            // ตรวจสอบข้อมูลในระบบ Cache ก่อน
            stopwatch.Restart();
            if (_memoryCache.TryGetValue($"User:{id}", out User cachedUser))
            {
                User = cachedUser;
                stopwatch.Stop();
                Console.WriteLine($"[CACHE HIT] Time taken to retrieve from cache: {stopwatch.Elapsed.TotalMilliseconds} ms");
                return Page();
            }

            // ถ้าไม่มีใน Cache ให้ดึงจากฐานข้อมูล
            stopwatch.Restart();
            var user = await _context.User.FirstOrDefaultAsync(m => m.ID == id);
            stopwatch.Stop();

            if (user == null)
            {
                Console.WriteLine($"[DATABASE QUERY TIME] Time taken: {stopwatch.Elapsed.TotalMilliseconds} ms");
                return NotFound();
            }

            User = user;
            Console.WriteLine($"[GET USER TIME] Time taken: {stopwatch.Elapsed.TotalMilliseconds} ms");
            return Page();
        }

        // Handler method สำหรับบันทึกข้อมูลที่แก้ไข
        public async Task<IActionResult> OnPostAsync()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            // ตรวจสอบความถูกต้องของข้อมูล
            if (!ModelState.IsValid)
            {
                stopwatch.Stop();
                Console.WriteLine($"[VALIDATION TIME] Time taken: {stopwatch.Elapsed.TotalMilliseconds} ms");
                return Page();
            }

            // อัปเดตเวลาเป็นเวลาปัจจุบัน
            User.CreatedAt = DateTime.Now;
            
            _context.Attach(User).State = EntityState.Modified;

            // บันทึกลงฐานข้อมูล
            stopwatch.Restart();
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UserExists(User.ID))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            // อัปเดตข้อมูลใน Cache พร้อมกำหนดเวลาหมดอายุ
            _memoryCache.Set($"User:{User.ID}", User, new MemoryCacheEntryOptions()
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(7), // หมดอายุใน 7 วัน
                SlidingExpiration = TimeSpan.FromHours(1) // ต่ออายุทุกครั้งที่มีการเรียกใช้ โดยต่อครั้งละ 1 ชั่วโมง
            });

            stopwatch.Stop();
            Console.WriteLine($"[TOTAL TIME] Total time taken for Edit: {stopwatch.Elapsed.TotalMilliseconds} ms");

            return RedirectToPage("./Index");
        }

        // เมธอดสำหรับตรวจสอบว่ามี User อยู่ในระบบหรือไม่
        private bool UserExists(int id)
        {
            return _context.User.Any(e => e.ID == id);
        }
    }
}