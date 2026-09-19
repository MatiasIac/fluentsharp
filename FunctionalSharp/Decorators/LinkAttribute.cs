using System;

namespace FunctionalSharp.Decorators
{
    /// <summary>Assigns a name to a chain link for discovery through AddDecoratedLink.</summary>
    [AttributeUsage(
        AttributeTargets.Class | AttributeTargets.Struct, 
        AllowMultiple = false)]
    public sealed class LinkAttribute : Attribute
    {
        /// <summary>Gets or sets the unique name used to locate the link.</summary>
        public string LinkName { get; set; }

        /// <summary>Creates an attribute identifying a chain link by name.</summary>
        /// <param name="linkName">The name used by AddDecoratedLink.</param>
        public LinkAttribute(string linkName) 
        { 
            LinkName = linkName; 
        }
    }
}
