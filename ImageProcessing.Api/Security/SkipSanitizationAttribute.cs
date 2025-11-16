using System;

namespace ImageProcessing.Api.Security;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
public sealed class SkipSanitizationAttribute : Attribute { }
