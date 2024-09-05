using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using WebShoppingAPI.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
//ลง packet  Microsoft.AspNetCore.Authentication.JwtBearer 

namespace WebShoppingAPI.Helpers;


public class TokenHelper(IConfiguration config, UserManager<UserModel> _userManager)
{
    //primary constructor 
    private readonly IConfigurationSection jwtSettings = config.GetSection("JwtSettings");
    private readonly IConfigurationSection refreshTokenSettings = config.GetSection("RefreshTokenSettings");
    private readonly UserManager<UserModel> userManager = _userManager;

    /////////////Jwt token//////////////////
    public async Task<string> CreateJwtToken(UserModel user)
    {
        var signingCredentials = CreateSigningCredentials();
        var claims = await CreateClaims(user);
        var jwtSecurityToken = CreateJwtSecurityToken(signingCredentials, claims);
        var token = new JwtSecurityTokenHandler().WriteToken(jwtSecurityToken);
        return token;
    }
    ////////////Refresh Token////////////////
    public string CreateRefreshToken()
    {
        var randomNumber = new byte[Convert.ToInt32(refreshTokenSettings["TokenLength"])];
        //using method ขจัดค่าในตัวแปรเมื่อจบการทำงานใน blockนั้นๆ
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomNumber);
        }
        return Convert.ToBase64String(randomNumber);
    }
    /////////////////////////////////////////

    ////Create Token////////////////////////
    public async Task<(string AccessToken, string RefreshToken)> CreateToken(UserModel user, bool populateExp = true)
    {
        var accessToken = await CreateJwtToken(user);
        var refreshToken = CreateRefreshToken();
        user.RefreshToken = refreshToken;
        if (populateExp)
        {
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddMinutes(Convert.ToDouble(refreshTokenSettings["ExpiryInMinutes"]));
        }

        await userManager.UpdateAsync(user);

        return (accessToken, user.RefreshToken);
    }


    public ClaimsPrincipal GetClaimsPrincipalFromExpiredToken(string token)
    {
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = false,
            ValidIssuer = jwtSettings["ValidIssuer"],
            ValidAudience = jwtSettings["ValidAudience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["SecurityKey"]!))
        };

        var claimsPrincipal = new JwtSecurityTokenHandler().ValidateToken(token, tokenValidationParameters, out var validatedToken);

        var jwtToken = validatedToken as JwtSecurityToken;

        if (jwtToken is null || !jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
        {
            throw new SecurityTokenException("Invalid token.");
        }

        return claimsPrincipal;
    }



    private SigningCredentials CreateSigningCredentials()
    {
        var key = Encoding.UTF8.GetBytes(jwtSettings["SecurityKey"]!);
        var secretKey = new SymmetricSecurityKey(key);
        return new SigningCredentials(secretKey, SecurityAlgorithms.HmacSha256);

    }
    private async Task<List<Claim>> CreateClaims(UserModel user)
    {
        var claims = new List<Claim>{
            new Claim("uid",user.Id.ToString()),
            new Claim("name", user.FirstName + " " + user.LastName),
            new Claim("preferred_username", user.UserName!),
            new Claim("img",user.UserImageURL!)
        };
        var roles = await userManager.GetRolesAsync(user);
        foreach (var role in roles)
        {
            claims.Add(new Claim("role", role));
        }
        return claims;
    }

    private JwtSecurityToken CreateJwtSecurityToken(SigningCredentials signingCredentials, List<Claim> claims)
    {
        var token = new JwtSecurityToken(
            issuer: jwtSettings["ValidIssuer"],
            audience: jwtSettings["ValidAudience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(Convert.ToDouble(jwtSettings["ExpiryInMinutes"])),
            signingCredentials: signingCredentials
        );
        return token;
    }

}
