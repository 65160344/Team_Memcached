using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Memcach.Data;
using Memcach.Model;
using Microsoft.Extensions.Caching.Memory;
using System.Diagnostics;

namespace Memcach.Pages.Users
{
    public class DeleteModel : PageModel
    {
        private readonly MemcachContext _context;
        private readonly IMemoryCache _memoryCache;

        public DeleteModel(MemcachContext context, IMemoryCache memoryCache)
        {
            _context = context;
            _memoryCache = memoryCache;
        }

        [BindProperty]
        public User User { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            var stopwatch = Stopwatch.StartNew(); // เริ่มจับเวลา

            if (id == null)
            {
                Console.WriteLine($"[GET CHECK ID TIME] Time taken: {stopwatch.Elapsed.TotalMilliseconds} ms");
                return NotFound();
            }

            var user = await _context.User.FirstOrDefaultAsync(m => m.ID == id);
            if (user == null)
            {
                Console.WriteLine($"[DATABASE QUERY TIME] Time taken: {stopwatch.Elapsed.TotalMilliseconds} ms");
                return NotFound();
            }

            User = user;
            Console.WriteLine($"[GET USER TIME] Time taken: {stopwatch.Elapsed.TotalMilliseconds} ms");
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            var stopwatch = Stopwatch.StartNew(); // เริ่มจับเวลา

            if (id == null)
            {
                Console.WriteLine($"[CHECK ID TIME] Time taken: {stopwatch.Elapsed.TotalMilliseconds} ms");
                return NotFound();
            }

            var user = await _context.User.FindAsync(id);
            if (user != null)
            {
                User = user;

                // ลบข้อมูลจาก Cache ก่อนลบผู้ใช้
                _memoryCache.Remove($"User:{User.Email}");

                _context.User.Remove(User);
                await _context.SaveChangesAsync();
            }

            stopwatch.Stop();
            Console.WriteLine($"[TOTAL TIME] Total time taken for Delete: {stopwatch.Elapsed.TotalMilliseconds} ms");
            return RedirectToPage("./Index");
        }
    }
}
