global using System;
global using System.Collections.Generic;
global using System.Linq;
global using Microsoft.EntityFrameworkCore;
global using Serilog;
global using System.Text.Json;
global using NodaMoney;

global using DiegoG.ToolSite.Server.Attributes;
global using DiegoG.ToolSite.Server.Logging;
global using DiegoG.ToolSite.Server.Models;
global using DiegoG.ToolSite.Server.Database.Models;
global using DiegoG.ToolSite.Server.Database.Models.Base;
global using DiegoG.ToolSite.Server.Database.Models.Ledger;
global using DiegoG.ToolSite.Server.Services;
global using DiegoG.ToolSite.Server.Types;

global using DiegoG.ToolSite.Shared.Models;
global using DiegoG.ToolSite.Shared.Models.Requests;
global using DiegoG.ToolSite.Shared.Models.Responses;

global using ILogger = Serilog.ILogger;