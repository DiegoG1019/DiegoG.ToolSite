﻿namespace DiegoG.ToolSite.Shared.Models.Requests;

public record class NewUserRequest(string Username, string Email, string PasswordSha256);
