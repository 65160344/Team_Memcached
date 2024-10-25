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
   // PageModel สำหรับสร้างผู้ใช้งานใหม่
   public class CreateModel : PageModel
   {
       // ตัวแปรสำหรับเชื่อมต่อฐานข้อมูลและระบบ Cache
       private readonly MemcachContext _context;
       private readonly IMemoryCache _memoryCache;

       // Constructor รับ database context และ memory cache จาก dependency injection
       public CreateModel(MemcachContext context, IMemoryCache memoryCache)
       {
           _context = context;
           _memoryCache = memoryCache;
       }

       // Property สำหรับผูกข้อมูลจากฟอร์ม
       [BindProperty]
       public User User { get; set; }

       // Handler method สำหรับแสดงหน้าสร้างผู้ใช้งานใหม่
       public void OnGet()
       {
           // เรียกหน้า Create.cshtml
       }

       // Handler method สำหรับบันทึกข้อมูลผู้ใช้งานใหม่
       public async Task<IActionResult> OnPostAsync()
       {
           var stopwatch = Stopwatch.StartNew(); // เริ่มจับเวลาการทำงาน

           // ตรวจสอบความถูกต้องของข้อมูลที่ส่งมา
           if (!ModelState.IsValid)
           {
               Console.WriteLine($"[VALIDATION TIME] Time taken: {stopwatch.Elapsed.TotalMilliseconds} ms");
               return Page();
           }

           // ตรวจสอบว่ามีข้อมูล User หรือไม่
           if (User == null)
           {
               ModelState.AddModelError(string.Empty, "User data is missing.");
               Console.WriteLine($"[USER CHECK TIME] Time taken: {stopwatch.Elapsed.TotalMilliseconds} ms");
               return Page();
           }

           // ตรวจสอบว่ามีผู้ใช้ที่มีข้อมูลซ้ำในระบบหรือไม่
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

           // สร้าง key สำหรับเก็บใน Cache โดยใช้ Email เป็น unique key
           string cacheKey = $"User:{User.Email}";

           // ตรวจสอบข้อมูลใน Cache 
           bool cacheHit = _memoryCache.TryGetValue(cacheKey, out User cachedUser);
           if (cacheHit)
           {
               User = cachedUser;
               Console.WriteLine($"[CACHE HIT] Time taken to retrieve from cache: {stopwatch.Elapsed.TotalMilliseconds} ms");
               return RedirectToPage("./Index");
           }

           try
           {
               // บันทึกข้อมูลลงฐานข้อมูล พร้อมเวลาที่สร้าง
               User.CreatedAt = DateTime.Now;
               _context.User.Add(User);
               await _context.SaveChangesAsync();

               // บันทึกข้อมูลลง Cache พร้อมกำหนดเวลาหมดอายุ
               _memoryCache.Set(cacheKey, User, new MemoryCacheEntryOptions
               {
                   AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(7), // หมดอายุใน 7 วัน
                   SlidingExpiration = TimeSpan.FromHours(1) // ต่ออายุทุกครั้งที่มีการเรียกใช้ โดยต่อครั้งละ 1 ชั่วโมง
               });
           }
           catch (Exception ex)
           {
               // จัดการกรณีเกิดข้อผิดพลาดในการบันทึกข้อมูล
               Console.WriteLine($"Error occurred while saving user: {ex.Message}");
               ModelState.AddModelError(string.Empty, "An error occurred while saving data.");
               return Page();
           }

           // จบการทำงานและแสดงเวลาที่ใช้ทั้งหมด
           stopwatch.Stop();
           Console.WriteLine($"[TOTAL TIME] Total time taken for Create: {stopwatch.Elapsed.TotalMilliseconds} ms");
           return RedirectToPage("./Index");
       }
   }
}