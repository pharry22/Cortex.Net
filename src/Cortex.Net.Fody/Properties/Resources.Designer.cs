﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Cortex.Net.Fody.Properties {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "16.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Resources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resources() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("Cortex.Net.Fody.Properties.Resources", typeof(Resources).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to CannotHandle more than 16 parameters on Action Method: ({0}).
        /// </summary>
        internal static string MoreThan16Parameters {
            get {
                return ResourceManager.GetString("MoreThan16Parameters", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Property with name ({0}) has an unknown collection type ({1}). Skipping..
        /// </summary>
        internal static string NonReplaceableCollection {
            get {
                return ResourceManager.GetString("NonReplaceableCollection", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to EqualityComparer ({0}) on Property with name ({1}) on class ({2}) .
        /// </summary>
        internal static string NoParameterLessConstructorForEqualityComparer {
            get {
                return ResourceManager.GetString("NoParameterLessConstructorForEqualityComparer", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Property with name ({0}) on class ({1}) has no setter. Did you mean to create a computed property? Use [Computed] instead..
        /// </summary>
        internal static string NoSetterForObservable {
            get {
                return ResourceManager.GetString("NoSetterForObservable", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Property with name ({0}) on class ({1}) is not an auto property generated by the compiler. It&apos;s not possible to add the ObservableAttribute. Try manually creating an observable property or use an Atom instead..
        /// </summary>
        internal static string PropertyNotAutogenerated {
            get {
                return ResourceManager.GetString("PropertyNotAutogenerated", resourceCulture);
            }
        }
    }
}
