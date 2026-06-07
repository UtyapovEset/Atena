using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebAtena.Models;
using WebAtena.ViewModels;

namespace WebAtena.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public AccountController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        public IActionResult Login() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email) ??
                       await _userManager.FindByNameAsync(model.Email);

            if (user == null)
            {
                ModelState.AddModelError("", "Неверный логин/email или пароль");
                return View(model);
            }

            var result = await _signInManager.PasswordSignInAsync(user.UserName!, model.Password, model.RememberMe, false);

            if (result.Succeeded)
            {
                HttpContext.Session.SetString("userId", user.Id);
                HttpContext.Session.SetString("role", user.Role);
                HttpContext.Session.SetString("username", user.FullName ?? user.Email ?? user.UserName!);

                TempData["Success"] = "Добро пожаловать!";
                return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError("", "Неверный логин/email или пароль");
            return View(model);
        }

        public IActionResult Register() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName,
                PhoneNumber = model.Phone
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "Customer");

                await _signInManager.SignInAsync(user, isPersistent: false);

                HttpContext.Session.SetString("userId", user.Id);
                HttpContext.Session.SetString("role", "Customer");
                HttpContext.Session.SetString("username", user.FullName ?? user.Email!);

                TempData["Success"] = "Регистрация прошла успешно!";
                return RedirectToAction("Index", "Home");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            return View(model);
        }

        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            HttpContext.Session.Clear();
            TempData["Success"] = "Вы успешно вышли из системы";
            return RedirectToAction("Index", "Home");
        }
    }
}