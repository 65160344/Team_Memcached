using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Memcach.Model;
using Memcach.Data;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Memcach.Pages.Users
{
    public class CreateModel : PageModel
    {
        private readonly MemcachContext _context;
        private readonly IMemoryCache _memoryCache;

        public CreateModel(MemcachContext context, IMemoryCache memoryCache)
        {
            _context = context;
            _memoryCache = memoryCache;
        }

        [BindProperty]
        public User User { get; set; }

        public void OnGet()
        {
            // เรียกหน้า Create.cshtml
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var stopwatch = Stopwatch.StartNew(); // เริ่มจับเวลา

            // ตรวจสอบความถูกต้องของข้อมูล
            if (!ModelState.IsValid)
            {
                Console.WriteLine($"[VALIDATION TIME] Time taken: {stopwatch.Elapsed.TotalMilliseconds} ms");
                return Page();
            }

            if (User == null)
            {
                ModelState.AddModelError(string.Empty, "User data is missing.");
                Console.WriteLine($"[USER CHECK TIME] Time taken: {stopwatch.Elapsed.TotalMilliseconds} ms");
                return Page();
            }

            // ตรวจสอบว่ามีผู้ใช้ที่มีชื่อซ้ำอยู่หรือไม่
            var existingUser = await _context.User
                .FirstOrDefaultAsync(u => u.FirstName == User.FirstName &&
                                           u.LastName == User.LastName &&
                                           u.Email == User.Email);

            if (existingUser != null)
            {
                ModelState.AddModelError(string.Empty, "A user with the same name and email already exists.");
                Console.WriteLine($"[DUPLICATE CHECK TIME] Time taken: {stopwatch.Elapsed.TotalMilliseconds} ms");
                return Page();
            }

            string cacheKey = $"User:{User.Email}"; // ใช้ Email เป็น Key เพื่อหลีกเลี่ยงการซ้ำซ้อนใน Cache

            // ตรวจสอบ Cache
            bool cacheHit = _memoryCache.TryGetValue(cacheKey, out User cachedUser);
            if (cacheHit)
            {
                User = cachedUser;
                Console.WriteLine($"[CACHE HIT] Time taken to retrieve from cache: {stopwatch.Elapsed.TotalMilliseconds} ms");
                return RedirectToPage("./Index");
            }

            try
            {
                // บันทึกข้อมูลลงฐานข้อมูล
                User.CreatedAt = DateTime.Now;
                _context.User.Add(User);
                await _context.SaveChangesAsync();

                // บันทึกข้อมูลลง Cache
                _memoryCache.Set(cacheKey, User, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(7),
                    SlidingExpiration = TimeSpan.FromHours(1)
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error occurred while saving user: {ex.Message}");
                ModelState.AddModelError(string.Empty, "An error occurred while saving data.");
                return Page();
            }

            stopwatch.Stop();
            Console.WriteLine($"[TOTAL TIME] Total time taken for Create: {stopwatch.Elapsed.TotalMilliseconds} ms");
            return RedirectToPage("./Index");
        }
    }
}
