// Part of FemtoCraft | Copyright 2012 Matvei Stefarov <me@matvei.org> | See LICENSE.txt
/*
 * Copyright 2007-2011 JetBrains s.r.o.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
using System;

namespace JetBrains.Annotations {
    [AttributeUsage( AttributeTargets.Constructor | AttributeTargets.Method, AllowMultiple = false, Inherited = true )]
    sealed class StringFormatMethodAttribute : Attribute {
        public StringFormatMethodAttribute( string formatParameterName ) {
            FormatParameterName = formatParameterName;
        }

        [UsedImplicitly]
        public string FormatParameterName { get; private set; }
    }


    [AttributeUsage( AttributeTargets.Parameter, AllowMultiple = false, Inherited = true )]
    sealed class InvokerParameterNameAttribute : Attribute { }


    [AttributeUsage( AttributeTargets.Method, AllowMultiple = false, Inherited = true )]
    sealed class TerminatesProgramAttribute : Attribute { }


    [AttributeUsage( AttributeTargets.Method | AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.Delegate | AttributeTargets.Field, AllowMultiple = false, Inherited = true )]
    sealed class CanBeNullAttribute : Attribute { }


    [AttributeUsage( AttributeTargets.Method | AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.Delegate | AttributeTargets.Field, AllowMultiple = false, Inherited = true )]
    sealed class NotNullAttribute : Attribute { }


    [AttributeUsage( AttributeTargets.Class, AllowMultiple = true, Inherited = true )]
    [BaseTypeRequired( typeof( Attribute ) )]
    sealed class BaseTypeRequiredAttribute : Attribute {
        public BaseTypeRequiredAttribute( Type baseType ) {
            BaseTypes = new[] { baseType };
        }

        public Type[] BaseTypes { get; private set; }
    }


    [AttributeUsage( AttributeTargets.All, AllowMultiple = false, Inherited = true )]
    sealed class UsedImplicitlyAttribute : Attribute {
        [UsedImplicitly]
        public UsedImplicitlyAttribute()
            : this( ImplicitUseKindFlags.Default ) { }

        [UsedImplicitly]
        public UsedImplicitlyAttribute( ImplicitUseKindFlags useKindFlags, ImplicitUseTargetFlags targetFlags = ImplicitUseTargetFlags.Default ) {
            UseKindFlags = useKindFlags;
            TargetFlags = targetFlags;
        }

        [UsedImplicitly]
        public UsedImplicitlyAttribute( ImplicitUseTargetFlags targetFlags )
            : this( ImplicitUseKindFlags.Default, targetFlags ) { }

        [UsedImplicitly]
        public ImplicitUseKindFlags UseKindFlags { get; private set; }

        [UsedImplicitly]
        public ImplicitUseTargetFlags TargetFlags { get; private set; }
    }


    [AttributeUsage( AttributeTargets.Class, AllowMultiple = false, Inherited = true )]
    sealed class MeansImplicitUseAttribute : Attribute {
        [UsedImplicitly]
        public MeansImplicitUseAttribute()
            : this( ImplicitUseKindFlags.Default ) { }

        [UsedImplicitly]
        public MeansImplicitUseAttribute( ImplicitUseKindFlags useKindFlags, ImplicitUseTargetFlags targetFlags = ImplicitUseTargetFlags.Default ) {
            UseKindFlags = useKindFlags;
            TargetFlags = targetFlags;
        }

        [UsedImplicitly]
        public MeansImplicitUseAttribute( ImplicitUseTargetFlags targetFlags )
            : this( ImplicitUseKindFlags.Default, targetFlags ) { }

        [UsedImplicitly]
        public ImplicitUseKindFlags UseKindFlags { get; private set; }

        [UsedImplicitly]
        public ImplicitUseTargetFlags TargetFlags { get; private set; }
    }


    [Flags]
    enum ImplicitUseKindFlags {
        Default = Access | Assign | InstantiatedWithFixedConstructorSignature,
        Access = 1,
        Assign = 2,
        InstantiatedWithFixedConstructorSignature = 4,
        InstantiatedNoFixedConstructorSignature = 8,
    }


    [Flags]
    enum ImplicitUseTargetFlags {
        Default = Itself,
        Itself = 1,
        Members = 2,
        WithMembers = Itself | Members
    }


    [AttributeUsage( AttributeTargets.Method, Inherited = true )]
    sealed class PureAttribute : Attribute {}
}