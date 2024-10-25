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
   // PageModel สำหรับลบข้อมูลผู้ใช้งาน
   public class DeleteModel : PageModel
   {
       // ตัวแปรสำหรับเชื่อมต่อฐานข้อมูลและระบบ Cache
       private readonly MemcachContext _context;
       private readonly IMemoryCache _memoryCache;

       // Constructor รับ database context และ memory cache จาก dependency injection
       public DeleteModel(MemcachContext context, IMemoryCache memoryCache)
       {
           _context = context;
           _memoryCache = memoryCache;
       }

       // Property สำหรับเก็บข้อมูล User ที่จะลบ
       [BindProperty]
       public User User { get; set; } = default!;

       // Handler method สำหรับแสดงข้อมูลผู้ใช้ที่จะลบ
       public async Task<IActionResult> OnGetAsync(int? id)
       {
           var stopwatch = Stopwatch.StartNew(); // เริ่มจับเวลาการทำงาน

           // ตรวจสอบว่ามีการส่ง id มาหรือไม่
           if (id == null)
           {
               Console.WriteLine($"[GET CHECK ID TIME] Time taken: {stopwatch.Elapsed.TotalMilliseconds} ms");
               return NotFound();
           }

           // ค้นหาข้อมูล User จากฐานข้อมูล
           var user = await _context.User.FirstOrDefaultAsync(m => m.ID == id);
           if (user == null)
           {
               // ถ้าไม่พบข้อมูลให้บันทึกเวลาและส่ง NotFound
               Console.WriteLine($"[DATABASE QUERY TIME] Time taken: {stopwatch.Elapsed.TotalMilliseconds} ms");
               return NotFound();
           }

           // เก็บข้อมูล User ที่จะลบและบันทึกเวลาที่ใช้
           User = user;
           Console.WriteLine($"[GET USER TIME] Time taken: {stopwatch.Elapsed.TotalMilliseconds} ms");
           return Page();
       }

       // Handler method สำหรับดำเนินการลบข้อมูล
       public async Task<IActionResult> OnPostAsync(int? id)
       {
           var stopwatch = Stopwatch.StartNew(); // เริ่มจับเวลาการทำงาน

           // ตรวจสอบว่ามีการส่ง id มาหรือไม่
           if (id == null)
           {
               Console.WriteLine($"[CHECK ID TIME] Time taken: {stopwatch.Elapsed.TotalMilliseconds} ms");
               return NotFound();
           }

           // ค้นหาข้อมูล User ที่จะลบ
           var user = await _context.User.FindAsync(id);
           if (user != null)
           {
               User = user;

               // ลบข้อมูลออกจาก Cache ก่อน
               _memoryCache.Remove($"User:{User.Email}");

               // ลบข้อมูลจากฐานข้อมูล
               _context.User.Remove(User);
               await _context.SaveChangesAsync();
           }

           // จบการทำงานและบันทึกเวลาที่ใช้ทั้งหมด
           stopwatch.Stop();
           Console.WriteLine($"[TOTAL TIME] Total time taken for Delete: {stopwatch.Elapsed.TotalMilliseconds} ms");
           return RedirectToPage("./Index");
       }
   }
}