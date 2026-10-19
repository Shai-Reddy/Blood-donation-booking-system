using BloodDonation.Web.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using BloodDonation.Web.Models;

namespace BloodDonation.Web.Controllers
{
    // Handles registration, signing in and signing out
    public class AccountController : Controller
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;

        // Gets the services we need to manage accounts and sign users in
        public AccountController(SignInManager<IdentityUser> signInManager, UserManager<IdentityUser> userManager)
        {
            _signInManager = signInManager;
            _userManager = userManager;
        }

        // Opens the registration page
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegistrationViewModel model)
       {
         //check the required registration fields before creating the account
         if (!ModelState.IsValid)
       {
        return View(model);
       }

        // Only the account (email, password, Donor role)is created here.
        // Storing the donor details and questionnaire answers is part of the registration split (Part 4).
        if (model.Password != model.ConfirmPassword)
       {
          // Sends the user back to the form if the passwords do not match
          ViewData["RegisterErrors"] = new[] { "Passwords do not match." };
          return View(model);
       }

        // Uses the email address as the username for the new account
        var user = new IdentityUser
     {
        UserName = model.Email,
        Email = model.Email
    };

       // Tries to create the account with the password provided
       var result = await _userManager.CreateAsync(user, model.Password);

       if (!result.Succeeded)
       {
        // Sends any account creation errors back to the registration page
        ViewData["RegisterErrors"] = result.Errors
            .Select(e => e.Description)
            .ToArray();

        return View(model);
      }

        // Gives the new user the donor role and signs them in
        await _userManager.AddToRoleAsync(user, Roles.Donor);
        await _signInManager.SignInAsync(user, isPersistent: false);

        // Takes them to their donor dashboard after registering
        return RedirectToAction("Dashboard", "Donor");
}

        // Opens the donor login page and keeps the return address if there is one
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // Passes the login details through to the shared sign-in method
        [HttpPost]
        [ValidateAntiForgeryToken]
        public Task<IActionResult> Login(string Email, string Password, bool RememberMe, string? returnUrl = null)
        {
            return SignInAndRedirect(Email, Password, RememberMe, returnUrl);
        }

        // Opens the staff login page
        [HttpGet]
        public IActionResult StaffLogin(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // Staff and coordinators both sign in here.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public Task<IActionResult> StaffLogin(string Email, string Password, string? returnUrl = null)
        {
            return SignInAndRedirect(Email, Password, rememberMe: false, returnUrl);
        }

        // Signs the current user out and takes them back to the homepage
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        // Shows the page for users who do not have permission to access something
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        // Handles signing in and works out where the user should go afterwards
        private async Task<IActionResult> SignInAndRedirect(string email, string password, bool rememberMe, string? returnUrl)
        {
            // Keeps the return address and email available for the login page
            ViewData["ReturnUrl"] = returnUrl;
            ViewData["Email"] = email;

            // Checks the login details and uses the remember me option
            var result = await _signInManager.PasswordSignInAsync(email ?? "", password ?? "", rememberMe, lockoutOnFailure: false);
            if (!result.Succeeded)
            {
                // Shows an error if the sign-in attempt was unsuccessful
                ViewData["LoginError"] = "Invalid email or password.";
                return View();
            }

            // Takes the user back to their requested page if the address is within this site
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return LocalRedirect(returnUrl);
            }

            // Gets the user's roles so we can send them to the right dashboard
            var user = await _userManager.FindByEmailAsync(email!);
            var roles = user is null ? new List<string>() : await _userManager.GetRolesAsync(user);

            // Coordinators go to the coordinator dashboard
            if (roles.Contains(Roles.Coordinator))
            {
                return RedirectToAction("Dashboard", "Coordinator");
            }

            // Staff go to the staff dashboard
            if (roles.Contains(Roles.Staff))
            {
                return RedirectToAction("Dashboard", "Staff");
            }

            // Everyone else goes to the donor dashboard
            return RedirectToAction("Dashboard", "Donor");
        }
    }
}