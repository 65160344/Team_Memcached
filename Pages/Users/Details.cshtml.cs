using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Memcach.Data;
using Memcach.Model;

namespace Memcach.Pages.Users
{
    // PageModel สำหรับแสดงรายละเอียดของผู้ใช้งาน
    public class DetailsModel : PageModel
    {
        // ตัวแปรสำหรับเชื่อมต่อกับฐานข้อมูล
        private readonly Memcach.Data.MemcachContext _context;

        // Constructor รับ database context จาก dependency injection
        public DetailsModel(Memcach.Data.MemcachContext context)
        {
            _context = context;
        }

        // Property สำหรับเก็บข้อมูล User ที่จะแสดงผล
        public User User { get; set; } = default!;

        // Handler method สำหรับ HTTP GET request
        // รับ parameter id เพื่อค้นหาข้อมูล User
        public async Task<IActionResult> OnGetAsync(int? id)
        {
            // ตรวจสอบว่ามีการส่ง id มาหรือไม่
            if (id == null)
            {
                return NotFound();
            }

            // ค้นหา User จากฐานข้อมูลด้วย id
            var user = await _context.User.FirstOrDefaultAsync(m => m.ID == id);
            
            // ถ้าไม่พบข้อมูลให้ส่ง NotFound กลับไป
            if (user == null)
            {
                return NotFound();
            }
            else 
            {
                // ถ้าพบข้อมูลให้เก็บไว้ใน Property User
                User = user;
            }
            // ส่งกลับไปแสดงผลที่หน้าเว็บ
            return Page();
        }
    }
}