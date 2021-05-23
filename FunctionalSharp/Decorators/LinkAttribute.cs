using System;

namespace FunctionalSharp.Decorators
{
    [AttributeUsage(
        AttributeTargets.Class | AttributeTargets.Struct, 
        AllowMultiple = false)]
    public sealed class LinkAttribute : Attribute
    {
        public string LinkName { get; set; }

        public LinkAttribute(string linkName) 
        { 
            LinkName = linkName; 
        }
    }
}