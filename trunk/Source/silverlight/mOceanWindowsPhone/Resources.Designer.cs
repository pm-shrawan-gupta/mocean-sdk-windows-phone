﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.225
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace mOceanWindowsPhone {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0")]
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
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("mOceanWindowsPhone.Resources", typeof(Resources).Assembly);
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
        ///   Looks up a localized string similar to var JSON = JSON || {};
        ///JSON.stringify = JSON.stringify || function (obj) {
        ///	var t = typeof (obj);
        ///	if (t != &quot;object&quot; || obj === null) {
        ///		if (obj === null) {
        ///			return &quot;null&quot;;
        ///		}
        ///		// simple data type  
        ///		if (t == &quot;string&quot;) obj = &apos;&quot;&apos; + obj + &apos;&quot;&apos;;
        ///		return String(obj);
        ///	}
        ///	else {
        ///		// recurse array or object  
        ///		var n, v, json = [], arr = (obj &amp;&amp; obj.constructor == Array);
        ///		for (n in obj) {
        ///			v = obj[n]; t = typeof (v);
        ///			if (t == &quot;string&quot;) v = &apos;&quot;&apos; + v + &apos;&quot;&apos;;
        ///			else if (t == &quot;object&quot; &amp;&amp; [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string ormmalib {
            get {
                return ResourceManager.GetString("ormmalib", resourceCulture);
            }
        }
    }
}