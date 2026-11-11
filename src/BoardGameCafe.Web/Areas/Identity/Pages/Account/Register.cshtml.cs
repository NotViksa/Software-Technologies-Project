using System.ComponentModel.DataAnnotations;
using BoardGameCafe.Web.Application.Common;
using BoardGameCafe.Web.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BoardGameCafe.Web.Areas.Identity.Pages.Account;

[AllowAnonymous]
public class RegisterModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public RegisterModel(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ReturnUrl { get; set; }

    public void OnGet(string? returnUrl = null)
    {
        ReturnUrl = returnUrl;
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        ReturnUrl = returnUrl;

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var user = new ApplicationUser
        {
            UserName = Input.Email,
            Email = Input.Email,
            DisplayName = Input.DisplayName,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, Input.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, Translate(error));
            }

            return Page();
        }

        await _userManager.AddToRoleAsync(user, Roles.Customer);
        await _signInManager.SignInAsync(user, isPersistent: false);

        if (!string.IsNullOrWhiteSpace(ReturnUrl) && Url.IsLocalUrl(ReturnUrl))
        {
            return LocalRedirect(ReturnUrl);
        }

        return LocalRedirect(Url.Content("~/")!);
    }

    private static string Translate(IdentityError error)
    {
        return error.Code switch
        {
            "PasswordTooShort" => "Паролата трябва да е поне 8 знака.",
            "PasswordRequiresDigit" => "Паролата трябва да съдържа цифра.",
            "PasswordRequiresLower" => "Паролата трябва да съдържа малка буква.",
            "PasswordRequiresUpper" => "Паролата трябва да съдържа главна буква.",
            "DuplicateUserName" or "DuplicateEmail" => "Вече има профил с този имейл.",
            _ => error.Description
        };
    }

    public class InputModel
    {
        [Required(ErrorMessage = "Въведи име.")]
        [StringLength(80, ErrorMessage = "Името е твърде дълго.")]
        public string DisplayName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Въведи имейл.")]
        [EmailAddress(ErrorMessage = "Невалиден имейл.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Въведи парола.")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Паролата трябва да е поне 8 знака.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Повтори паролата.")]
        [DataType(DataType.Password)]
        [Compare(nameof(Password), ErrorMessage = "Паролите не съвпадат.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
