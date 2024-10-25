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
    public class EditModel : PageModel
    {
        private readonly MemcachContext _context;
        private readonly IMemoryCache _memoryCache;

        public EditModel(MemcachContext context, IMemoryCache memoryCache)
        {
            _context = context;
            _memoryCache = memoryCache;
        }

        [BindProperty]
        public User User { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start(); // เริ่มจับเวลา

            if (id == null)
            {
                stopwatch.Stop();
                Console.WriteLine($"[GET CHECK ID TIME] Time taken: {stopwatch.Elapsed.TotalMilliseconds} ms");
                return NotFound();
            }

            // ลองดึงข้อมูลจากแคชก่อน
            stopwatch.Restart();
            if (_memoryCache.TryGetValue($"User:{id}", out User cachedUser))
            {
                User = cachedUser;
                stopwatch.Stop();
                Console.WriteLine($"[CACHE HIT] Time taken to retrieve from cache: {stopwatch.Elapsed.TotalMilliseconds} ms");
                return Page(); // คืนค่าเพจที่มีข้อมูลในแคช
            }

            // หากไม่มีข้อมูลในแคชให้ดึงข้อมูลจากฐานข้อมูล
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

        public async Task<IActionResult> OnPostAsync()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start(); // เริ่มจับเวลา

            if (!ModelState.IsValid)
            {
                stopwatch.Stop();
                Console.WriteLine($"[VALIDATION TIME] Time taken: {stopwatch.Elapsed.TotalMilliseconds} ms");
                return Page();
            }

            _context.Attach(User).State = EntityState.Modified;

            // บันทึกเวลาที่ใช้ในการแก้ไขข้อมูล
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

            // อัปเดตข้อมูลในแคช
            _memoryCache.Set($"User:{User.ID}", User, new MemoryCacheEntryOptions()
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(7),
                SlidingExpiration = TimeSpan.FromHours(1)
            });

            stopwatch.Stop();
            Console.WriteLine($"[TOTAL TIME] Total time taken for Edit: {stopwatch.Elapsed.TotalMilliseconds} ms");

            return RedirectToPage("./Index");
        }

        private bool UserExists(int id)
        {
            return _context.User.Any(e => e.ID == id);
        }
    }
}
