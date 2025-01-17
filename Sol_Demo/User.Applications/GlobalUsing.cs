﻿global using Microsoft.AspNetCore.Mvc;
global using Asp.Versioning;
global using MediatR;
global using Microsoft.AspNetCore.Http;
global using Microsoft.AspNetCore.Authorization;
global using Microsoft.AspNetCore.RateLimiting;
global using Models.Shared.Responses;
global using System.Net;
global using System.Text.RegularExpressions;
global using sorovi.DependencyInjection.AutoRegister;
global using FluentResults;
global using FluentValidation;
global using Hangfire;
global using Microsoft.Extensions.Logging;
global using HashPassword;
global using Utility.Shared.AES;
global using Utility.Shared.Config;
global using Utility.Shared.Exceptions;
global using Utility.Shared.Response;
global using Utility.Shared.ServiceHandler;
global using Utility.Shared.Validations;

global using Models.Shared.Requests;