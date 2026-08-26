using System;
using System.Collections.Generic;
using System.Text;

namespace Myra.Markup
{
    /// <summary>
    /// x:ViewModel reference
    /// </summary>
    public sealed class ViewModel
    {       
        /// <summary>
        /// Default constructor
        /// </summary>
        public ViewModel(string type)
        {
            Type = type;
            MemberReference = "ViewModel";
        }

        /// <summary>
        /// Default constructor
        /// </summary> 
        public ViewModel(string type, string memberReference)  
        {
            Type = type;
            MemberReference = memberReference;
        } 

        /// <summary>
        /// Full assembly reference of the type to assign.
        /// </summary>
        public string Type { get; }

        /// <summary>
        /// name of the field or property reference containing the view model.
        /// </summary>
        public string MemberReference { get; }
    }
}
