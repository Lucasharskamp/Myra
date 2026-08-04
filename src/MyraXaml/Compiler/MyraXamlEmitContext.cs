using Mono.Cecil.Cil;
using System.Collections.Generic;
using System.Linq;
using XamlX;
using XamlX.Ast;
using XamlX.Emit;
using XamlX.IL;
using XamlX.Transform;
using XamlX.TypeSystem;

namespace Myra.Xaml.Compiler
{
    public sealed class MyraXamlEmitContext
        : XamlEmitContext<MyraCecilILEmitter, XamlILNodeEmitResult>
    {
        public MyraXamlEmitContext(
            MyraCecilILEmitter emitter,
            TransformerConfiguration configuration,
            XamlLanguageEmitMappings<MyraCecilILEmitter, XamlILNodeEmitResult> emitMappings,
            XamlRuntimeContext<MyraCecilILEmitter, XamlILNodeEmitResult> runtimeContext,
            IXamlLocal? contextLocal,
            IXamlTypeBuilder<MyraCecilILEmitter> declaringType,
            IFileSource? file,
            IEnumerable<object> emitters)
            : base(
                emitter,
                configuration,
                emitMappings,
                runtimeContext,
                contextLocal,
                declaringType,
                file,
                emitters)
        {
        }

        protected override void EmitConvert(IXamlAstNode value, MyraCecilILEmitter codeGen, IXamlType expectedType, IXamlType returnedType)
        {
            // No conversion needed
            if (expectedType.Equals(returnedType))
                return;

            // Derived type -> base/interface type
            if (expectedType.IsAssignableFrom(returnedType))
            {
                return;
            }

            // Nullable<T>
            if (expectedType.GenericTypeDefinition?.FullName == "System.Nullable`1")
            {
                if (returnedType.Equals(expectedType.GenericArguments[0]))
                {
                    codeGen.Emit(OpCodes.Newobj, expectedType.Constructors.First(x => x.Parameters.Count == 1));

                    return;
                }
            }


            // Enum conversion
            if (expectedType.IsEnum && returnedType.IsValueType)
            {
                codeGen.Emit(OpCodes.Conv_I4);

                return;
            }


            // Primitive numeric conversions
            if (expectedType.IsValueType &&
                returnedType.IsValueType)
            {
                var opCode = GetNumericConversion(expectedType);

                if (opCode != null)
                {
                    codeGen.Emit(opCode.Value);
                    return;
                }
            }

            //todo: CONVERT TO MYRA TEXTURE- AND COLOR BRUSHES AND THE LIKE

            throw new XamlParseException(
                $"Cannot convert '{returnedType.FullName}' to '{expectedType.FullName}'.",
                0,
                0);
        }

        private static OpCode? GetNumericConversion(IXamlType type)
        {
            switch (type.FullName)
            {
                case "System.Int32":
                    return OpCodes.Conv_I4;

                case "System.Int64":
                    return OpCodes.Conv_I8;

                case "System.Single":
                    return OpCodes.Conv_R4;

                case "System.Double":
                    return OpCodes.Conv_R8;

                case "System.UInt32":
                    return OpCodes.Conv_U4;

                case "System.UInt64":
                    return OpCodes.Conv_U8;

                default:
                    return null;
            }
        }
    }
}
