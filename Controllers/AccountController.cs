using FitnessApp.Models;
using FitnessApp.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Policy;
using System.Threading.Tasks;

namespace FitnessApp.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;

        public AccountController(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        // ==========================================
        //              GİRİŞ İŞLEMLERİ
        // ==========================================

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            // Kullanıcı zaten giriş yapmışsa tekrar login sayfasına sokma
            if (User.Identity!.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }

            // Kullanıcının gitmek istediği bir yer varsa (örn: Sepetim) onu sakla
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken] // Form güvenliği (CSRF saldırılarına karşı)
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user != null)
            {
                // lockoutOnFailure: true -> 5 kere yanlış girerse hesabı kısa süreli kilitler
                var result = await _signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, lockoutOnFailure: true);

                if (result.Succeeded)
                {
                    // 1. Eğer kullanıcı spesifik bir linkten geldiyse oraya geri gönder
                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    {
                        return Redirect(returnUrl);
                    }

                    // 2. Admin ise Yönetim Paneline gönder
                    if (await _userManager.IsInRoleAsync(user, "Admin"))
                    {
                        return RedirectToAction("Index", "Admin");
                    }

                    // 3. Standart üyeyse Ana Sayfaya gönder
                    return RedirectToAction("Index", "Home");
                }

                if (result.IsLockedOut)
                {
                    ModelState.AddModelError("", "Çok fazla başarısız deneme. Hesabınız güvenlik için kısa süreliğine kilitlendi.");
                    return View(model);
                }
            }

            ModelState.AddModelError("", "Hatalı e-posta veya şifre.");
            return View(model);
        }

        // ==========================================
        //              KAYIT İŞLEMLERİ
        // ==========================================

        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity!.IsAuthenticated) return RedirectToAction("Index", "Home");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = new AppUser
            {
                UserName = model.Email, // Genelde username olarak email kullanılır
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                // Yeni üyeye varsayılan olarak "Member" rolü veriyoruz
                await _userManager.AddToRoleAsync(user, "Member");

                // Kayıt sonrası otomatik giriş yaptır
                await _signInManager.SignInAsync(user, isPersistent: false);

                // Başarı mesajı (Layout'ta yakalanabilir)
                TempData["SuccessMessage"] = "Aramıza hoşgeldin! Kaydın başarıyla tamamlandı.";
                return RedirectToAction("Index", "Home");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }
            return View(model);
        }

        // ==========================================
        //           ÇIKIŞ VE YETKİ KONTROLÜ
        // ==========================================

        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            TempData["WarningMessage"] = "Oturum başarıyla kapatıldı.";
            return RedirectToAction("Login", "Account");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            // Yetkisi olmayan bir sayfaya girmeye çalıştığında burası çalışır
            return View();
        }
    }
}