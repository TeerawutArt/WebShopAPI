﻿namespace WebShoppingAPI.DTOs.Request;

public class RefreshTokenDTO
{
    public string? AccessToken { get; set; }

    public string? RefreshToken { get; set; }
}
