using System;

namespace Utils.Torch
{
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class ConfigPropertyIgnoreAttribute : Attribute
    {
    }
}